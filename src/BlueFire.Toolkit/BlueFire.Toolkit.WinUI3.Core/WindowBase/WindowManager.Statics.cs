using BlueFire.Toolkit.WinUI3.Icons;
using Microsoft.UI;
using Microsoft.UI.Content;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32.Foundation;
using PInvoke = Windows.Win32.PInvoke;

namespace BlueFire.Toolkit.WinUI3
{
    partial class WindowManager
    {
        private static Dictionary<WindowId, WindowManager> windowManagers = new Dictionary<WindowId, WindowManager>();

        public static WindowManager? Get(WindowId windowId)
        {
            if (windowId.Value == 0) return null;

            lock (windowManagers)
            {
                if (windowManagers.TryGetValue(windowId, out var manager)) return manager;

                var hWnd = Win32Interop.GetWindowFromWindowId(windowId);
                if (!PInvoke.IsWindow(new HWND(hWnd))) return null;

                manager = new WindowManager(windowId);
                windowManagers[windowId] = manager;

                return manager;
            }
        }

        public static WindowManager? Get(AppWindow appWindow) => Get(appWindow.Id);

        public static WindowManager? Get(Window window) => Get(window.AppWindow.Id);

        public static WindowManager? Get(XamlRoot xamlRoot) => Get(xamlRoot.ContentIslandEnvironment.AppWindowId);

        public static unsafe IReadOnlyList<WindowId> TryGetAllWindowIds()
        {
            var charArray = ArrayPool<char>.Shared.Rent(256);

            try
            {
                fixed (char* _pChar = &charArray[0])
                {
                    var pChar = _pChar;
                    var str = new PWSTR(pChar);

                    var hWndArray = PInvoke.EnumThreadWindows((_hWnd, _) =>
                    {
                        var length = PInvoke.GetClassName(_hWnd, str, 255);

                        if (length > 0)
                        {
                            var className = new string(pChar, 0, length);
                            
                            return className == "Microsoft.UI.Windowing.Window" 
                                || className == "WinUIDesktopWin32WindowClass";
                        }

                        return false;
                    }, 0);

                    return hWndArray?
                        .Select(c => Win32Interop.GetWindowIdFromWindow(c.Value))
                        .ToArray() ?? Array.Empty<WindowId>();
                }
            }
            finally
            {
                ArrayPool<char>.Shared.Return(charArray);
            }
        }

        private static void OnWindowDestroy(WindowId windowId)
        {
            lock (windowManagers)
            {
                windowManagers.Remove(windowId);
            }
        }

        #region Window Icon

        private static void SetDefaultIcon(WindowId windowId, bool useDarkMode, uint dpi)
        {
            if (windowId.Value == 0) return;
            var hWnd = Microsoft.UI.Win32Interop.GetWindowFromWindowId(windowId);

            SetIcon(hWnd,
                DefaultIconProvider.Instance.GetSharedLargeIcon(dpi, useDarkMode ? ApplicationTheme.Dark : ApplicationTheme.Light, false),
                DefaultIconProvider.Instance.GetSharedSmallIcon(dpi, useDarkMode ? ApplicationTheme.Dark : ApplicationTheme.Light, false));
        }

        private static void RemoveIcon(WindowId windowId)
        {
            if (windowId.Value == 0) return;
            var hWnd = Win32Interop.GetWindowFromWindowId(windowId);

            SetIcon(hWnd, new IconId(0), new IconId(0));
        }

        private static void SetIcon(nint hWnd, IconId? bigIconId, IconId? smallIconId)
        {
            if (hWnd == IntPtr.Zero) return;
            if (!bigIconId.HasValue && !smallIconId.HasValue) return;

            if (bigIconId.HasValue)
            {
                var icon = Win32Interop.GetIconFromIconId(bigIconId.Value);
                PInvoke.SendMessage(new Windows.Win32.Foundation.HWND(hWnd), PInvoke.WM_SETICON, PInvoke.ICON_BIG, icon);
            }

            if (smallIconId.HasValue)
            {
                var icon = Win32Interop.GetIconFromIconId(smallIconId.Value);
                PInvoke.SendMessage(new Windows.Win32.Foundation.HWND(hWnd), PInvoke.WM_SETICON, PInvoke.ICON_SMALL, icon);
            }
        }


        #endregion Window Icon
    }
}
