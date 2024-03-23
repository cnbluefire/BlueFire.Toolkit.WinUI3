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
using BlueFire.Toolkit.WinUI3.Core.Dispatching;
using Windows.System;

namespace BlueFire.Toolkit.WinUI3.Input
{
    internal class HotKeyListener : WindowMessageListener
    {
        private static Dictionary<nint, HotKeyListener> listeners = new Dictionary<nint, HotKeyListener>();

        private bool disposedValue;

        private static int id;
        private Dictionary<(HotKeyModifiers modifiers, VirtualKeys key), int> hotkeyEventHandlers
            = new Dictionary<(HotKeyModifiers modifiers, VirtualKeys key), int>();

        public HotKeyListener() : base(MessageWindow.HWND_MESSAGE, "BlueFire.Toolkit.HotKeyWnd")
        {
            this.MessageReceived += OnMessageReceived;
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

            return RunOnDispatcherQueueSynchronously<bool>(() =>
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

                var res = PInvoke.RegisterHotKey((HWND)WindowHandle, hotKeyId, modifiers2 | HOT_KEY_MODIFIERS.MOD_NOREPEAT, (uint)key);
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

            RunOnDispatcherQueueSynchronously(() =>
            {
                lock (hotkeyEventHandlers)
                {
                    if (hotkeyEventHandlers.Remove((modifiers, key), out var hotKeyId))
                    {
                        PInvoke.UnregisterHotKey((HWND)WindowHandle, hotKeyId);
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

            RunOnDispatcherQueueSynchronously(() =>
            {
                lock (hotkeyEventHandlers)
                {
                    foreach (var (_, key) in hotkeyEventHandlers)
                    {
                        PInvoke.UnregisterHotKey((HWND)WindowHandle, key);
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
                }

                disposedValue = true;
            }
        }
    }

    internal record HotKeyListenerInvokedEventArgs(HotKeyModifiers Modifier, VirtualKeys Key);

    internal delegate void HotKeyListenerInvokedEventHandler(HotKeyListener sender, HotKeyListenerInvokedEventArgs args);
}
