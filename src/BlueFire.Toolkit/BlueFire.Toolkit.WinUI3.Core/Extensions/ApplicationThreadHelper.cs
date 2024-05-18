using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinRT;

namespace BlueFire.Toolkit.WinUI3.Core.Extensions
{
    internal static class ApplicationThreadHelper
    {
        private const int RPC_E_WRONG_THREAD = unchecked((int)0x8001010e);
        private static readonly Guid IID_IApplication = new Guid(111736039u, 4422, 21935, 130, 13, 235, 213, 86, 67, 176, 33);
        private static readonly Guid IID_IApplication3 = new Guid(3197375893u, 25086, 23350, 163, 211, 150, 42, 100, 125, 124, 111);

        private static Microsoft.UI.Dispatching.DispatcherQueue? _dispatcherQueue;
        private static bool supportIApplication3 = true;

        public static bool CheckAccess()
        {
            var dispatcherQueue = _dispatcherQueue;

            if (dispatcherQueue == null)
            {
                dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
                if (dispatcherQueue == null)
                {
                    return false;
                }
                if (IsApplicationThread())
                {
                    _dispatcherQueue = dispatcherQueue;
                    return true;
                }
            }

            return dispatcherQueue.HasThreadAccess;
        }

        public static void VerifyAccess()
        {
            if (!CheckAccess())
            {
                throw new InvalidOperationException("The calling thread cannot access this object because a different thread owns it.")
                {
                    HResult = RPC_E_WRONG_THREAD
                };
            }
        }

        private static unsafe bool IsApplicationThread()
        {
            if (Application.Current == null) return false;

            if (supportIApplication3)
            {
                if (((IWinRTObject)Application.Current).NativeObject.TryAs<WinRT.Interop.IUnknownVftbl>(IID_IApplication3, out var objRefIApplication3) >= 0)
                {
                    // Application.Current.DispatcherShutdownMode
                    try
                    {
                        var thisPtr = objRefIApplication3.ThisPtr;
                        int result = default;
                        var hr = ((delegate* unmanaged[Stdcall]<IntPtr, out int, int>)(*(IntPtr*)(*(IntPtr*)(void*)thisPtr + 6 * (nint)sizeof(delegate* unmanaged[Stdcall]<IntPtr, out int, int>))))(thisPtr, out result);
                        return hr != RPC_E_WRONG_THREAD;
                    }
                    finally { objRefIApplication3.Dispose(); }
                }
                else
                {
                    supportIApplication3 = false;
                }
            }

            if (((IWinRTObject)Application.Current).NativeObject.TryAs<WinRT.Interop.IUnknownVftbl>(IID_IApplication, out var objRefIApplication) >= 0)
            {
                // Application.Current.Resource
                try
                {
                    var thisPtr = objRefIApplication.ThisPtr;
                    nint intPtr = 0;
                    var hr = ((delegate* unmanaged[Stdcall]<IntPtr, out IntPtr, int>)(*(IntPtr*)(*(IntPtr*)(void*)thisPtr + 6 * (nint)sizeof(delegate* unmanaged[Stdcall]<IntPtr, out IntPtr, int>))))(thisPtr, out intPtr);
                    return hr != RPC_E_WRONG_THREAD;
                }
                finally { objRefIApplication.Dispose(); }
            }

            return false;
        }
    }
}
