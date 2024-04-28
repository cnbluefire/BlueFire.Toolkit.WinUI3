using BlueFire.Toolkit.WinUI3.WindowBase;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics;
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
        /// <param name="owner">Sets the window that owns this Window.</param>
        /// <returns></returns>
        public static async Task<bool?> ShowDialogAsync(this AppWindow window, WindowId owner)
        {
            var manager = WindowManager.Get(window);
            if (manager == null) return null;

            var hwnd = new Windows.Win32.Foundation.HWND((nint)window.Id.Value);
            var ownerHwnd = EnsureOwnerWindow(owner);

            if (!PInvoke.IsWindowEnabled(ownerHwnd)) return null;

            var context = new DialogWindowContext(manager, ownerHwnd);

            try
            {
                if (window.Presenter is not OverlappedPresenter presenter)
                {
                    presenter = OverlappedPresenter.CreateForDialog();
                    window.SetPresenter(presenter);
                }

                // 无需设置 presenter.IsModal 因为随后会禁用UI线程所有窗口
                presenter.IsMinimizable = false;

                MoveWindowToCenter(ownerHwnd.Value, hwnd.Value);

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

        internal class DialogWindowContext : IDisposable
        {
            private static Dictionary<nint, DialogWindowContext> dialogWindowContexts = new Dictionary<nint, DialogWindowContext>();

            private bool disposeValue;

            private WindowManager windowManager;
            private bool? result;
            private HWND activatedWindow;
            private TaskCompletionSource<bool?> resultTaskSource;

            public DialogWindowContext(WindowManager windowManager, HWND ownerHWnd)
            {
                this.windowManager = windowManager;
                OwnerHWnd = ownerHWnd;

                if (!OwnerHWnd.IsNull)
                {
                    lock (dialogWindowContexts)
                    {
                        dialogWindowContexts.Add(windowManager.WindowHandle, this);
                    }

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

                var monitor = windowManager.GetMonitorInternal();

                if (monitor != null)
                {
                    monitor.WindowMessageBeforeReceived += OnWindowMessageBeforeReceived;
                    monitor.WindowMessageAfterReceived += OnWindowMessageAfterReceived;
                }

                PInvoke.SetWindowLongAuto(windowManager.HWND, Windows.Win32.UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX.GWL_HWNDPARENT, OwnerHWnd);

                resultTaskSource = new TaskCompletionSource<bool?>();
            }

            public InputNonClientPointerSource? InputNonClientPointerSource { get; set; }

            public IReadOnlyDictionary<NonClientRegionKind, RectInt32[]?>? NonClientRegions { get; }

            public IReadOnlyList<HWND>? ThreadWindows { get; private set; }

            public bool? Result
            {
                get => result;
                set
                {
                    this.result = value;

                    if (windowManager != null && PInvoke.IsWindow(windowManager.HWND))
                    {
                        if (!Closing
                            && !Closed)
                        {
                            PInvoke.SendMessage(
                                windowManager.HWND,
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
                }
                else if (e.MessageId == PInvoke.WM_DESTROY)
                {
                    Closed = true;
                    Closing = false;

                    PInvoke.SetWindowLongAuto(sender.HWND, Windows.Win32.UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX.GWL_HWNDPARENT, default);
                    this.Dispose();
                }
                else if (_WM_MOVECENTER != 0 && e.MessageId == _WM_MOVECENTER)
                {
                    MoveWindowToCenter(OwnerHWnd.Value, sender.WindowHandle);
                }
            }

            private void OnWindowMessageAfterReceived(WindowManager sender, WindowMessageReceivedEventArgs e)
            {
                if (e.MessageId == PInvoke.WM_CLOSE)
                {
                    if (!e.Handled)
                    {
                        Closing = false;
                        Closed = true;
                        this.Dispose();
                    }
                }
            }

            public Task<bool?> GetResultAsync() => resultTaskSource.Task;

            public void Dispose()
            {
                if (disposeValue) return;
                disposeValue = true;

                Windows.Win32.Foundation.HWND hWnd = default;

                if (windowManager != null)
                {
                    hWnd = windowManager.HWND;
                    lock (dialogWindowContexts)
                    {
                        dialogWindowContexts.Remove(windowManager.WindowHandle);
                    }

                    PInvoke.SetWindowLongAuto(windowManager.HWND, Windows.Win32.UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX.GWL_HWNDPARENT, default);

                    var monitor = windowManager.GetMonitorInternal();

                    if (monitor != null)
                    {
                        monitor.WindowMessageBeforeReceived -= OnWindowMessageBeforeReceived;
                        monitor.WindowMessageAfterReceived -= OnWindowMessageAfterReceived;
                    }
                }

                if (ThreadWindows != null)
                {
                    EnableThreadWindows(ThreadWindows, true);
                    ThreadWindows = null;
                }

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
}
