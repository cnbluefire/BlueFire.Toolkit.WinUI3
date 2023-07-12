using BlueFire.Toolkit.WinUI3.Extensions;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32.Foundation;
using WinRT;
using Windows.Win32;
using Microsoft.UI.Xaml.Controls;

namespace BlueFire.Toolkit.WinUI3.WindowBase
{
    public partial class WindowManager : IDisposable
    {
        private DispatcherQueue dispatcherQueue;
        private readonly WindowId windowId;
        private readonly HWND hWnd;
        private AppWindow? appWindow;
        private WindowEx? windowExInternal;
        private WindowMessageMonitor? messageMonitor;
        private bool wndProcInstalled;

        private double minWidth;
        private double minHeight;
        private double maxWidth;
        private double maxHeight;

        private uint dpi = 0;

        private bool disposedValue;

        internal WindowManager(WindowId windowId)
        {
            dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            this.windowId = windowId;
            this.hWnd = new HWND((nint)windowId.Value);

            messageMonitor = new WindowMessageMonitor(this);
        }

        public WindowId WindowId => windowId;

        internal HWND HWND => hWnd;

        public AppWindow? AppWindow => appWindow ??= AppWindow.GetFromWindowId(windowId);

        internal WindowEx? WindowExInternal
        {
            get => windowExInternal;
            set => windowExInternal = value;
        }

        internal WindowMessageMonitor GetMonitorInternal()
        {
            if (disposedValue) throw new ObjectDisposedException(nameof(WindowManager));
            return messageMonitor!;
        }

        public event WindowMessageReceivedEventHandler? WindowMessageReceived
        {
            add => messageMonitor!.WindowMessageReceived += value;
            remove => messageMonitor!.WindowMessageReceived -= value;
        }

        public double MinWidth
        {
            get => minWidth;
            set
            {
                if (minWidth != value)
                {
                    minWidth = value;
                    UpdateWindowProcHandler();
                    RefreshWindowSize();
                }
            }
        }

        public double MinHeight
        {
            get => minHeight;
            set
            {
                if (minHeight != value)
                {
                    minHeight = value;
                    UpdateWindowProcHandler();
                    RefreshWindowSize();
                }
            }
        }

        public double MaxWidth
        {
            get => maxWidth;
            set
            {
                if (maxWidth != value)
                {
                    maxWidth = value;
                    UpdateWindowProcHandler();
                    RefreshWindowSize();
                }
            }
        }

        public double MaxHeight
        {
            get => maxHeight;
            set
            {
                if (maxHeight != value)
                {
                    maxHeight = value;
                    UpdateWindowProcHandler();
                    RefreshWindowSize();
                }
            }
        }


        private void UpdateWindowProcHandler()
        {
            var v = !disposedValue
                && (minWidth != 0 || minHeight != 0 || minWidth != 0 || minHeight != 0);

            if (v && !wndProcInstalled)
            {
                wndProcInstalled = true;
                GetMonitorInternal().WindowMessageBeforeReceived += OverrideWindowProc;
            }
            else if (!v && wndProcInstalled)
            {
                dpi = 0;
                GetMonitorInternal().WindowMessageBeforeReceived -= OverrideWindowProc;
            }
        }

        private void RefreshWindowSize()
        {
            if (!disposedValue && PInvoke.GetWindowRect(HWND, out var rect))
            {
                PInvoke.SetWindowPos(HWND, default, rect.X, rect.Y, rect.Width, rect.Height,
                    global::Windows.Win32.UI.WindowsAndMessaging.SET_WINDOW_POS_FLAGS.SWP_NOZORDER);
            }
        }

        private unsafe void OverrideWindowProc(WindowManager sender, WindowMessageReceivedEventArgs e)
        {
            if (e.MessageId == PInvoke.WM_GETMINMAXINFO)
            {
                var info = (global::Windows.Win32.UI.WindowsAndMessaging.MINMAXINFO*)e.LParam;
                if (dpi == 0)
                {
                    dpi = PInvoke.GetDpiForWindow(HWND);
                }

                var minWidthPixel = (int)(minWidth * dpi / 96);
                var minHeightPixel = (int)(minHeight * dpi / 96);

                var maxWidthPixel = (int)(maxWidth * dpi / 96);
                var maxHeightPixel = (int)(maxHeight * dpi / 96);

                if (maxWidthPixel == 0) maxWidthPixel = PInvoke.GetSystemMetrics(global::Windows.Win32.UI.WindowsAndMessaging.SYSTEM_METRICS_INDEX.SM_CXMAXTRACK);
                if (maxHeightPixel == 0) maxHeightPixel = PInvoke.GetSystemMetrics(global::Windows.Win32.UI.WindowsAndMessaging.SYSTEM_METRICS_INDEX.SM_CYMAXTRACK);

                info->ptMinTrackSize = new System.Drawing.Point(minWidthPixel, minHeightPixel);
                info->ptMaxTrackSize = new System.Drawing.Point(maxWidthPixel, maxHeightPixel);

                e.Handled = true;
                e.LResult = 0;
            }
        }

        private void OnDestroyProc(WindowManager sender, WindowMessageReceivedEventArgs e)
        {
            if (e.MessageId == PInvoke.WM_DESTROY)
            {
                WindowMessageReceived += OnDestroyProc;

                lock (windowManagers)
                {
                    if (windowManagers.Remove(WindowId, out var manager))
                    {
                        manager.Dispose();
                    }
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

                if (messageMonitor != null)
                {
                    messageMonitor.Dispose();
                    messageMonitor = null;
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        ~WindowManager()
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
