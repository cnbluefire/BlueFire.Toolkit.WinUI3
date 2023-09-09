using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Win32.Foundation;
using PInvoke = Windows.Win32.PInvoke;
using WNDCLASSEXW = Windows.Win32.UI.WindowsAndMessaging.WNDCLASSEXW;
using HOT_KEY_MODIFIERS = Windows.Win32.UI.Input.KeyboardAndMouse.HOT_KEY_MODIFIERS;

namespace BlueFire.Toolkit.WinUI3.Input
{
    internal class HotKeyListener : IDisposable
    {
        private static Dictionary<nint, HotKeyListener> listeners = new Dictionary<nint, HotKeyListener>();

        private bool disposedValue;

        private static int id;
        private Thread backgroundThread;
        private DispatcherQueueController? dispatcherQueueController;
        private EventWaitHandle? initializeWaitHandle;
        private HWND messageWindowHandle;
        private Dictionary<(HotKeyModifiers modifiers, VirtualKeys key), int> hotkeyEventHandlers
            = new Dictionary<(HotKeyModifiers modifiers, VirtualKeys key), int>();

        public HotKeyListener()
        {
            backgroundThread = new Thread(ThreadMain);
            backgroundThread.IsBackground = true;
            backgroundThread.SetApartmentState(ApartmentState.STA);

            initializeWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
            backgroundThread.Start();

            initializeWaitHandle.WaitOne();
            initializeWaitHandle.Dispose();
            initializeWaitHandle = null;

            lock (listeners)
            {
                listeners[messageWindowHandle.Value] = this;
            }
        }

        private void ThreadMain()
        {
            dispatcherQueueController = DispatcherQueueController.CreateOnCurrentThread();

            try
            {
                CreateMessageWindow();
            }
            catch { }

            initializeWaitHandle!.Set();

            dispatcherQueueController.DispatcherQueue.RunEventLoop();
        }

        private unsafe void CreateMessageWindow()
        {
            const nint HWND_MESSAGE = -3;

            var windowName = $"HotKeyWnd";
            var className = $"{windowName}_{Guid.NewGuid()}";

            fixed (char* pClassName = className)
            {
                var wndClassEx = new WNDCLASSEXW()
                {
                    cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
                    lpfnWndProc = &GlobalWndProc,
                    lpszClassName = pClassName
                };

                var ret = PInvoke.RegisterClassEx(in wndClassEx);

                if (ret != 0)
                {
                    var hInstance = new HINSTANCE(PInvoke.GetModuleHandle((char*)0).Value);

                    fixed (char* pWindowName = windowName)
                    {
                        messageWindowHandle = PInvoke.CreateWindowEx(
                            0,
                            pClassName,
                            pWindowName,
                            0,
                            0, 0, 0, 0,
                            new HWND(HWND_MESSAGE),
                            Windows.Win32.UI.WindowsAndMessaging.HMENU.Null, hInstance, (void*)0);
                    }
                }
            }
        }

        private LRESULT WndProc(HWND hwnd, uint uMsg, WPARAM wParam, LPARAM lParam)
        {
            if (uMsg == PInvoke.WM_HOTKEY)
            {
                if (wParam.Value != unchecked((nuint)(-2)) && wParam.Value != unchecked((nuint)(-1)))
                {
                    HotKeyModifiers modifiers = 0;
                    VirtualKeys key = 0;

                    lock (hotkeyEventHandlers)
                    {
                        foreach (var ((tmpModifier, tmpKey), id) in hotkeyEventHandlers)
                        {
                            if (unchecked((nuint)id) == wParam.Value)
                            {
                                modifiers = tmpModifier;
                                key = tmpKey;

                                dispatcherQueueController?.DispatcherQueue.TryEnqueue(() =>
                                {
                                    try
                                    {
                                        HotKeyInvoked?.Invoke(this, new HotKeyInvokedEventArgs(tmpModifier, tmpKey));
                                    }
                                    catch { }
                                });

                                break;
                            }
                        }
                    }
                }
                return new LRESULT(0);
            }

            return PInvoke.DefWindowProc(hwnd, uMsg, wParam, lParam);
        }

        public async Task<bool> RegisterKey(HotKeyModifiers modifiers, VirtualKeys key)
        {
            if (modifiers == 0 && key == 0) return false;

            var tcs = new TaskCompletionSource<bool>();

            dispatcherQueueController!.DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    int hotKeyId;

                    lock (hotkeyEventHandlers)
                    {
                        if (hotkeyEventHandlers.ContainsKey((modifiers, key)))
                        {
                            tcs.TrySetResult(true);
                            return;
                        }

                        hotKeyId = id;
                        id++;
                    }

                    var modifiers2 = (HOT_KEY_MODIFIERS)(ushort)modifiers;

                    var res = PInvoke.RegisterHotKey(messageWindowHandle, hotKeyId, modifiers2 | HOT_KEY_MODIFIERS.MOD_NOREPEAT, (uint)key);
                    if (res)
                    {
                        hotkeyEventHandlers[(modifiers, key)] = hotKeyId;
                    }

                    tcs.TrySetResult(res);
                }
                finally
                {
                    tcs.TrySetResult(false);
                }
            });

            return await tcs.Task.ConfigureAwait(false);
        }

        public async Task UnregisterKey(HotKeyModifiers modifiers, VirtualKeys key)
        {
            if (modifiers == 0 && key == 0) return;

            var tcs = new TaskCompletionSource();

            dispatcherQueueController!.DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    lock (hotkeyEventHandlers)
                    {
                        if (hotkeyEventHandlers.Remove((modifiers, key), out var hotKeyId))
                        {
                            PInvoke.UnregisterHotKey(messageWindowHandle, hotKeyId);
                        }
                    }
                }
                finally
                {
                    tcs.TrySetResult();
                }
            });

            await tcs.Task.ConfigureAwait(false);
        }

        public async Task UnregisterAllKeys()
        {
            lock (hotkeyEventHandlers)
            {
                if (hotkeyEventHandlers.Count == 0) return;
            }

            var tcs = new TaskCompletionSource();

            dispatcherQueueController!.DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    lock (hotkeyEventHandlers)
                    {
                        foreach (var (_, key) in hotkeyEventHandlers)
                        {
                            PInvoke.UnregisterHotKey(messageWindowHandle, key);
                        }

                        hotkeyEventHandlers.Clear();
                    }
                }
                finally
                {
                    tcs.TrySetResult();
                }
            });

            await tcs.Task.ConfigureAwait(false);
        }

        public bool GetHotKeyState(HotKeyModifiers modifiers, VirtualKeys key)
        {
            lock (hotkeyEventHandlers)
            {
                return hotkeyEventHandlers.ContainsKey((modifiers, key));
            }
        }

        public event HotKeyInvokedEventHandler? HotKeyInvoked;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    lock (hotkeyEventHandlers)
                    {
                        hotkeyEventHandlers.Clear();
                    }

                    initializeWaitHandle?.WaitOne();
                    initializeWaitHandle?.Dispose();
                    initializeWaitHandle = null;

                    PInvoke.DestroyWindow(messageWindowHandle);

                    bool hasThreadAccess = dispatcherQueueController?.DispatcherQueue.HasThreadAccess ?? false;

                    dispatcherQueueController?.DispatcherQueue.TryEnqueue(() =>
                    {
                        Thread.Sleep(10000);
                    });

                    dispatcherQueueController?.DispatcherQueue.EnqueueEventLoopExit();

                    dispatcherQueueController = null;

                    if (!hasThreadAccess)
                    {
                        backgroundThread.Join();
                    }

                    backgroundThread = null!;

                    lock (listeners)
                    {
                        listeners.Remove(messageWindowHandle.Value);
                    }

                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        private static LRESULT GlobalWndProc(HWND hwnd, uint uMsg, WPARAM wParam, LPARAM lParam)
        {
            lock (listeners)
            {
                if (listeners.TryGetValue(hwnd.Value, out var value))
                {
                    return value.WndProc(hwnd, uMsg, wParam, lParam);
                }
            }

            return PInvoke.DefWindowProc(hwnd, uMsg, wParam, lParam);
        }
    }

    public record HotKeyInvokedEventArgs(HotKeyModifiers Modifier, VirtualKeys Key);


    internal delegate void HotKeyInvokedEventHandler(HotKeyListener sender, HotKeyInvokedEventArgs args);
}
