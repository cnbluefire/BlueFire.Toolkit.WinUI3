﻿using Microsoft.UI;
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32.Foundation;
using PInvoke = global::Windows.Win32.PInvoke;
using BlueFire.Toolkit.WinUI3.Extensions;
using System.Diagnostics;

namespace BlueFire.Toolkit.WinUI3.WindowBase
{
    internal class WindowMessageMonitor : IDisposable
    {
        private readonly WindowManager windowManager;

        private HWND hWnd;
        private SUBCLASSPROC subClassProc;
        private const int subClassId = 119;
        private bool disposedValue;
        private bool attached = false;

        private WindowMessageReceivedEventHandler? windowMessageReceived;
        private WindowMessageReceivedEventHandler? windowMessageBeforeReceived;
        private WindowMessageReceivedEventHandler? windowMessageAfterReceived;

        internal unsafe WindowMessageMonitor(WindowManager windowManager)
        {
            hWnd = windowManager.HWND;

            subClassProc = new SUBCLASSPROC()
            {
                Func = &GlobalSubClassProc
            };
            this.windowManager = windowManager;
        }

        internal bool MessageReceivedEventHandled => windowMessageReceived != null;

        [DebuggerNonUserCode]
        private LRESULT SubClassProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam, nuint uIdSubclass, nuint dwRefData)
        {
            var handler1 = windowMessageBeforeReceived;
            var handler2 = windowMessageReceived;
            var handler3 = windowMessageAfterReceived;

            LRESULT? result = null;

            WindowMessageReceivedEventArgs? args = null;

            if (handler1 != null || handler2 != null)
            {
                args = new WindowMessageReceivedEventArgs();

                args.WindowId = new WindowId((ulong)hWnd.Value.ToInt64());
                args.MessageId = uMsg;
                args.WParam = wParam.Value;
                args.LParam = lParam.Value;

                handler1?.Invoke(windowManager, args);

                if (args.Handled)
                {
                    result = new LRESULT(args.LResult);
                }
                else
                {
                    if (handler2 != null)
                    {
                        handler2.Invoke(windowManager, args);

                        if (args.Handled)
                        {
                            result = new LRESULT(args.LResult);
                        }
                    }
                }
            }

            if (!result.HasValue)
            {
                if (uMsg == PInvoke.WM_CLOSE)
                {
                    result = PInvoke.DefSubclassProc(hWnd, uMsg, wParam, lParam);
                    if (!PInvoke.IsWindow(hWnd))
                    {
                        PInvoke.DestroyWindow(hWnd);
                    }
                }
                else if (uMsg == PInvoke.WM_DESTROY)
                {
                    windowMessageReceived = null;
                    windowMessageBeforeReceived = null;
                    windowMessageAfterReceived = null;

                    Uninstall();
                }
            }

            if (!result.HasValue)
            {
                result = PInvoke.DefSubclassProc(hWnd, uMsg, wParam, lParam);
            }

            if (handler3 != null)
            {
                if (args == null)
                {
                    args = new WindowMessageReceivedEventArgs();
                }

                args.WindowId = new WindowId((ulong)hWnd.Value.ToInt64());
                args.MessageId = uMsg;
                args.WParam = wParam.Value;
                args.LParam = lParam.Value;

                args.Handled = true;
                args.LResult = result.Value;

                handler3?.Invoke(windowManager, args);
            }

            return result.Value;
        }

        private unsafe bool Install()
        {
            if (attached) return true;
            if (disposedValue) return false;

            if (PInvoke.IsWindow(hWnd))
            {
                PInvoke.SetWindowSubclass(hWnd, subClassProc.Func, subClassId, 0).ThrowIfFalse();
                attached = true;
                return true;
            }

            return false;
        }

        private unsafe void Uninstall()
        {
            if (attached)
            {
                PInvoke.RemoveWindowSubclass(hWnd, subClassProc.Func, subClassId).ThrowIfFalse();
                attached = false;
            }
        }

        public event WindowMessageReceivedEventHandler? WindowMessageReceived
        {
            add
            {
                windowMessageReceived += value;
                if (windowMessageReceived != null)
                {
                    Install();
                }
            }
            remove
            {
                windowMessageReceived -= value;
                if (!disposedValue
                    && windowMessageReceived == null
                    && windowMessageBeforeReceived == null
                    && windowMessageAfterReceived == null)
                {
                    Uninstall();
                }
            }
        }

        internal event WindowMessageReceivedEventHandler? WindowMessageBeforeReceived
        {
            add
            {
                windowMessageBeforeReceived += value;
                if (windowMessageBeforeReceived != null)
                {
                    Install();
                }
            }
            remove
            {
                windowMessageBeforeReceived -= value;
                if (!disposedValue
                    && windowMessageReceived == null
                    && windowMessageBeforeReceived == null
                    && windowMessageAfterReceived == null)
                {
                    Uninstall();
                }
            }
        }

        internal event WindowMessageReceivedEventHandler? WindowMessageAfterReceived
        {
            add
            {
                windowMessageAfterReceived += value;
                if (windowMessageAfterReceived != null)
                {
                    Install();
                }
            }
            remove
            {
                windowMessageAfterReceived -= value;
                if (!disposedValue
                    && windowMessageReceived == null
                    && windowMessageBeforeReceived == null
                    && windowMessageAfterReceived == null)
                {
                    Uninstall();
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                }

                windowMessageReceived = null;
                windowMessageBeforeReceived = null;
                windowMessageAfterReceived = null;

                Uninstall();

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        ~WindowMessageMonitor()
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


        private unsafe struct SUBCLASSPROC
        {
            public delegate* unmanaged[Stdcall]<global::Windows.Win32.Foundation.HWND, uint, global::Windows.Win32.Foundation.WPARAM, global::Windows.Win32.Foundation.LPARAM, nuint, nuint, global::Windows.Win32.Foundation.LRESULT> Func;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        private static unsafe LRESULT GlobalSubClassProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam, nuint uIdSubclass, nuint dwRefData)
        {
            return WindowManager.Get(Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd.Value))?
                .GetMonitorInternal()
                .SubClassProc(hWnd, uMsg, wParam, lParam, uIdSubclass, dwRefData) ?? new LRESULT(0);
        }

    }
}
