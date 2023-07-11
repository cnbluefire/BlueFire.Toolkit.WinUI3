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

namespace BlueFire.Toolkit.WinUI3.WindowBase
{
    partial class WindowManager
    {
        private static Dictionary<WindowId, WindowManager> windowManagers = new Dictionary<WindowId, WindowManager>();
        private static object windowEventHookLocker = new object();
        private static WindowEventHook? windowEventHook;

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
                else
                {
                    if(windowManagers.TryGetValue(windowId,out var manager))
                    {
                        manager.WindowMessageReceived += manager.OnDestroyProc;
                    }
                }
            }
        }
    }
}
