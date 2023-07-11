using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using PInvoke = global::Windows.Win32.PInvoke;

namespace BlueFire.Toolkit.WinUI3.WindowBase
{
    internal class WindowEventHook : IDisposable
    {
        private bool disposedValue;
        private HOOKPROC hookProcHandler;
        private UnhookWindowsHookExSafeHandle hHook;
        private HashSet<nint> windowHandles = new HashSet<nint>();

        public WindowEventHook()
        {
            hookProcHandler = new HOOKPROC(HookProc);

            hHook = PInvoke.SetWindowsHookEx(
                WINDOWS_HOOK_ID.WH_CBT,
                hookProcHandler,
                PInvoke.GetModuleHandle(""),
                PInvoke.GetCurrentThreadId());
        }

        internal IReadOnlyCollection<nint> Windows => windowHandles;

        private unsafe LRESULT HookProc(int code, WPARAM wParam, LPARAM lParam)
        {
            if (code == PInvoke.HCBT_CREATEWND)
            {
                var hWnd = (nint)wParam.Value;
                var className = GetWindowClassName(hWnd);

                if (className == "WinUIDesktopWin32WindowClass")
                {
                    lock (windowHandles)
                    {
                        if (windowHandles.Add(hWnd))
                        {
                            var exStyle = (uint)PInvoke.GetWindowLongAuto(new HWND(hWnd), WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
                            exStyle |= (uint)(WINDOW_EX_STYLE.WS_EX_NOREDIRECTIONBITMAP);
                            PInvoke.SetWindowLongAuto(new HWND(hWnd), WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (nint)exStyle);

                            WindowChanged?.Invoke(this, new WindowEventArgs(new HWND(hWnd), WindowEventType.Created));
                        }
                    }
                }
            }
            else if (code == PInvoke.HCBT_DESTROYWND)
            {
                var hWnd = (nint)wParam.Value;

                lock (windowHandles)
                {
                    if (windowHandles.Remove(hWnd))
                    {
                        WindowChanged?.Invoke(this, new WindowEventArgs(new HWND(hWnd), WindowEventType.Destroyed));
                    }
                }
            }

            return PInvoke.CallNextHookEx(hHook, code, wParam, lParam);
        }

        public event WindowEventHookHandler? WindowChanged;

        internal unsafe static string GetWindowClassName(nint hWnd)
        {
            var pStr = stackalloc char[255];
            var str = new PWSTR(pStr);
            var count = PInvoke.GetClassName(new HWND((nint)hWnd), str, 255);
            return new string(pStr, 0, count);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                }

                hHook?.Dispose();
                hHook = null!;

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        ~WindowEventHook()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal record struct WindowEventArgs(HWND HWND, WindowEventType Type);

        internal delegate void WindowEventHookHandler(object sender, in WindowEventArgs e);

        internal enum WindowEventType
        {
            Created,
            Destroyed
        }
    }
}
