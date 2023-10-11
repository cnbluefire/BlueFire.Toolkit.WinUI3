using BlueFire.Toolkit.WinUI3.WindowBase;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PInvoke = Windows.Win32.PInvoke;

namespace BlueFire.Toolkit.WinUI3.Extensions
{
    public static class WindowDialogExtensions
    {
        public static void SetDialogResult(WindowId windowId, bool? result)
        {
            if (windowId.Value == 0) return;

            if (DialogPropertiesManager.TryGetProperties((nint)windowId.Value, out var value))
            {
                value.Result = result;
            }
        }

        public static bool? GetDialogResult(WindowId windowId)
        {
            if (windowId.Value == 0) return default;

            if (DialogPropertiesManager.TryGetProperties((nint)windowId.Value, out var value))
            {
                return value.Result;
            }

            return default;
        }

        public static void SetDialogResult(this AppWindow window, bool? result) => SetDialogResult(window?.Id ?? default, result);

        public static bool? GetDialogResult(this AppWindow window) => GetDialogResult(window?.Id ?? default);

        public static async Task<bool?> ShowDialogAsync(this AppWindow window, WindowId owner)
        {
            var hwnd = new Windows.Win32.Foundation.HWND((nint)window.Id.Value);
            var ownerHwnd = new Windows.Win32.Foundation.HWND((nint)owner.Value);

            var tcs = new TaskCompletionSource<bool?>();
            var properties = new ShowDialogProperties(window);
            DialogPropertiesManager.SetProperties(hwnd, properties);
            var locker = new object();

            var manager = WindowManager.Get(window);

            if (manager == null) return null;

            var monitor = manager.GetMonitorInternal();
            monitor.WindowMessageBeforeReceived += OnWindowMessageBeforeReceived;
            monitor.WindowMessageAfterReceived += OnWindowMessageAfterReceived;

            try
            {
                try
                {
                    PInvoke.SetWindowLongAuto(hwnd, Windows.Win32.UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX.GWL_HWNDPARENT, ownerHwnd);
                }
                catch { }

                if (window.Presenter is not OverlappedPresenter presenter)
                {
                    presenter = OverlappedPresenter.CreateForDialog();
                    window.SetPresenter(presenter);
                }

                presenter.IsMinimizable = false;
                presenter.IsModal = true;

                if (PInvoke.GetWindowRect(ownerHwnd, out var rect))
                {
                    var windowSize = window.Size;

                    var left = rect.left + rect.Width / 2 - windowSize.Width / 2;
                    var top = rect.top + rect.Height / 2 - windowSize.Height / 2;

                    window.Move(new Windows.Graphics.PointInt32(left, top));
                }

                window.Show();

                return await tcs.Task;
            }
            finally
            {
                DialogPropertiesManager.RemoveProperties(hwnd);
                monitor.WindowMessageBeforeReceived -= OnWindowMessageBeforeReceived;
                monitor.WindowMessageAfterReceived -= OnWindowMessageAfterReceived;
            }


            void OnWindowMessageBeforeReceived(WindowManager sender, WindowMessageReceivedEventArgs e)
            {
                if (e.MessageId == PInvoke.WM_CLOSE)
                {
                    properties.Closing = true;

                    PInvoke.EnableWindow(ownerHwnd, true);
                    PInvoke.SetActiveWindow(ownerHwnd);
                }
                else if (e.MessageId == PInvoke.WM_DESTROY)
                {
                    tcs.TrySetResult(properties.Result);
                    DialogPropertiesManager.RemoveProperties(hwnd);
                    monitor.WindowMessageBeforeReceived -= OnWindowMessageBeforeReceived;
                    monitor.WindowMessageAfterReceived -= OnWindowMessageAfterReceived;
                }
            }

            void OnWindowMessageAfterReceived(WindowManager sender, WindowMessageReceivedEventArgs e)
            {
                if (e.MessageId == PInvoke.WM_CLOSE)
                {
                    if (PInvoke.IsWindow(hwnd))
                    {
                        // Closing Cancelled
                        PInvoke.EnableWindow(ownerHwnd, false);
                        PInvoke.SetActiveWindow(hwnd);
                    }
                    else
                    {
                        properties.Closed = true;
                    }

                    properties.Closing = false;
                }

            }
        }

        #region Properties Manager

        private static class DialogPropertiesManager
        {
            private static Dictionary<nint, ShowDialogProperties> showDialogProperties = new Dictionary<nint, ShowDialogProperties>();

            public static bool TryGetProperties(nint hwnd, [NotNullWhen(true)] out ShowDialogProperties? properties)
            {
                properties = default;

                lock (showDialogProperties)
                {
                    if (showDialogProperties.TryGetValue(hwnd, out var value))
                    {
                        properties = value;
                        return true;
                    }
                }

                return false;
            }

            public static void SetProperties(nint hwnd, ShowDialogProperties properties)
            {
                lock (showDialogProperties)
                {
                    if (!showDialogProperties.TryAdd(hwnd, properties))
                    {
                        throw new ArgumentException(nameof(hwnd));
                    }
                }
            }

            public static void RemoveProperties(nint hwnd)
            {
                lock (showDialogProperties)
                {
                    showDialogProperties.Remove(hwnd);
                }
            }

        }

        #endregion Properties Manager

        internal class ShowDialogProperties
        {
            private WeakReference<AppWindow> appWindow;
            private bool? result;

            public ShowDialogProperties(AppWindow appWindow)
            {
                this.appWindow = new WeakReference<AppWindow>(appWindow);
            }

            public bool? Result
            {
                get => result;
                set
                {
                    this.result = value;

                    if (appWindow != null
                        && appWindow.TryGetTarget(out AppWindow? window)
                        && window != null)
                    {
                        if (!Closing
                            && !Closed)
                        {
                            var hwnd = (nint)window.Id.Value;

                            PInvoke.SendMessage(
                                new Windows.Win32.Foundation.HWND(hwnd),
                                PInvoke.WM_SYSCOMMAND,
                                PInvoke.SC_CLOSE,
                                0);
                        }
                    }
                }
            }
            public bool Closing { get; set; }

            public bool Closed { get; set; }
        }
    }
}
