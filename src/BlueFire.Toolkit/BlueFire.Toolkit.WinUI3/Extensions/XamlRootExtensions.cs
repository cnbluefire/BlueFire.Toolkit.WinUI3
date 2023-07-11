using Microsoft.UI;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using WinRT;
using WinRT.Interop;

namespace BlueFire.Toolkit.WinUI3.Extensions
{
    internal static class XamlRootExtensions
    {
        private const string IID_IXamlRootFeature_ExperimentalApi_String = "0919ab0b-7e01-59a2-bf8b-2e8008d1f88d";
        private readonly static Guid IID_IXamlRootFeature_ExperimentalApi = new Guid(IID_IXamlRootFeature_ExperimentalApi_String);

        internal unsafe static WindowId GetContentWindowId(this XamlRoot xamlRoot)
        {
            var experimentalApi = ((IWinRTObject)xamlRoot).NativeObject.As(IID_IXamlRootFeature_ExperimentalApi);
            var contentWindow = GetContentWindow(experimentalApi);

            try
            {
                return GetWindowId(contentWindow);
            }
            finally
            {
                if (contentWindow != IntPtr.Zero)
                {
                    MarshalInspectable<object>.DisposeAbi(contentWindow);
                }
            }
        }

        private unsafe static IntPtr GetContentWindow(IObjectReference _obj)
        {
            IntPtr thisPtr = _obj.ThisPtr;
            IntPtr intPtr = default(IntPtr);

            ExceptionHelpers.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<IntPtr, out IntPtr, int>)(*(IntPtr*)((nint)(*(IntPtr*)(void*)thisPtr) + (nint)6 * (nint)sizeof(delegate* unmanaged[Stdcall]<IntPtr, out IntPtr, int>))))(thisPtr, out intPtr));
            return intPtr;
        }

        private unsafe static WindowId GetWindowId(IntPtr thisPtr)
        {
            WindowId result = default(WindowId);
            ExceptionHelpers.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<IntPtr, out WindowId, int>)(*(IntPtr*)((nint)(*(IntPtr*)(void*)thisPtr) + (nint)10 * (nint)sizeof(delegate* unmanaged[Stdcall]<IntPtr, out WindowId, int>))))(thisPtr, out result));
            return result;
        }

    }
}
