using BlueFire.Toolkit.WinUI3.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinRT;
using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using BlueFire.Toolkit.WinUI3.Icons;
using PInvoke = Windows.Win32.PInvoke;

namespace BlueFire.Toolkit.WinUI3.WindowBase
{
    partial class WindowManager
    {
        private static Dictionary<WindowId, WindowManager> windowManagers = new Dictionary<WindowId, WindowManager>();
        private static object windowEventHookLocker = new object();
        private static WindowEventHook? windowEventHook;

        private static void SetDefaultIcon(WindowId windowId, bool useDarkMode)
        {
            if (windowId.Value == 0) return;
            var hWnd = Microsoft.UI.Win32Interop.GetWindowFromWindowId(windowId);

            var dpi = PInvoke.GetDpiForWindow(new Windows.Win32.Foundation.HWND(hWnd));
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

        public static WindowManager? Get(WindowId windowId)
        {
            lock (windowManagers)
            {
                if (windowManagers.TryGetValue(windowId, out var manager))
                {
                    return manager;
                }

                return null;
            }
        }

        public static WindowManager? Get(AppWindow window) => Get(window.Id);

        public static WindowManager? Get(Window window) => Get(window.AppWindow);

        public static IReadOnlyList<WindowId> Windows => windowEventHook?.Windows
            .Select(c => Win32BaseObjectExtensions.ToWindowId(c)).ToArray()
            ?? Array.Empty<WindowId>();

        public static IReadOnlyList<WindowManager> GetWindowManagers()
        {
            lock (windowManagers)
            {
                return windowManagers.Values.ToArray();
            }
        }

        public static void Initialize()
        {
            if (windowEventHook == null)
            {
                lock (windowEventHookLocker)
                {
                    windowEventHook = new WindowEventHook();
                    windowEventHook.WindowChanged += WindowEventHook_WindowChanged;
                }
            }
        }

        public static void Uninitialize()
        {
            if (windowEventHook != null)
            {
                lock (windowEventHookLocker)
                {
                    if (windowEventHook != null)
                    {
                        windowEventHook.WindowChanged -= WindowEventHook_WindowChanged;
                        windowEventHook?.Dispose();
                        windowEventHook = null;
                    }
                }
            }
        }

        private static void WindowEventHook_WindowChanged(object sender, in WindowEventHook.WindowEventArgs e)
        {
            lock (windowManagers)
            {
                var windowId = new WindowId((ulong)e.HWND.Value.ToInt64());
                if (e.Type == WindowEventHook.WindowEventType.Created)
                {
                    windowManagers[windowId] = new WindowManager(windowId);
                }
            }
        }
    }
}
