using BlueFire.Toolkit.Graphics;
using BlueFire.Toolkit.WinUI3.WindowBase;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace BlueFire.Toolkit.WinUI3.Extensions
{
    public static class WindowExtensions
    {
        public static nint GetWindowHandle(this WindowEx window) => GetWindowHandle(window.XamlWindow);

        public static nint GetWindowHandle(this Window window) => WinRT.Interop.WindowNative.GetWindowHandle(window);

        public static nint GetWindowHandle(this AppWindow window) => (nint?)(WindowManager.Get(window)?.HWND.Value) ?? Win32Interop.GetWindowFromWindowId(window.Id);

        public static uint GetDpiForWindow(this WindowEx window) => window.WindowDpi;

        public static uint GetDpiForWindow(this Window window) => WindowManager.Get(window)?.WindowDpi ?? PInvoke.GetDpiForWindow((HWND)GetWindowHandle(window));

        public static uint GetDpiForWindow(this AppWindow window) => WindowManager.Get(window)?.WindowDpi ?? PInvoke.GetDpiForWindow((HWND)GetWindowHandle(window));

        public static void SetForegroundWindow(this WindowEx window) => PInvoke.SetForegroundWindow((HWND)GetWindowHandle(window));

        public static void SetForegroundWindow(this Window window) => PInvoke.SetForegroundWindow((HWND)GetWindowHandle(window));

        public static void SetForegroundWindow(this AppWindow window) => PInvoke.SetForegroundWindow((HWND)GetWindowHandle(window));

        public static Screen GetScreen(this WindowEx window) => Screen.FromHandle(GetWindowHandle(window));

        public static Screen GetScreen(this Window window) => Screen.FromHandle(GetWindowHandle(window));

        public static Screen GetScreen(this AppWindow window) => Screen.FromHandle(GetWindowHandle(window));
    }
}
