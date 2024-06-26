using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace BlueFire.Toolkit.WinUI3.Core.Dispatching
{
    internal class MessageWindow : IDisposable
    {
        private bool disposedValue;

        public const nint HWND_MESSAGE = -3;

        private const string WindowClassName = "BlueFire.Toolkit.MessageWindow";
        private const string DefaultWindowName = "BlueFire.Toolkit.MessageWindow";

        private static ConcurrentDictionary<nint, WeakReference<MessageWindow>> windows = new ConcurrentDictionary<nint, WeakReference<MessageWindow>>();
        private static HINSTANCE HINSTANCE;
        private static object hInstanceLocker = new object();

        [ThreadStatic]
        private static ushort windowClassAtom;

        private readonly nint parentWindow;
        private readonly string? windowName;
        private nint windowHandle;

        public MessageWindow(nint parentWindow, string? windowName = DefaultWindowName)
        {
            this.parentWindow = parentWindow;
            this.windowName = windowName;

            CreateWindow();
        }

        private unsafe void CreateWindow()
        {
            if (HINSTANCE.IsNull)
            {
                lock (hInstanceLocker)
                {
                    if (HINSTANCE.IsNull)
                    {
                        HINSTANCE = PInvoke.GetModuleHandle(default(PCWSTR));
                    }
                }
            }

            if (HINSTANCE.IsNull) throw new ArgumentException(nameof(HINSTANCE));

            if (windowClassAtom == 0)
            {
                var className = WindowClassName;
                fixed (char* pClassName = className)
                {
                    var wndClassEx = new WNDCLASSEXW()
                    {
                        cbSize = (uint)sizeof(WNDCLASSEXW),
                        style = 0,
                        lpfnWndProc = &GlobalWndProc,
                        cbClsExtra = 0,
                        cbWndExtra = 0,
                        hInstance = HINSTANCE,
                        hIcon = default,
                        hIconSm = default,
                        hCursor = default,
                        hbrBackground = (Windows.Win32.Graphics.Gdi.HBRUSH)((nint)Windows.Win32.Graphics.Gdi.SYS_COLOR_INDEX.COLOR_WINDOW + 1),
                        lpszMenuName = (char*)null,
                        lpszClassName = pClassName
                    };
                    windowClassAtom = PInvoke.RegisterClassEx(&wndClassEx);
                    if (windowClassAtom == 0) ((HRESULT)Marshal.GetHRForLastWin32Error()).ThrowOnFailure();
                }
            }

            fixed (char* pWindowName = windowName)
            {
                var gcHandle = GCHandle.Alloc(this, GCHandleType.Normal);

                windowHandle = Windows.Win32.PInvoke.CreateWindowEx(
                    0,
                    new PCWSTR((char*)windowClassAtom),
                    pWindowName,
                    0,
                    100,
                    100,
                    100,
                    100,
                    (HWND)parentWindow,
                    default,
                    HINSTANCE,
                    (void*)GCHandle.ToIntPtr(gcHandle));

                if (windowHandle == 0) ((HRESULT)Marshal.GetHRForLastWin32Error()).ThrowOnFailure();
            }
        }

        internal nint WindowHandle => windowHandle;

        internal event WindowMessageReceivedEventHandler? MessageReceived;

        protected virtual LRESULT WndProc(HWND hWnd, uint Msg, WPARAM wParam, LPARAM lParam, ref bool handled)
        {
            var handler = MessageReceived;

            if (handler != null)
            {
                var args = new WindowMessageReceivedEventArgs();

                args.WindowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd.Value);
                args.MessageId = Msg;
                args.WParam = wParam.Value;
                args.LParam = lParam.Value;

                handler?.Invoke(this, args);
                if (args.Handled)
                {
                    handled = args.Handled;
                    return (LRESULT)args.LResult;
                }
            }

            return new LRESULT(0);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        private unsafe static LRESULT GlobalWndProc(HWND hWnd, uint Msg, WPARAM wParam, LPARAM lParam)
        {
            bool handled = false;
            LRESULT result = default;

            if (Msg == PInvoke.WM_NCCREATE)
            {
                var createStruct = (CREATESTRUCTW*)lParam.Value;
                var gcHandle = GCHandle.FromIntPtr((nint)createStruct->lpCreateParams);
                try
                {
                    if (gcHandle.IsAllocated && gcHandle.Target is MessageWindow window)
                    {
                        RemoveAllInvalidWindow();
                        windows[hWnd.Value] = new WeakReference<MessageWindow>(window);
                    }
                }
                finally
                {
                    gcHandle.Free();
                }
            }
            if (Msg == PInvoke.WM_DESTROY)
            {
                if (windows.Remove(hWnd.Value, out var weakWindow))
                {
                    if (weakWindow.TryGetTarget(out var window))
                    {
                        result = window.WndProc(hWnd, Msg, wParam, lParam, ref handled);
                    }
                    else
                    {
                        RemoveAllInvalidWindow();
                    }
                }
            }
            else
            {
                if (windows.TryGetValue(hWnd.Value, out var weakWindow))
                {
                    if (weakWindow.TryGetTarget(out var window))
                    {
                        result = window.WndProc(hWnd, Msg, wParam, lParam, ref handled);
                    }
                    else
                    {
                        RemoveAllInvalidWindow();
                    }
                }
            }

            if (!handled)
            {
                result = Windows.Win32.PInvoke.DefWindowProc(hWnd, Msg, wParam, lParam);
            }

            return result;
        }

        private static void RemoveAllInvalidWindow()
        {
            lock (windows)
            {
                var keys = windows.Keys.ToArray();
                for (int i = 0; i < keys.Length; i++)
                {
                    if (windows.TryGetValue(keys[i], out var weakRef)
                        && !weakRef.TryGetTarget(out _))
                    {
                        windows.Remove(keys[i], out _);
                    }
                }
            }
        }

        private void DestroyWindow()
        {
            var hWnd = (nint)Interlocked.Exchange(ref windowHandle, (nint)0);
            if (hWnd != 0 && Windows.Win32.PInvoke.IsWindow((HWND)hWnd))
            {
                if (!Windows.Win32.PInvoke.DestroyWindow((HWND)windowHandle))
                {
                    Windows.Win32.PInvoke.PostMessage((HWND)windowHandle, Windows.Win32.PInvoke.WM_CLOSE, 0, 0);
                }
            }
        }

        protected virtual void Dispose(bool disposing) { }

        private void DisposeCore(bool disposing)
        {
            if (!disposedValue)
            {
                DestroyWindow();

                Dispose(disposing);

                disposedValue = true;
            }
        }

        ~MessageWindow()
        {
            DisposeCore(disposing: false);
        }

        public void Dispose()
        {
            DisposeCore(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    public delegate void WindowMessageReceivedEventHandler(object sender, WindowMessageReceivedEventArgs e);
}
