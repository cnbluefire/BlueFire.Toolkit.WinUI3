using BlueFire.Toolkit.Extensions;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace BlueFire.Toolkit.Graphics
{
    /// <summary>
    ///  Represents a display device or multiple display devices on a single system.
    /// </summary>
    public partial class Screen
    {
        private static readonly RECT RectInt32Empty = new RECT(0, 0, 0, 0);

        private readonly HMONITOR _hmonitor;

        /// <summary>
        ///  Bounds of the screen
        /// </summary>
        private readonly RECT _bounds;

        /// <summary>
        ///  Available working area on the screen. This excludes taskbars and other docked windows.
        /// </summary>
        private RECT _workingArea = RectInt32Empty;

        /// <summary>
        ///  Set to true if this screen is the primary monitor
        /// </summary>
        private readonly bool _primary;

        /// <summary>
        ///  Device name associated with this monitor
        /// </summary>
        private readonly string _deviceName;

        private readonly int _bitDepth;

        private static readonly Lock s_syncLock = new(); // used to lock this class before syncing to SystemEvents

        private static int s_desktopChangedCount = -1; // static counter of desktop size changes

        private int _currentDesktopChangedCount = -1; // instance-based counter used to invalidate WorkingArea

        private uint _dpiX;
        private uint _dpiY;

        private static Screen[]? s_screens;

        internal Screen(HMONITOR monitor) : this(monitor, default)
        {
        }

        internal unsafe Screen(HMONITOR monitor, HDC hdc)
        {
            HDC screenDC = hdc;
            // Multiple monitor system
            MONITORINFOEXW info = new()
            {
                monitorInfo = new() { cbSize = (uint)sizeof(MONITORINFOEXW) }
            };

            // API takes EX, determines which one you pass by size.
            PInvoke.GetMonitorInfo(monitor, (MONITORINFO*)&info);
            _bounds = info.monitorInfo.rcMonitor;
            _primary = ((info.monitorInfo.dwFlags & PInvoke.MONITORINFOF_PRIMARY) != 0);

            _deviceName = new string(info.szDevice.ToString());

            if (hdc == 0)
            {
                screenDC = PInvoke.CreateDCW(_deviceName, pwszDevice: null, pszPort: null, pdm: null);
            }

            _hmonitor = monitor;

            _bitDepth = PInvoke.GetDeviceCaps(screenDC, GET_DEVICE_CAPS_INDEX.BITSPIXEL);
            _bitDepth *= PInvoke.GetDeviceCaps(screenDC, GET_DEVICE_CAPS_INDEX.PLANES);

            if (hdc != screenDC)
            {
                PInvoke.DeleteDC(screenDC);
            }

            var hr = PInvoke.GetDpiForMonitor(monitor, Windows.Win32.UI.HiDpi.MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out _dpiX, out _dpiY);
            if (hr.Failed)
            {
                _dpiX = 96;
                _dpiY = 96;
            }
        }

        /// <summary>
        ///  Gets an array of all of the displays on the system.
        /// </summary>
        public static unsafe Screen[] AllScreens
        {
            get
            {
                if (s_screens is null)
                {
                    List<Screen> screens = [];

                    MonitorHelper.EnumDisplayMonitors((HMONITOR hmonitor, HDC hdc) =>
                    {
                        screens.Add(new(hmonitor, hdc));
                        return true;
                    });

                    s_screens = screens.Count > 0 ? [.. screens] : [];

                    // Now that we have our screens, attach a display setting changed
                    // event so that we know when to invalidate them.
                    SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
                }

                return s_screens;
            }
        }

        /// <summary>
        ///  Gets Bits per Pixel value.
        /// </summary>
        public int BitsPerPixel => _bitDepth;

        /// <summary>
        ///  Gets the bounds of the display.
        /// </summary>
        public RectInt32 Bounds => new RectInt32(_bounds.X, _bounds.Y, _bounds.Width, _bounds.Height);

        /// <summary>
        ///  Gets the device name associated with a display.
        /// </summary>
        public string DeviceName => _deviceName;

        /// <summary>
        ///  Gets a value indicating whether a particular display is the primary device.
        /// </summary>
        public bool Primary => _primary;

        public nint HMonitor => _hmonitor.Value;

        public uint DpiX => _dpiX;

        public uint DpiY => _dpiY;

        /// <summary>
        ///  Gets the primary display.
        /// </summary>
        public static Screen? PrimaryScreen
        {
            get
            {
                Screen[] screens = AllScreens;
                for (int i = 0; i < screens.Length; i++)
                {
                    if (screens[i]._primary)
                    {
                        return screens[i];
                    }
                }

                return null;
            }
        }

        /// <summary>
        ///  Gets the working area of the screen.
        /// </summary>
        public unsafe RectInt32 WorkingArea
        {
            get
            {
                // if the static Screen class has a different desktop change count
                // than this instance then update the count and recalculate our working area
                if (_currentDesktopChangedCount != DesktopChangedCount)
                {
                    Interlocked.Exchange(ref _currentDesktopChangedCount, DesktopChangedCount);

                    // Multiple monitor System
                    MONITORINFOEXW info = new()
                    {
                        monitorInfo = new() { cbSize = (uint)sizeof(MONITORINFOEXW) }
                    };

                    // API takes EX, determines which one you pass by size.
                    PInvoke.GetMonitorInfo(_hmonitor, (MONITORINFO*)&info);
                    _workingArea = info.monitorInfo.rcWork;
                }

                return new RectInt32(_workingArea.X, _workingArea.Y, _workingArea.Width, _workingArea.Height);
            }
        }

        /// <summary>
        ///  Screen instances call this property to determine if their WorkingArea cache needs to be invalidated.
        /// </summary>
        private static int DesktopChangedCount
        {
            get
            {
                if (s_desktopChangedCount == -1)
                {
                    lock (s_syncLock)
                    {
                        // Now that we have a lock, verify (again) our changecount.
                        if (s_desktopChangedCount == -1)
                        {
                            // Sync the UserPreference.Desktop change event. We'll keep count of desktop changes so that
                            // the WorkingArea property on Screen instances know when to invalidate their cache.
                            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

                            s_desktopChangedCount = 0;
                        }
                    }
                }

                return s_desktopChangedCount;
            }
        }

        /// <summary>
        ///  Specifies a value that indicates whether the specified object is equal to this one.
        /// </summary>
        public override bool Equals(object? obj) => obj is Screen comp && _hmonitor == comp._hmonitor;

        public static Screen? FromMonitor(nint monitor)
            => MonitorHelper.IsMonitor((HMONITOR)monitor) ?
                new Screen((HMONITOR)monitor) : null;

        /// <summary>
        ///  Retrieves a <see cref="Screen"/> for the monitor that contains the specified point.
        /// </summary>
        public static Screen FromPoint(PointInt32 point)
            => new Screen(PInvoke.MonitorFromPoint(new System.Drawing.Point(point.X, point.Y), MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST));

        /// <summary>
        ///  Retrieves a <see cref="Screen"/> for the monitor that contains the largest region of the rectangle.
        /// </summary>
        public static Screen FromRectangle(RectInt32 rect)
            => new Screen(PInvoke.MonitorFromRect(new System.Drawing.Rectangle(rect.X, rect.Y, rect.Width, rect.Height), MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST));

        /// <summary>
        ///  Retrieves a <see cref="Screen"/> for the monitor that contains the largest region of the window.
        /// </summary>
        public static Screen FromHandle(IntPtr hwnd)
            => new Screen(PInvoke.MonitorFromWindow((HWND)hwnd, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST));

        /// <summary>
        ///  Retrieves the working area for the monitor that is closest to the specified point.
        /// </summary>
        public static RectInt32 GetWorkingArea(PointInt32 pt) => FromPoint(pt).WorkingArea;

        /// <summary>
        ///  Retrieves the working area for the monitor that contains the largest region of the specified rectangle.
        /// </summary>
        public static RectInt32 GetWorkingArea(RectInt32 rect) => FromRectangle(rect).WorkingArea;

        /// <summary>
        ///  Retrieves the bounds of the monitor that is closest to the specified point.
        /// </summary>
        public static RectInt32 GetBounds(PointInt32 pt) => FromPoint(pt).Bounds;

        /// <summary>
        ///  Retrieves the bounds of the monitor that contains the largest region of the specified rectangle.
        /// </summary>
        public static RectInt32 GetBounds(RectInt32 rect) => FromRectangle(rect).Bounds;

        /// <summary>
        ///  Computes and retrieves a hash code for an object.
        /// </summary>
        public override int GetHashCode() => unchecked((int)_hmonitor.Value);

        /// <summary>
        ///  Called by the SystemEvents class when our display settings are changing. We cache screen information and
        ///  at this point we must invalidate our cache.
        /// </summary>
        private static void OnDisplaySettingsChanged(object? sender, EventArgs e)
        {
            // Now that we've responded to this event, we don't need it again until
            // someone re-queries. We will re-add the event at that time.
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;

            // Display settings changed, so the set of screens we have is invalid.
            s_screens = null;
        }

        /// <summary>
        ///  Called by the SystemEvents class when our display settings have changed. Here, we increment a static counter
        ///  that Screen instances can check against to invalidate their cache.
        /// </summary>
        private static void OnUserPreferenceChanged(object? sender, SystemEvents.UserPreferenceChangedEventArgs e)
        {
            if (e.Category == SystemEvents.UserPreferenceCategory.Desktop)
            {
                Interlocked.Increment(ref s_desktopChangedCount);
            }
        }

        /// <summary>
        ///  Retrieves a string representing this object.
        /// </summary>
        public override string ToString()
            => $"{GetType().Name}[Bounds={_bounds} WorkingArea={WorkingArea} Primary={_primary} DeviceName={_deviceName}";

        private static class MonitorHelper
        {
            public delegate bool EnumDisplayMonitorsCallback(HMONITOR monitor, HDC hdc);

            public unsafe static BOOL EnumDisplayMonitors(EnumDisplayMonitorsCallback callBack)
            {
                GCHandle gcHandle = GCHandle.Alloc(callBack);
                try
                {
                    return Windows.Win32.PInvoke.EnumDisplayMonitors(default, (RECT*)null, &EnumDisplayMonitorsNativeCallback, (LPARAM)(nint)gcHandle);
                }
                finally
                {
                    gcHandle.Free();
                }
            }

            public static bool IsMonitor(HMONITOR monitor)
            {
                var hr = PInvoke.GetDpiForMonitor(monitor, Windows.Win32.UI.HiDpi.MONITOR_DPI_TYPE.MDT_DEFAULT, out _, out _);
                return hr.Succeeded;
            }

            /// <summary>
            ///  Gets the size of the working area in pixels.
            /// </summary>
            public static RECT WorkingArea
            {
                get
                {
                    unsafe
                    {
                        RECT workingArea = default;
                        PInvoke.SystemParametersInfo(Windows.Win32.UI.WindowsAndMessaging.SYSTEM_PARAMETERS_INFO_ACTION.SPI_GETWORKAREA, 0, &workingArea, 0); ;
                        return workingArea;
                    }
                }
            }

            /// <summary>
            ///  Gets the bounds of the virtual screen.
            /// </summary>
            public static RECT VirtualScreen
            {
                get
                {
                    if (MultiMonitorSupport)
                    {
                        return new(PInvoke.GetSystemMetrics(Windows.Win32.UI.WindowsAndMessaging.SYSTEM_METRICS_INDEX.SM_XVIRTUALSCREEN),
                            PInvoke.GetSystemMetrics(Windows.Win32.UI.WindowsAndMessaging.SYSTEM_METRICS_INDEX.SM_YVIRTUALSCREEN),
                            PInvoke.GetSystemMetrics(Windows.Win32.UI.WindowsAndMessaging.SYSTEM_METRICS_INDEX.SM_CXVIRTUALSCREEN),
                            PInvoke.GetSystemMetrics(Windows.Win32.UI.WindowsAndMessaging.SYSTEM_METRICS_INDEX.SM_CYVIRTUALSCREEN));
                    }

                    var size = PrimaryMonitorSize;
                    return new RECT(0, 0, size.Width, size.Height);
                }
            }

            public static bool MultiMonitorSupport
                => PInvoke.GetSystemMetrics(Windows.Win32.UI.WindowsAndMessaging.SYSTEM_METRICS_INDEX.SM_CMONITORS) != 0;

            public static SIZE PrimaryMonitorSize
                => new SIZE(
                    PInvoke.GetSystemMetrics(Windows.Win32.UI.WindowsAndMessaging.SYSTEM_METRICS_INDEX.SM_CXSCREEN),
                    PInvoke.GetSystemMetrics(Windows.Win32.UI.WindowsAndMessaging.SYSTEM_METRICS_INDEX.SM_CYSCREEN));

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
            private unsafe static BOOL EnumDisplayMonitorsNativeCallback(HMONITOR monitor, HDC hdc, RECT* lprcMonitor, LPARAM lParam)
            {
                return ((EnumDisplayMonitorsCallback)((GCHandle)(nint)lParam).Target!)(monitor, hdc);
            }
        }
    }
}
