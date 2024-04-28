using BlueFire.Toolkit.WinUI3.Icons;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32.Foundation;
using PInvoke = Windows.Win32.PInvoke;
using WinComposition = Windows.UI.Composition;

namespace BlueFire.Toolkit.WinUI3
{
    public sealed partial class WindowManager
    {
        private bool destroyed;
        private WindowBase.WindowMessageMonitor? messageMonitor;
        private AppWindow? appWindow;
        private WindowEx? windowExInternal;
        private WindowBase.WindowCompositionResources? compositionResources;
        private bool internalEnableWindowProc;

        private double minWidth;
        private double minHeight;
        private double maxWidth;
        private double maxHeight;
        private bool useDefaultIcon;
        private bool messageHandleInstalled;
        private uint dpi;
        private bool isActivated;
        private EventHandler? activatedStateChanged;

        private WindowManager(WindowId windowId)
        {
            HWND = new HWND(Win32Interop.GetWindowFromWindowId(windowId));
            if (!PInvoke.IsWindow(HWND)) throw new ArgumentException(null, nameof(windowId));

            WindowId = windowId;
            messageMonitor = new WindowBase.WindowMessageMonitor(this);
        }

        internal HWND HWND { get; }

        internal WindowEx? WindowExInternal
        {
            get => windowExInternal;
            set
            {
                if (windowExInternal != value)
                {
                    windowExInternal = value;
                    UpdateWindowProc();
                }
            }
        }

        internal bool InternalEnableWindowProc
        {
            get => internalEnableWindowProc;
            set
            {
                if (internalEnableWindowProc != value)
                {
                    internalEnableWindowProc = value;
                    UpdateWindowProc();
                }
            }
        }

        internal bool IsForegroundWindow => messageHandleInstalled ? isActivated : PInvoke.GetForegroundWindow() == HWND;

        public WindowId WindowId { get; }

        public AppWindow? AppWindow => !destroyed ? (appWindow ??= AppWindow.GetFromWindowId(WindowId)) : null;

        public nint WindowHandle => HWND.Value;

        /// <summary>
        /// Set window icon to application default icon.
        /// </summary>
        public bool UseDefaultIcon
        {
            get => useDefaultIcon;
            set
            {
                if (useDefaultIcon != value)
                {
                    useDefaultIcon = value;
                    if (!destroyed)
                    {
                        if (useDefaultIcon)
                        {
                            SetDefaultIcon(WindowId, false, WindowDpi);
                        }
                        else
                        {
                            RemoveIcon(WindowId);
                        }
                        UpdateWindowProc();
                    }
                }
            }
        }

        public uint WindowDpi
        {
            get
            {
                if (!messageHandleInstalled || dpi == 0)
                {
                    var _dpi = PInvoke.GetDpiForWindow(HWND);
                    if (messageHandleInstalled) dpi = _dpi;
                    return _dpi;
                }

                return dpi;
            }
        }

        internal WindowBase.WindowMessageMonitor GetMonitorInternal()
        {
            if (destroyed) throw new ObjectDisposedException(nameof(WindowManager));
            return messageMonitor!;
        }

        public event WindowMessageReceivedEventHandler? WindowMessageReceived
        {
            add
            {
                messageMonitor!.WindowMessageReceived += value;
                UpdateWindowProc();
            }
            remove
            {
                messageMonitor!.WindowMessageReceived -= value;
                UpdateWindowProc();
            }
        }

        public event EventHandler? ActivatedStateChanged
        {
            add
            {
                activatedStateChanged += value;
                UpdateWindowProc();
            }
            remove
            {
                activatedStateChanged -= value;
                UpdateWindowProc();
            }
        }

        public double MinWidth
        {
            get => minWidth;
            set
            {
                if (minWidth != value)
                {
                    minWidth = value;
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
                    RefreshWindowSize();
                }
            }
        }

        private void RefreshWindowSize()
        {
            if (!destroyed && PInvoke.GetWindowRect(HWND, out var rect))
            {
                PInvoke.SetWindowPos(HWND, default, rect.X, rect.Y, rect.Width, rect.Height,
                    global::Windows.Win32.UI.WindowsAndMessaging.SET_WINDOW_POS_FLAGS.SWP_NOZORDER);

                UpdateWindowProc();
            }
        }

        internal WinComposition.SpriteVisual WindowContentVisual => EnsureCompositionResources().WindowContentVisual;

        internal WinComposition.SpriteVisual BackdropVisual => EnsureCompositionResources().BackdropVisual;

        private WindowBase.WindowCompositionResources EnsureCompositionResources()
        {
            if (destroyed) throw new ObjectDisposedException(nameof(WindowManager));

            if (compositionResources == null)
            {
                compositionResources = new WindowBase.WindowCompositionResources(this);
            }

            return compositionResources;
        }

        private void UpdateWindowProc()
        {
            if (destroyed) return;

            var flag = false;
            if (useDefaultIcon) flag = true;

            if (!flag) flag = InternalEnableWindowProc;

            if (!flag) flag = WindowExInternal != null;

            if (!flag)
            {
                if (minWidth != 0 || minHeight != 0 || maxWidth != 0 || maxHeight != 0)
                {
                    flag = true;
                }
            }

            if (!flag) flag = GetMonitorInternal().MessageReceivedEventHandled;

            if (!flag) flag = activatedStateChanged != null;

            if (messageHandleInstalled != flag)
            {
                messageHandleInstalled = flag;
                GetMonitorInternal().WindowMessageBeforeReceived -= OverrideWindowProc;
                dpi = 0;

                if (flag)
                {
                    GetMonitorInternal().WindowMessageBeforeReceived += OverrideWindowProc;
                }
            }
        }

        private unsafe void OverrideWindowProc(WindowManager sender, WindowMessageReceivedEventArgs e)
        {
            if (destroyed) return;

            if (e.MessageId == PInvoke.WM_GETMINMAXINFO)
            {
                ref var info = ref Unsafe.AsRef<Windows.Win32.UI.WindowsAndMessaging.MINMAXINFO>((void*)e.LParam);

                var dpi = WindowDpi;

                var minWidthPixel = (int)(minWidth * dpi / 96);
                var minHeightPixel = (int)(minHeight * dpi / 96);

                var maxWidthPixel = (int)(maxWidth * dpi / 96);
                var maxHeightPixel = (int)(maxHeight * dpi / 96);

                if (maxWidthPixel == 0) maxWidthPixel = PInvoke.GetSystemMetrics(Windows.Win32.UI.WindowsAndMessaging.SYSTEM_METRICS_INDEX.SM_CXMAXTRACK);
                if (maxHeightPixel == 0) maxHeightPixel = PInvoke.GetSystemMetrics(Windows.Win32.UI.WindowsAndMessaging.SYSTEM_METRICS_INDEX.SM_CYMAXTRACK);

                info.ptMinTrackSize = new System.Drawing.Point(minWidthPixel, minHeightPixel);
                info.ptMaxTrackSize = new System.Drawing.Point(maxWidthPixel, maxHeightPixel);

                e.Handled = true;
                e.LResult = 0;
            }
            else if (e.MessageId == PInvoke.WM_DPICHANGED)
            {
                dpi = 0;
                SetDefaultIcon(WindowId, false, WindowDpi);
            }
            else if (e.MessageId == PInvoke.WM_ACTIVATE)
            {
                isActivated = unchecked((ushort)e.WParam) != 0;
                activatedStateChanged?.Invoke(this, EventArgs.Empty);
            }
            else if (e.MessageId == PInvoke.WM_CREATE)
            {
                SetDefaultIcon(WindowId, false, WindowDpi);
            }
            else if (e.MessageId == PInvoke.WM_DESTROY)
            {
                OnDestroy();
            }
        }

        private void OnDestroy()
        {
            if (!destroyed)
            {
                destroyed = true;

                OnWindowDestroy(WindowId);
                if (messageMonitor != null)
                {
                    messageMonitor.WindowMessageBeforeReceived -= OverrideWindowProc;
                    messageMonitor.Dispose();
                    messageMonitor = null!;
                }

                if (compositionResources != null)
                {
                    compositionResources.Dispose();
                    compositionResources = null;
                }

                RemoveIcon(WindowId);
            }
        }

        public WindowStyleFlags WindowStyle
        {
            get => unchecked((WindowStyleFlags)PInvoke.GetWindowLongAuto(HWND, Windows.Win32.UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX.GWL_STYLE));
            set => PInvoke.SetWindowLongAuto(HWND, Windows.Win32.UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX.GWL_STYLE, unchecked((nint)(uint)value));
        }

        public WindowExStyleFlags WindowExStyle
        {
            get => unchecked((WindowExStyleFlags)(uint)PInvoke.GetWindowLongAuto(HWND, Windows.Win32.UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE));
            set => PInvoke.SetWindowLongAuto(HWND, Windows.Win32.UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, unchecked((nint)(uint)value));
        }

        public void RedrawFrame()
        {
            Windows.Win32.PInvoke.SetWindowPos(HWND, default, 0, 0, 0, 0,
                Windows.Win32.UI.WindowsAndMessaging.SET_WINDOW_POS_FLAGS.SWP_NOMOVE
                | Windows.Win32.UI.WindowsAndMessaging.SET_WINDOW_POS_FLAGS.SWP_NOSIZE
                | Windows.Win32.UI.WindowsAndMessaging.SET_WINDOW_POS_FLAGS.SWP_NOZORDER
                | Windows.Win32.UI.WindowsAndMessaging.SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED);
        }

        [Flags]
        public enum WindowStyleFlags : uint
        {
            WS_OVERLAPPED = 0x00000000,
            WS_POPUP = 0x80000000,
            WS_CHILD = 0x40000000,
            WS_MINIMIZE = 0x20000000,
            WS_VISIBLE = 0x10000000,
            WS_DISABLED = 0x08000000,
            WS_CLIPSIBLINGS = 0x04000000,
            WS_CLIPCHILDREN = 0x02000000,
            WS_MAXIMIZE = 0x01000000,
            WS_CAPTION = 0x00C00000,
            WS_BORDER = 0x00800000,
            WS_DLGFRAME = 0x00400000,
            WS_VSCROLL = 0x00200000,
            WS_HSCROLL = 0x00100000,
            WS_SYSMENU = 0x00080000,
            WS_THICKFRAME = 0x00040000,
            WS_GROUP = 0x00020000,
            WS_TABSTOP = 0x00010000,
            WS_MINIMIZEBOX = 0x00020000,
            WS_MAXIMIZEBOX = 0x00010000,
            WS_TILED = 0x00000000,
            WS_ICONIC = 0x20000000,
            WS_SIZEBOX = 0x00040000,
            WS_TILEDWINDOW = 0x00CF0000,
            WS_OVERLAPPEDWINDOW = 0x00CF0000,
            WS_POPUPWINDOW = 0x80880000,
            WS_CHILDWINDOW = 0x40000000,
            WS_ACTIVECAPTION = 0x00000001,
        }


        [Flags]
        public enum WindowExStyleFlags : uint
        {
            WS_EX_DLGMODALFRAME = 0x00000001,
            WS_EX_NOPARENTNOTIFY = 0x00000004,
            WS_EX_TOPMOST = 0x00000008,
            WS_EX_ACCEPTFILES = 0x00000010,
            WS_EX_TRANSPARENT = 0x00000020,
            WS_EX_MDICHILD = 0x00000040,
            WS_EX_TOOLWINDOW = 0x00000080,
            WS_EX_WINDOWEDGE = 0x00000100,
            WS_EX_CLIENTEDGE = 0x00000200,
            WS_EX_CONTEXTHELP = 0x00000400,
            WS_EX_RIGHT = 0x00001000,
            WS_EX_LEFT = 0x00000000,
            WS_EX_RTLREADING = 0x00002000,
            WS_EX_LTRREADING = 0x00000000,
            WS_EX_LEFTSCROLLBAR = 0x00004000,
            WS_EX_RIGHTSCROLLBAR = 0x00000000,
            WS_EX_CONTROLPARENT = 0x00010000,
            WS_EX_STATICEDGE = 0x00020000,
            WS_EX_APPWINDOW = 0x00040000,
            WS_EX_OVERLAPPEDWINDOW = 0x00000300,
            WS_EX_PALETTEWINDOW = 0x00000188,
            WS_EX_LAYERED = 0x00080000,
            WS_EX_NOINHERITLAYOUT = 0x00100000,
            WS_EX_NOREDIRECTIONBITMAP = 0x00200000,
            WS_EX_LAYOUTRTL = 0x00400000,
            WS_EX_COMPOSITED = 0x02000000,
            WS_EX_NOACTIVATE = 0x08000000,
        }
    }
}
