using BlueFire.Toolkit.WinUI3.WindowBase;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics;
using Windows.System;
using Windows.Win32.Foundation;
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

            if (DialogWindowContext.TryGetContext((nint)windowId.Value, out var value))
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

            if (DialogWindowContext.TryGetContext((nint)windowId.Value, out var value))
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
        /// <param name="options"></param>
        /// <returns></returns>
        public static async Task<bool?> ShowDialogAsync(this AppWindow window, ShowDialogOptions? options = null)
        {
            var manager = WindowManager.Get(window);
            if (manager == null) return null;

            var hwnd = new HWND((nint)window.Id.Value);
            var ownerHwnd = EnsureOwnerWindow(options?.OwnerWindowId);

            if (!ownerHwnd.IsNull && !PInvoke.IsWindowEnabled(ownerHwnd)) return null;

            var context = new DialogWindowContext(manager, options, ownerHwnd);

            try
            {
                if (window.Presenter is not OverlappedPresenter presenter)
                {
                    presenter = OverlappedPresenter.CreateForDialog();
                    window.SetPresenter(presenter);
                }

                // 无需设置 presenter.IsModal 因为随后会禁用UI线程所有窗口
                presenter.IsMinimizable = false;

                MoveWindowToCenter(ownerHwnd, hwnd.Value, options?.Location ?? DialogWindowStartupLocation.CenterScreen);

                if (PInvoke.GetWindowRect(ownerHwnd, out var rect))
                {
                    var windowSize = window.Size;

                    var left = rect.left + rect.Width / 2 - windowSize.Width / 2;
                    var top = rect.top + rect.Height / 2 - windowSize.Height / 2;

                    window.Move(new Windows.Graphics.PointInt32(left, top));
                }

                window.Show();

                return await context.GetResultAsync();
            }
            catch
            {
                context.Dispose();
                throw;
            }
        }

        private static IReadOnlyList<HWND> GetThreadWindows() =>
            PInvoke.EnumThreadWindows((hWnd, _) =>
                PInvoke.IsWindowVisible(hWnd)
                && PInvoke.IsWindowEnabled(hWnd), 0) ?? Array.Empty<HWND>();

        private static void EnableThreadWindows(IEnumerable<HWND> windows, bool state)
        {
            var _state = new BOOL(state);
            foreach (var hWnd in windows)
            {
                if (PInvoke.IsWindow(hWnd)) PInvoke.EnableWindow(hWnd, _state);
            }
        }

        private static HWND EnsureOwnerWindow(WindowId? ownerWindow)
        {
            if (!ownerWindow.HasValue) return default;
            var ownerHWnd = (HWND)Win32Interop.GetWindowFromWindowId(ownerWindow.Value);
            if (ownerHWnd.IsNull || ownerHWnd.Value == PInvoke.GetDesktopWindow()) return new HWND();
            if (!PInvoke.IsWindow(ownerHWnd)) return new HWND();

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

        private static unsafe void MoveWindowToCenter(nint ownerWindow, nint ownedWindow, DialogWindowStartupLocation location)
        {
            var parentRect = new RECT(0, 0, 0, 0);

            if (ownerWindow != 0 && location == DialogWindowStartupLocation.CenterOwner)
            {
                if (!PInvoke.GetWindowRect((HWND)ownerWindow, out parentRect))
                {
                    parentRect = new RECT(0, 0, 0, 0);
                }
            }

            if (parentRect.Width == 0 || parentRect.Height == 0)
            {
                var monitor = PInvoke.MonitorFromWindow((HWND)ownerWindow, Windows.Win32.Graphics.Gdi.MONITOR_FROM_FLAGS.MONITOR_DEFAULTTOPRIMARY);
                Windows.Win32.Graphics.Gdi.MONITORINFO monitorInfo = new()
                {
                    cbSize = (uint)sizeof(Windows.Win32.Graphics.Gdi.MONITORINFO)
                };

                if (PInvoke.GetMonitorInfo(monitor, &monitorInfo))
                {
                    parentRect = monitorInfo.rcWork;
                }
            }

            if (parentRect.Width > 0 && parentRect.Height > 0
                && PInvoke.GetWindowRect((HWND)ownedWindow, out var rect))
            {
                var left = parentRect.left + parentRect.Width / 2 - rect.Width / 2;
                var top = parentRect.top + parentRect.Height / 2 - rect.Height / 2;

                PInvoke.SetWindowPos((HWND)ownedWindow,
                    default,
                    left, top, 0, 0,
                    Windows.Win32.UI.WindowsAndMessaging.SET_WINDOW_POS_FLAGS.SWP_NOSIZE
                    | Windows.Win32.UI.WindowsAndMessaging.SET_WINDOW_POS_FLAGS.SWP_NOZORDER);
            }
        }

        internal class DialogWindowContext : IDisposable
        {
            private static Dictionary<nint, DialogWindowContext> dialogWindowContexts = new Dictionary<nint, DialogWindowContext>();

            private bool disposeValue;

            private WindowManager windowManager;
            private readonly ShowDialogOptions? options;
            private WindowMessageMonitor monitor;
            private HWND hWnd;
            private bool? result;
            private HWND activatedWindow;
            private TaskCompletionSource<bool?> resultTaskSource;
            private bool ownerWindowConnected;

            public DialogWindowContext(WindowManager windowManager, ShowDialogOptions? options, Windows.Win32.Foundation.HWND ownerHWnd)
            {
                this.windowManager = windowManager;
                this.options = options;
                OwnerHWnd = ownerHWnd;

                hWnd = windowManager.HWND;
                monitor = windowManager.GetMonitorInternal();

                lock (dialogWindowContexts)
                {
                    dialogWindowContexts.Add(windowManager.WindowHandle, this);
                }

                if (!OwnerHWnd.IsNull)
                {
                    var nonClientRegions = new Dictionary<NonClientRegionKind, RectInt32[]?>();
                    NonClientRegions = nonClientRegions;

                    // 缓存 inputNonClientPointerSource 防止 winui3 出现内部错误
                    var inputNonClientPointerSource = InputNonClientPointerSource.GetForWindowId(Win32Interop.GetWindowIdFromWindow(OwnerHWnd.Value));
                    InputNonClientPointerSource = inputNonClientPointerSource;

                    if (inputNonClientPointerSource != null)
                    {
                        nonClientRegions[NonClientRegionKind.Close] = inputNonClientPointerSource.GetRegionRects(NonClientRegionKind.Close);
                        nonClientRegions[NonClientRegionKind.Maximize] = inputNonClientPointerSource.GetRegionRects(NonClientRegionKind.Maximize);
                        nonClientRegions[NonClientRegionKind.Minimize] = inputNonClientPointerSource.GetRegionRects(NonClientRegionKind.Minimize);
                        nonClientRegions[NonClientRegionKind.Icon] = inputNonClientPointerSource.GetRegionRects(NonClientRegionKind.Icon);
                        nonClientRegions[NonClientRegionKind.Caption] = inputNonClientPointerSource.GetRegionRects(NonClientRegionKind.Caption);
                        nonClientRegions[NonClientRegionKind.TopBorder] = inputNonClientPointerSource.GetRegionRects(NonClientRegionKind.TopBorder);
                        nonClientRegions[NonClientRegionKind.LeftBorder] = inputNonClientPointerSource.GetRegionRects(NonClientRegionKind.LeftBorder);
                        nonClientRegions[NonClientRegionKind.BottomBorder] = inputNonClientPointerSource.GetRegionRects(NonClientRegionKind.BottomBorder);
                        nonClientRegions[NonClientRegionKind.RightBorder] = inputNonClientPointerSource.GetRegionRects(NonClientRegionKind.RightBorder);
                        nonClientRegions[NonClientRegionKind.Passthrough] = inputNonClientPointerSource.GetRegionRects(NonClientRegionKind.Passthrough);

                        inputNonClientPointerSource.ClearAllRegionRects();
                    }
                }

                ThreadWindows = GetThreadWindows();
                activatedWindow = PInvoke.GetActiveWindow();
                EnableThreadWindows(ThreadWindows, false);

                var capture = PInvoke.GetCapture();
                if (!capture.IsNull)
                {
                    PInvoke.SendMessage(capture, PInvoke.WM_CANCELMODE, 0, 0);
                    PInvoke.ReleaseCapture();
                }

                ConnectOwnerWindow();

                monitor.WindowMessageBeforeReceived += OnWindowMessageBeforeReceived;
                monitor.WindowMessageAfterReceived += OnWindowMessageAfterReceived;

                resultTaskSource = new TaskCompletionSource<bool?>();
            }

            public InputNonClientPointerSource? InputNonClientPointerSource { get; set; }

            public IReadOnlyDictionary<NonClientRegionKind, RectInt32[]?>? NonClientRegions { get; }

            public IReadOnlyList<HWND> ThreadWindows { get; private set; }

            public bool? Result
            {
                get => result;
                set
                {
                    this.result = value;

                    if (windowManager != null && PInvoke.IsWindow(hWnd))
                    {
                        if (!Closing
                            && !Closed)
                        {
                            PInvoke.SendMessage(
                                hWnd,
                                PInvoke.WM_SYSCOMMAND,
                                PInvoke.SC_CLOSE,
                                0);
                        }
                    }
                }
            }
            public bool Closing { get; set; }

            public bool Closed { get; set; }

            public WindowId Owner { get; }

            public HWND OwnerHWnd { get; }

            private void OnWindowMessageBeforeReceived(WindowManager sender, WindowMessageReceivedEventArgs e)
            {
                if (e.MessageId == PInvoke.WM_CLOSE)
                {
                    Closing = true;
                    DisconnectOwnerWindow();
                }
                else if (e.MessageId == PInvoke.WM_DESTROY)
                {
                    Closed = true;
                    Closing = false;

                    this.Dispose();
                }
                else if (_WM_MOVECENTER != 0 && e.MessageId == _WM_MOVECENTER)
                {
                    MoveWindowToCenter(OwnerHWnd.Value, sender.WindowHandle, options?.Location ?? DialogWindowStartupLocation.CenterScreen);
                }
            }

            private void OnWindowMessageAfterReceived(WindowManager sender, WindowMessageReceivedEventArgs e)
            {
                if (e.MessageId == PInvoke.WM_CLOSE)
                {
                    if (PInvoke.IsWindow(hWnd))
                    {
                        Closing = false;
                        ConnectOwnerWindow();
                    }
                }
            }

            public Task<bool?> GetResultAsync() => resultTaskSource.Task;

            private void ConnectOwnerWindow()
            {
                if (disposeValue || ownerWindowConnected) return;

                EnableThreadWindows(ThreadWindows, false);
                try
                {
                    if (PInvoke.IsWindow(hWnd))
                    {
                        if (!OwnerHWnd.IsNull)
                        {
                            PInvoke.SetWindowLongAuto(hWnd, Windows.Win32.UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX.GWL_HWNDPARENT, OwnerHWnd.Value);
                        }
                        PInvoke.SetActiveWindow(hWnd);
                    }
                }
                catch { }

                ownerWindowConnected = true;
            }

            private void DisconnectOwnerWindow()
            {
                if (disposeValue || !ownerWindowConnected) return;

                EnableThreadWindows(ThreadWindows, true);
                try
                {
                    if (PInvoke.IsWindow(hWnd))
                    {
                        PInvoke.SetWindowLongAuto(hWnd, Windows.Win32.UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX.GWL_HWNDPARENT, 0);
                    }
                }
                catch { }

                if (!activatedWindow.IsNull) PInvoke.SetActiveWindow(activatedWindow);
                else if (!OwnerHWnd.IsNull) PInvoke.SetActiveWindow(OwnerHWnd);

                ownerWindowConnected = false;
            }

            public void Dispose()
            {
                if (disposeValue) return;

                DisconnectOwnerWindow();
                disposeValue = true;

                lock (dialogWindowContexts)
                {
                    dialogWindowContexts.Remove(windowManager.WindowHandle);
                }

                monitor.WindowMessageBeforeReceived -= OnWindowMessageBeforeReceived;
                monitor.WindowMessageAfterReceived -= OnWindowMessageAfterReceived;

                ThreadWindows = Array.Empty<HWND>();

                if (!hWnd.IsNull) PInvoke.ShowWindow(hWnd, Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_HIDE);

                if (!activatedWindow.IsNull) PInvoke.SetActiveWindow(activatedWindow);
                else if (!OwnerHWnd.IsNull) PInvoke.SetActiveWindow(OwnerHWnd);

                if (InputNonClientPointerSource != null && NonClientRegions != null)
                {
                    foreach (var (kind, regions) in NonClientRegions)
                    {
                        InputNonClientPointerSource.ClearRegionRects(kind);
                        if (regions != null && regions.Length > 0)
                        {
                            InputNonClientPointerSource.SetRegionRects(kind, regions);
                        }
                    }
                }

                resultTaskSource.TrySetResult(Result);
            }

            public static bool TryGetContext(nint hwnd, [NotNullWhen(true)] out DialogWindowContext? context)
            {
                context = default;

                lock (dialogWindowContexts)
                {
                    if (dialogWindowContexts.TryGetValue(hwnd, out var value))
                    {
                        context = value;
                        return true;
                    }
                }

                return false;
            }
        }
    }

    public enum DialogWindowStartupLocation
    {
        CenterScreen = 1,
        CenterOwner = 2,
    }

    public record class ShowDialogOptions(WindowId OwnerWindowId, DialogWindowStartupLocation Location);
}
