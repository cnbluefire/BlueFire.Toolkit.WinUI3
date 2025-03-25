using BlueFire.Toolkit.WindowBase;
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

namespace BlueFire.Toolkit.Dispatching
{
    internal partial class NativeWindow : IDisposable
    {
        private bool disposedValue;

        public const nint HWND_MESSAGE = -3;

        private const string WindowClassName = "BlueFire.Toolkit.NativeWindow";
        private const string DefaultWindowName = "BlueFire.Toolkit.NativeWindow";

        private static HINSTANCE HINSTANCE;
        private static Lock hInstanceLocker = new Lock();

        private static ushort windowClassAtom;

        private readonly nint parentWindow;
        private readonly string? windowName;
        private nint windowHandle;
        private GCHandle gcHandle;

        internal NativeWindow(nint parentWindow, string? windowName = DefaultWindowName)
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
                        lpfnWndProc = &StaticWndProc,
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
                    if (windowClassAtom == 0) Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }
            }

            fixed (char* pWindowName = windowName)
            {
                gcHandle = GCHandle.Alloc(this, GCHandleType.Weak);

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

                if (windowHandle == 0)
                {
                    gcHandle.Free();
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }

                GC.KeepAlive(this);
            }
        }

        public nint WindowHandle => windowHandle;

        public event WindowMessageReceivedEventHandler? MessageReceived;

        protected virtual void OnWndProc(WindowMessageReceivedEventArgs args)
        {
            var handler = MessageReceived;

            if (handler != null)
            {
                handler?.Invoke(this, args);
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private unsafe static LRESULT StaticWndProc(HWND hWnd, uint Msg, WPARAM wParam, LPARAM lParam)
        {
            const string NativeWindowPropertyName = "NATIVEWINDOW";

            bool handled = false;
            LRESULT result = default;

            if (Msg == PInvoke.WM_NCCREATE)
            {
                var createStruct = (CREATESTRUCTW*)lParam.Value;
                var gcHandle = (GCHandle)(nint)createStruct->lpCreateParams;

                if (gcHandle.IsAllocated && gcHandle.Target is NativeWindow window)
                {
                    fixed (char* pPropName = NativeWindowPropertyName)
                    {
                        PInvoke.SetProp(hWnd, pPropName, (HANDLE)(nint)createStruct->lpCreateParams);
                    }
                }
                else
                {
                    return (LRESULT)0;
                }
            }
            else
            {
                GCHandle gcHandle = default;
                fixed (char* pPropName = NativeWindowPropertyName)
                {
                    var value = PInvoke.GetProp(hWnd, pPropName);
                    if (!value.IsNull)
                    {
                        gcHandle = (GCHandle)(nint)value;
                    }
                }

                if (Msg == PInvoke.WM_DESTROY)
                {
                    fixed (char* pPropName = NativeWindowPropertyName)
                    {
                        PInvoke.RemoveProp(hWnd, pPropName);
                    }
                }

                if (gcHandle.IsAllocated && gcHandle.Target is NativeWindow nativeWindow)
                {
                    var args = new WindowMessageReceivedEventArgs();

                    args.WindowHandle = hWnd.Value;
                    args.MessageId = Msg;
                    args.WParam = wParam.Value;
                    args.LParam = lParam.Value;

                    nativeWindow.OnWndProc(args);

                    if ((handled = args.Handled))
                    {
                        result = (LRESULT)args.LResult;
                    }
                }
            }

            if (!handled)
            {
                result = Windows.Win32.PInvoke.DefWindowProc(hWnd, Msg, wParam, lParam);
            }

            return result;
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

        ~NativeWindow()
        {
            DisposeCore(disposing: false);
        }

        public void Dispose()
        {
            DisposeCore(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

}
