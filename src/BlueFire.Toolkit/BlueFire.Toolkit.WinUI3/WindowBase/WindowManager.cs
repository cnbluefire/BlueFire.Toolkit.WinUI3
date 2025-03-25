using BlueFire.Toolkit.WindowBase;
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
        private WindowMessageListener messageListener;
        private WindowMaxMinSizeFeature maxMinSizeFeature;
        private AppWindow? appWindow;
        private WindowEx? windowExInternal;
        private WindowBase.WindowCompositionResources? compositionResources;
        private bool internalEnableWindowProc;

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

            messageListener = WindowMessageListener.TryCreate(HWND);
            ArgumentNullException.ThrowIfNull(messageListener);

            maxMinSizeFeature = new WindowMaxMinSizeFeature();
            messageListener.AddFeature(maxMinSizeFeature);
            messageListener.MessageReceived += OverrideWindowProc;
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

        internal WindowMessageListener GetListenerInternal()
        {
            if (destroyed) throw new ObjectDisposedException(nameof(WindowManager));
            ArgumentNullException.ThrowIfNull(messageListener);
            return messageListener;
        }

        public event WindowMessageReceivedEventHandler? WindowMessageReceived;

        public event EventHandler? ActivatedStateChanged
        {
            add
            {
                activatedStateChanged += value;
            }
            remove
            {
                activatedStateChanged -= value;
            }
        }

        public double MinWidth
        {
            get
            {
                ArgumentNullException.ThrowIfNull(maxMinSizeFeature);
                return maxMinSizeFeature.MinWidth * 96d / WindowDpi;
            }
            set
            {
                ArgumentNullException.ThrowIfNull(maxMinSizeFeature);
                maxMinSizeFeature.MinWidth = (int)(value * WindowDpi / 96);
            }
        }

        public double MinHeight
        {
            get
            {
                ArgumentNullException.ThrowIfNull(maxMinSizeFeature);
                return maxMinSizeFeature.MinHeight * 96d / WindowDpi;
            }
            set
            {
                ArgumentNullException.ThrowIfNull(maxMinSizeFeature);
                maxMinSizeFeature.MinHeight = (int)(value * WindowDpi / 96);
            }
        }

        public double MaxWidth
        {
            get
            {
                ArgumentNullException.ThrowIfNull(maxMinSizeFeature);
                return maxMinSizeFeature.MaxWidth * 96d / WindowDpi;
            }
            set
            {
                ArgumentNullException.ThrowIfNull(maxMinSizeFeature);
                maxMinSizeFeature.MaxWidth = (int)(value * WindowDpi / 96);
            }
        }

        public double MaxHeight
        {
            get
            {
                ArgumentNullException.ThrowIfNull(maxMinSizeFeature);
                return maxMinSizeFeature.MaxHeight * 96d / WindowDpi;
            }
            set
            {
                ArgumentNullException.ThrowIfNull(maxMinSizeFeature);
                maxMinSizeFeature.MaxHeight = (int)(value * WindowDpi / 96);
            }
        }

        private void RefreshWindowSize()
        {
            if (!destroyed && PInvoke.GetWindowRect(HWND, out var rect))
            {
                PInvoke.SetWindowPos(HWND, default, rect.X, rect.Y, rect.Width, rect.Height,
                    global::Windows.Win32.UI.WindowsAndMessaging.SET_WINDOW_POS_FLAGS.SWP_NOZORDER);
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

        private unsafe void OverrideWindowProc(object? sender, WindowMessageReceivedEventArgs e)
        {
            if (destroyed) return;

            if (e.MessageId == PInvoke.WM_DPICHANGED)
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

            this.WindowMessageReceived?.Invoke(this, e);
        }

        private void OnDestroy()
        {
            if (!destroyed)
            {
                destroyed = true;

                OnWindowDestroy(WindowId);
                if (messageListener != null)
                {
                    messageListener.MessageReceived -= OverrideWindowProc;
                    messageListener.Dispose();
                    messageListener = null!;
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
