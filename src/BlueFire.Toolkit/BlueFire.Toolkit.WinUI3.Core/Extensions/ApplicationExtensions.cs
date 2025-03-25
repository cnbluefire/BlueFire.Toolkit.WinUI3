using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using WinRT;

namespace BlueFire.Toolkit.WinUI3.Extensions
{
    public static class ApplicationExtensions
    {
        // https://github.com/microsoft/microsoft-ui-xaml/blob/winui3/release/1.6-stable/src/dxaml/xcp/dxaml/idl/winrt/main/microsoft.ui.xaml.private.idl#L265

        private static readonly Guid IID_IFrameworkApplicationPrivate = new Guid("ef5eed91-1afa-53a7-8ecf-ef14f223ca2c");
        
        public unsafe static IReadOnlyList<Window> GetWindows(this Application application)
        {
            nint ptr = 0;
            nint result = 0;

            try
            {
                var thisPtr = ((IWinRTObject)application).NativeObject.ThisPtr;

                WinRT.ExceptionHelpers.ThrowExceptionForHR((*(WinRT.Interop.IUnknownVftbl**)thisPtr)->QueryInterface(thisPtr, (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in IID_IFrameworkApplicationPrivate)), &ptr));
                WinRT.ExceptionHelpers.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, nint*, int>)((*(void***)ptr)[6]))(ptr, &result));

                return ConvertToManaged(result);
            }
            finally
            {
                if (result != 0) (*(WinRT.Interop.IUnknownVftbl**)result)->Release(result);
                if (ptr != 0) (*(WinRT.Interop.IUnknownVftbl**)ptr)->Release(ptr);
            }
        }

        private static IReadOnlyList<Window> ConvertToManaged(nint abi)
        {
            var count = IVectorView_T_get_Size(abi);
            if (count == 0) return [];

            var array = new Window[count];

            IVectorView_T_GetMany(abi, 0, array);
            return array;

            unsafe static uint IVectorView_T_get_Size(nint thisPtr)
            {
                uint result = 0u;
                ExceptionHelpers.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, uint*, int>)(*(IntPtr*)((nint)(*(IntPtr*)thisPtr) + (nint)7 * (nint)sizeof(delegate* unmanaged[Stdcall]<nint, uint*, int>))))(thisPtr, &result));
                return result;
            }

            unsafe static uint IVectorView_T_GetMany(nint thisPtr, uint startIndex, Window[] items)
            {
                MarshalInterfaceHelper<Window>.MarshalerArray marshalerArray = default(MarshalInterfaceHelper<Window>.MarshalerArray);
                int num = 0;
                nint intPtr = 0;
                uint result = 0u;
                try
                {
                    marshalerArray = ABI.Microsoft.UI.Xaml.Window.CreateMarshalerArray(items);
                    (num, intPtr) = ABI.Microsoft.UI.Xaml.Window.GetAbiArray(marshalerArray);
                    ExceptionHelpers.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<IntPtr, uint, int, IntPtr, uint*, int>)(*(IntPtr*)((nint)(*(IntPtr*)(void*)thisPtr) + (nint)9 * (nint)sizeof(delegate* unmanaged[Stdcall]<IntPtr, uint, int, IntPtr, uint*, int>))))(thisPtr, startIndex, num, intPtr, &result));
                    ABI.Microsoft.UI.Xaml.Window.CopyAbiArray(items, (num, intPtr));
                    return result;
                }
                finally
                {
                    ABI.Microsoft.UI.Xaml.Window.DisposeMarshalerArray(marshalerArray);
                }
            }
        }
    }
}
