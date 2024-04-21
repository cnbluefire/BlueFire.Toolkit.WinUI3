using BlueFire.Toolkit.WinUI3.WindowBase;
using Microsoft.UI;
using Microsoft.UI.Composition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace BlueFire.Toolkit.WinUI3.SystemBackdrops
{
    internal class TransparentBackdropControllerEntry : IDisposable
    {
        private bool disposedValue;

        private WindowId windowId;
        private HWND hWnd;
        private WindowManager? windowManager;
        private WindowMessageMonitor? messageMonitor;
        private ICompositionSupportsSystemBackdrop connectedTarget;
        private bool flag;

        internal TransparentBackdropControllerEntry(ICompositionSupportsSystemBackdrop connectedTarget, WindowId windowId)
        {
            this.connectedTarget = connectedTarget;
            this.windowId = windowId;
            this.hWnd = new HWND(Microsoft.UI.Win32Interop.GetWindowFromWindowId(windowId));
            windowManager = WindowManager.Get(windowId);

            if (windowManager != null)
            {
                messageMonitor = windowManager.GetMonitorInternal();
                if (messageMonitor != null)
                {
                    flag = true;
                    messageMonitor.WindowMessageBeforeReceived += WndProc;

                    using var rgn = PInvoke.CreateRectRgn_SafeHandle(-1, -1, -2, -2);
                    var hRgn = new Windows.Win32.Graphics.Gdi.HRGN(rgn.DangerousGetHandle());
                    PInvoke.DwmEnableBlurBehindWindow(hWnd, new Windows.Win32.Graphics.Dwm.DWM_BLURBEHIND()
                    {
                        dwFlags = PInvoke.DWM_BB_ENABLE | PInvoke.DWM_BB_BLURREGION,
                        fEnable = true,
                        hRgnBlur = hRgn,
                    });

                    OnAttached(connectedTarget, windowId);
                }
            }

            if (!flag)
            {
                Clear();
            }
        }

        internal WindowId WindowId => windowId;

        internal ICompositionSupportsSystemBackdrop? ConnectedTarget => connectedTarget;

        internal virtual unsafe void WndProc(WindowManager sender, WindowMessageReceivedEventArgs e)
        {
            if (e.MessageId == PInvoke.WM_PAINT)
            {
                e.Handled = true;

                var hdc = PInvoke.BeginPaint(hWnd, out var ps);
                if (hdc.Value == 0) e.LResult = 0;

                var brush = PInvoke.GetStockObject(Windows.Win32.Graphics.Gdi.GET_STOCK_OBJECT_FLAGS.BLACK_BRUSH);
                if (PInvoke.FillRect(hdc, &ps.rcPaint, new Windows.Win32.Graphics.Gdi.HBRUSH(brush.Value)) != 0)
                {
                    e.LResult = 1;
                }
                e.LResult = 0;
            }
            else if (e.MessageId == PInvoke.WM_DESTROY)
            {
                Clear();
            }
        }

        private void Clear()
        {
            if (!flag) return;

            flag = false;

            if (messageMonitor != null)
            {
                messageMonitor.WindowMessageBeforeReceived -= WndProc;
                messageMonitor = null;
            }

            windowManager = null;

            PInvoke.DwmEnableBlurBehindWindow(hWnd, new Windows.Win32.Graphics.Dwm.DWM_BLURBEHIND()
            {
                dwFlags = PInvoke.DWM_BB_ENABLE,
                fEnable = false,
            });

            var target = connectedTarget;
            var windowId = this.windowId;

            if (connectedTarget != null)
            {
                connectedTarget.SystemBackdrop = null;
                connectedTarget = null!;
            }

            windowId = default;

            OnClear?.Invoke(this, EventArgs.Empty);

            OnDetached(target, windowId);
        }

        public event EventHandler? OnClear;

        protected virtual void OnAttached(ICompositionSupportsSystemBackdrop connectedTarget, WindowId windowId) { }

        protected virtual void OnDetached(ICompositionSupportsSystemBackdrop connectedTarget, WindowId windowId) { }

        protected virtual void DisposeCore(bool disposing) { }


        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)

                    Clear();
                }

                DisposeCore(disposing);

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
