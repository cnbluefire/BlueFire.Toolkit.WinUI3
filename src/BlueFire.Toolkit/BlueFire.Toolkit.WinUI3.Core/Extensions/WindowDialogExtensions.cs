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
    /// <summary>
    /// Modal window extensions.
    /// </summary>
    public static class WindowDialogExtensions
    {
        private static uint _WM_MOVECENTER = 0;

        internal static uint WM_MOVECENTER
        {
            get
            {
                if (_WM_MOVECENTER == 0)
                {
                    _WM_MOVECENTER = PInvoke.RegisterWindowMessage(nameof(WM_MOVECENTER));
                }
                return _WM_MOVECENTER;
            }
        }

        /// <summary>
        /// Set dialog window result and close window.
        /// </summary>
        /// <param name="windowId"></param>
        /// <param name="result"></param>
        public static void SetDialogResult(WindowId windowId, bool? result)
        {
            if (windowId.Value == 0) return;

            if (DialogPropertiesManager.TryGetProperties((nint)windowId.Value, out var value))
            {
                value.Result = result;
            }
        }

        /// <summary>
        /// Get dialog window result.
        /// </summary>
        /// <param name="windowId"></param>
        /// <returns></returns>
        public static bool? GetDialogResult(WindowId windowId)
        {
            if (windowId.Value == 0) return default;

            if (DialogPropertiesManager.TryGetProperties((nint)windowId.Value, out var value))
            {
                return value.Result;
            }

            return default;
        }

        /// <summary>
        /// Set dialog window result and close window.
        /// </summary>
        /// <param name="window"></param>
        /// <param name="result"></param>
        public static void SetDialogResult(this AppWindow window, bool? result) => SetDialogResult(window?.Id ?? default, result);

        /// <summary>
        /// Get dialog window result.
        /// </summary>
        /// <param name="window"></param>
        /// <returns></returns>
        public static bool? GetDialogResult(this AppWindow window) => GetDialogResult(window?.Id ?? default);

        /// <summary>
        /// Shows the window as a modal dialog box.
        /// </summary>
        /// <param name="window"></param>
        /// <param name="owner">Sets the window that owns this Window.</param>
        /// <returns></returns>
        public static async Task<bool?> ShowDialogAsync(this AppWindow window, WindowId owner)
        {
            var manager = WindowManager.Get(window);
            if (manager == null) return null;

            var hwnd = new Windows.Win32.Foundation.HWND((nint)window.Id.Value);
            var ownerHwnd = EnsureOwnerWindow(owner);

            if (!PInvoke.IsWindowEnabled(ownerHwnd)) return null;

            var threadWindows = GetThreadWindows();
            EnableThreadWindows(threadWindows, false);

            var capture = PInvoke.GetCapture();
            if (!capture.IsNull)
            {
                PInvoke.ReleaseCapture();
            }

            var tcs = new TaskCompletionSource<bool?>();
            var properties = new ShowDialogProperties(window);
            DialogPropertiesManager.SetProperties(hwnd, properties);

            var monitor = manager.GetMonitorInternal();
            monitor.WindowMessageBeforeReceived += OnWindowMessageBeforeReceived;
            monitor.WindowMessageAfterReceived += OnWindowMessageAfterReceived;

            try
            {
                PInvoke.SetWindowLongAuto(hwnd, Windows.Win32.UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX.GWL_HWNDPARENT, ownerHwnd);


                if (window.Presenter is not OverlappedPresenter presenter)
                {
                    presenter = OverlappedPresenter.CreateForDialog();
                    window.SetPresenter(presenter);
                }

                presenter.IsMinimizable = false;
                presenter.IsModal = true;

                MoveWindowToCenter(ownerHwnd.Value, hwnd.Value);

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
            catch
            {
                EnableThreadWindows(threadWindows, true);
                throw;
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

                    EnableThreadWindows(threadWindows, true);
                    PInvoke.SetActiveWindow(ownerHwnd);
                }
                else if (e.MessageId == PInvoke.WM_DESTROY)
                {
                    PInvoke.SetWindowLongAuto(hwnd, Windows.Win32.UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX.GWL_HWNDPARENT, default);
                    tcs.TrySetResult(properties.Result);
                    DialogPropertiesManager.RemoveProperties(hwnd);
                    monitor.WindowMessageBeforeReceived -= OnWindowMessageBeforeReceived;
                    monitor.WindowMessageAfterReceived -= OnWindowMessageAfterReceived;
                }
                else if (_WM_MOVECENTER != 0 && e.MessageId == _WM_MOVECENTER)
                {
                    MoveWindowToCenter(ownerHwnd.Value, hwnd.Value);
                }
            }

            void OnWindowMessageAfterReceived(WindowManager sender, WindowMessageReceivedEventArgs e)
            {
                if (e.MessageId == PInvoke.WM_CLOSE)
                {
                    if (PInvoke.IsWindow(hwnd))
                    {
                        // Closing Cancelled
                        EnableThreadWindows(threadWindows, false);
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


        private static IReadOnlyList<Windows.Win32.Foundation.HWND> GetThreadWindows() =>
            PInvoke.EnumThreadWindows((hWnd, _) =>
                PInvoke.IsWindowVisible(hWnd)
                && PInvoke.IsWindowEnabled(hWnd), 0) ?? Array.Empty<Windows.Win32.Foundation.HWND>();

        private static void EnableThreadWindows(IEnumerable<Windows.Win32.Foundation.HWND> windows, bool state)
        {
            var _state = new Windows.Win32.Foundation.BOOL(state);
            foreach (var hWnd in windows)
            {
                if (PInvoke.IsWindow(hWnd)) PInvoke.EnableWindow(hWnd, _state);
            }
        }

        private static Windows.Win32.Foundation.HWND EnsureOwnerWindow(WindowId ownerWindow)
        {
            var ownerHWnd = (Windows.Win32.Foundation.HWND)Win32Interop.GetWindowFromWindowId(ownerWindow);
            if (ownerHWnd.IsNull || ownerHWnd.Value == PInvoke.GetDesktopWindow()) return new Windows.Win32.Foundation.HWND();
            if (!PInvoke.IsWindow(ownerHWnd)) return new Windows.Win32.Foundation.HWND();

            while (!ownerHWnd.IsNull)
            {
                var style = PInvoke.GetWindowLongAuto(ownerHWnd, Windows.Win32.UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX.GWL_STYLE);
                if ((style & (nint)Windows.Win32.UI.WindowsAndMessaging.WINDOW_STYLE.WS_CHILD) != 0)
                {
                    ownerHWnd = PInvoke.GetParent(ownerHWnd);
                }
                else
                {
                    break;
                }
            }

            return ownerHWnd;
        }

        private static void MoveWindowToCenter(nint ownerWindow, nint ownedWindow)
        {
            if (PInvoke.GetWindowRect((Windows.Win32.Foundation.HWND)ownerWindow, out var parentRect)
                && PInvoke.GetWindowRect((Windows.Win32.Foundation.HWND)ownedWindow, out var rect))
            {
                var left = parentRect.left + parentRect.Width / 2 - rect.Width / 2;
                var top = parentRect.top + parentRect.Height / 2 - rect.Height / 2;

                PInvoke.SetWindowPos((Windows.Win32.Foundation.HWND)ownedWindow,
                    default,
                    left, top, 0, 0,
                    Windows.Win32.UI.WindowsAndMessaging.SET_WINDOW_POS_FLAGS.SWP_NOSIZE
                    | Windows.Win32.UI.WindowsAndMessaging.SET_WINDOW_POS_FLAGS.SWP_NOZORDER);
            }
        }

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
