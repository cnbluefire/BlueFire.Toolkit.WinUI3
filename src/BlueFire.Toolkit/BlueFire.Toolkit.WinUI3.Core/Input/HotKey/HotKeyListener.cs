using BlueFire.Toolkit.WinUI3.Core.Dispatching;
using BlueFire.Toolkit.WindowBase;
using BlueFire.Toolkit.Dispatching;
using Microsoft.UI.Dispatching;
using Windows.Win32.Foundation;
using Windows.System;
using PInvoke = Windows.Win32.PInvoke;
using HOT_KEY_MODIFIERS = Windows.Win32.UI.Input.KeyboardAndMouse.HOT_KEY_MODIFIERS;
using BlueFire.Toolkit.WinUI3.Extensions;

namespace BlueFire.Toolkit.WinUI3.Input
{
    internal partial class HotKeyListener : WorkerQueueObject
    {
        private const nint HWND_MESSAGE = -3;

        private static int id;

        private bool disposedValue;

        private NativeWindow nativeWindow = null!;
        private Dictionary<(HotKeyModifiers modifiers, VirtualKeys key), int> hotkeyEventHandlers
            = new Dictionary<(HotKeyModifiers modifiers, VirtualKeys key), int>();

        public HotKeyListener()
        {
            DispatcherQueue.RunSynchronously(() =>
            {
                nativeWindow = new NativeWindow(HWND_MESSAGE, "BlueFire.Toolkit.HotKeyWnd");
                nativeWindow.MessageReceived += OnMessageReceived;
            });
        }

        private void OnMessageReceived(object sender, WindowMessageReceivedEventArgs e)
        {
            if (e.MessageId == PInvoke.WM_HOTKEY)
            {
                if (e.WParam != unchecked((nuint)(-2)) && e.WParam != unchecked((nuint)(-1)))
                {
                    HotKeyModifiers modifiers = 0;
                    VirtualKeys key = 0;

                    lock (hotkeyEventHandlers)
                    {
                        foreach (var ((tmpModifier, tmpKey), id) in hotkeyEventHandlers)
                        {
                            if (unchecked((nuint)id) == e.WParam)
                            {
                                modifiers = tmpModifier;
                                key = tmpKey;

                                DispatcherQueue.TryEnqueue(() =>
                                {
                                    try
                                    {
                                        HotKeyInvoked?.Invoke(this, new HotKeyListenerInvokedEventArgs(tmpModifier, tmpKey));
                                    }
                                    catch { }
                                });

                                break;
                            }
                        }
                    }
                }
                e.LResult = new LRESULT(0);
                e.Handled = true;
            }
        }

        public bool RegisterKey(HotKeyModifiers modifiers, VirtualKeys key)
        {
            if (modifiers == 0 && key == 0) return false;

            return DispatcherQueue.RunSynchronously<bool>(() =>
            {
                int hotKeyId;

                lock (hotkeyEventHandlers)
                {
                    if (hotkeyEventHandlers.ContainsKey((modifiers, key)))
                    {
                        return true;
                    }

                    hotKeyId = id;
                    id++;
                }

                var modifiers2 = (HOT_KEY_MODIFIERS)(ushort)modifiers;

                var res = PInvoke.RegisterHotKey((HWND)nativeWindow.WindowHandle, hotKeyId, modifiers2 | HOT_KEY_MODIFIERS.MOD_NOREPEAT, (uint)key);
                if (res)
                {
                    hotkeyEventHandlers[(modifiers, key)] = hotKeyId;
                }

                return res;
            });
        }

        public void UnregisterKey(HotKeyModifiers modifiers, VirtualKeys key)
        {
            if (modifiers == 0 && key == 0) return;

            DispatcherQueue.RunSynchronously(() =>
            {
                lock (hotkeyEventHandlers)
                {
                    if (hotkeyEventHandlers.Remove((modifiers, key), out var hotKeyId))
                    {
                        PInvoke.UnregisterHotKey((HWND)nativeWindow.WindowHandle, hotKeyId);
                    }
                }
            });
        }

        public void UnregisterAllKeys()
        {
            lock (hotkeyEventHandlers)
            {
                if (hotkeyEventHandlers.Count == 0) return;
            }

            DispatcherQueue.RunSynchronously(() =>
            {
                lock (hotkeyEventHandlers)
                {
                    foreach (var (_, key) in hotkeyEventHandlers)
                    {
                        PInvoke.UnregisterHotKey((HWND)nativeWindow.WindowHandle, key);
                    }

                    hotkeyEventHandlers.Clear();
                }
            });
        }

        public bool GetHotKeyState(HotKeyModifiers modifiers, VirtualKeys key)
        {
            lock (hotkeyEventHandlers)
            {
                return hotkeyEventHandlers.ContainsKey((modifiers, key));
            }
        }

        public event HotKeyListenerInvokedEventHandler? HotKeyInvoked;

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    UnregisterAllKeys();
                    nativeWindow.Dispose();
                }

                disposedValue = true;
            }
        }
    }

    internal record HotKeyListenerInvokedEventArgs(HotKeyModifiers Modifier, VirtualKeys Key);

    internal delegate void HotKeyListenerInvokedEventHandler(HotKeyListener sender, HotKeyListenerInvokedEventArgs args);
}
