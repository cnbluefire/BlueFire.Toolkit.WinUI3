using Microsoft.UI;
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32.Foundation;
using PInvoke = global::Windows.Win32.PInvoke;
using BlueFire.Toolkit.WinUI3.Extensions;
using System.Diagnostics;
using BlueFire.Toolkit.WindowBase;

namespace BlueFire.Toolkit.WinUI3.WindowBase
{
    internal partial class WindowMessageMonitor : IDisposable
    {
        private readonly WindowManager windowManager;

        private HWND hWnd;
        private bool disposedValue;
        private WindowMessageListener? messageListener;

        private WindowMessageReceivedEventHandler? windowMessageReceived;
        private WindowMessageReceivedEventHandler? windowMessageBeforeReceived;
        private WindowMessageReceivedEventHandler? windowMessageAfterReceived;

        internal unsafe WindowMessageMonitor(WindowManager windowManager)
        {
            this.windowManager = windowManager;
            this.hWnd = windowManager.HWND;
        }

        internal bool MessageReceivedEventHandled => windowMessageReceived != null;

        private void OnMessageReceived(object sender, WindowMessageReceivedEventArgs e)
        {
            var handler1 = windowMessageBeforeReceived;
            var handler2 = windowMessageReceived;
            var handler3 = windowMessageAfterReceived;

            if (e.MessageId == PInvoke.WM_DESTROY)
            {
                windowMessageReceived = null;
                windowMessageBeforeReceived = null;
                windowMessageAfterReceived = null;

                Uninstall();
            }

            if (handler1 != null || handler2 != null)
            {
                handler1?.Invoke(windowManager, e);

                if (e.Handled)
                {
                    return;
                }
                else
                {
                    if (handler2 != null)
                    {
                        handler2.Invoke(windowManager, e);

                        if (e.Handled)
                        {
                            return;
                        }
                    }
                }
            }

            if (handler3 != null)
            {
                handler3?.Invoke(windowManager, e);
            }
        }

        private unsafe bool Install()
        {
            if (messageListener != null) return true;
            if (disposedValue) return false;

            var listener = messageListener = WindowMessageListener.TryCreate(hWnd);
            if (listener != null)
            {
                listener.MessageReceived += OnMessageReceived;
            }

            return messageListener != null;
        }

        private unsafe void Uninstall()
        {
            var listener = Interlocked.Exchange(ref messageListener, null);
            listener?.Dispose();
        }

        public event WindowMessageReceivedEventHandler? WindowMessageReceived
        {
            add
            {
                windowMessageReceived += value;
                if (windowMessageReceived != null)
                {
                    Install();
                }
            }
            remove
            {
                windowMessageReceived -= value;
                if (!disposedValue
                    && windowMessageReceived == null
                    && windowMessageBeforeReceived == null
                    && windowMessageAfterReceived == null)
                {
                    Uninstall();
                }
            }
        }

        internal event WindowMessageReceivedEventHandler? WindowMessageBeforeReceived
        {
            add
            {
                windowMessageBeforeReceived += value;
                if (windowMessageBeforeReceived != null)
                {
                    Install();
                }
            }
            remove
            {
                windowMessageBeforeReceived -= value;
                if (!disposedValue
                    && windowMessageReceived == null
                    && windowMessageBeforeReceived == null
                    && windowMessageAfterReceived == null)
                {
                    Uninstall();
                }
            }
        }

        internal event WindowMessageReceivedEventHandler? WindowMessageAfterReceived
        {
            add
            {
                windowMessageAfterReceived += value;
                if (windowMessageAfterReceived != null)
                {
                    Install();
                }
            }
            remove
            {
                windowMessageAfterReceived -= value;
                if (!disposedValue
                    && windowMessageReceived == null
                    && windowMessageBeforeReceived == null
                    && windowMessageAfterReceived == null)
                {
                    Uninstall();
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                }

                windowMessageReceived = null;
                windowMessageBeforeReceived = null;
                windowMessageAfterReceived = null;

                Uninstall();

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        ~WindowMessageMonitor()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
