#pragma warning disable CS0649

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using winmdroot = Windows.Win32;

namespace BlueFire.Toolkit.WinUI3.Graphics
{
    [Guid("5F10688D-EA55-4D55-A3B0-4DDB55C0C20A")]
    internal unsafe partial struct ICanvasResourceWrapperNative
    {
        internal unsafe winmdroot.Foundation.HRESULT QueryInterface(in global::System.Guid riid, out void* ppvObject)
        {
            fixed (void** ppvObjectLocal = &ppvObject)
            {
                fixed (global::System.Guid* riidLocal = &riid)
                {
                    winmdroot.Foundation.HRESULT __result = this.QueryInterface(riidLocal, ppvObjectLocal);
                    return __result;
                }
            }
        }

        public unsafe winmdroot.Foundation.HRESULT QueryInterface(global::System.Guid* riid, void** ppvObject)
        {
            return ((delegate* unmanaged[Stdcall]<ICanvasResourceWrapperNative*, global::System.Guid*, void**, winmdroot.Foundation.HRESULT>)lpVtbl[0])((ICanvasResourceWrapperNative*)Unsafe.AsPointer(ref this), riid, ppvObject);
        }

        public uint AddRef()
        {
            return ((delegate* unmanaged[Stdcall]<ICanvasResourceWrapperNative*, uint>)lpVtbl[1])((ICanvasResourceWrapperNative*)Unsafe.AsPointer(ref this));
        }

        public uint Release()
        {
            return ((delegate* unmanaged[Stdcall]<ICanvasResourceWrapperNative*, uint>)lpVtbl[2])((ICanvasResourceWrapperNative*)Unsafe.AsPointer(ref this));
        }

        public unsafe winmdroot.Foundation.HRESULT GetNativeResource(void* device, float dpi, Guid* iid, void** resource)
        {
            return ((delegate* unmanaged[Stdcall]<ICanvasResourceWrapperNative*, void*, float, Guid*, void**, winmdroot.Foundation.HRESULT>)lpVtbl[3])((ICanvasResourceWrapperNative*)Unsafe.AsPointer(ref this), device, dpi, iid, resource);
        }

        private void** lpVtbl;

        internal static readonly Guid IID_Guid = new Guid("5F10688D-EA55-4D55-A3B0-4DDB55C0C20A");

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("5F10688D-EA55-4D55-A3B0-4DDB55C0C20A")]
        internal interface Interface
        {
            [PreserveSig()]
            unsafe winmdroot.Foundation.HRESULT GetNativeResource(void* device, float dpi, Guid* iid, void** resource);
        }
    }

    [Guid("695C440D-04B3-4EDD-BFD9-63E51E9F7202")]
    internal unsafe partial struct ICanvasFactoryNative
    {
        internal unsafe winmdroot.Foundation.HRESULT QueryInterface(in global::System.Guid riid, out void* ppvObject)
        {
            fixed (void** ppvObjectLocal = &ppvObject)
            {
                fixed (global::System.Guid* riidLocal = &riid)
                {
                    winmdroot.Foundation.HRESULT __result = this.QueryInterface(riidLocal, ppvObjectLocal);
                    return __result;
                }
            }
        }

        public unsafe winmdroot.Foundation.HRESULT QueryInterface(global::System.Guid* riid, void** ppvObject)
        {
            return ((delegate* unmanaged[Stdcall]<ICanvasResourceWrapperNative*, global::System.Guid*, void**, winmdroot.Foundation.HRESULT>)lpVtbl[0])((ICanvasResourceWrapperNative*)Unsafe.AsPointer(ref this), riid, ppvObject);
        }

        public uint AddRef()
        {
            return ((delegate* unmanaged[Stdcall]<ICanvasResourceWrapperNative*, uint>)lpVtbl[1])((ICanvasResourceWrapperNative*)Unsafe.AsPointer(ref this));
        }

        public uint Release()
        {
            return ((delegate* unmanaged[Stdcall]<ICanvasResourceWrapperNative*, uint>)lpVtbl[2])((ICanvasResourceWrapperNative*)Unsafe.AsPointer(ref this));
        }

        public unsafe winmdroot.Foundation.HRESULT GetOrCreate(void* device, void* resource, float dpi, void** wrapper)
        {
            return ((delegate* unmanaged[Stdcall]<ICanvasFactoryNative*, void*, void*, float, void**, winmdroot.Foundation.HRESULT>)lpVtbl[6])((ICanvasFactoryNative*)Unsafe.AsPointer(ref this), device, resource, dpi, wrapper);
        }

        public unsafe winmdroot.Foundation.HRESULT RegisterWrapper(void* resource, void** wrapper)
        {
            return ((delegate* unmanaged[Stdcall]<ICanvasFactoryNative*, void*, void*, winmdroot.Foundation.HRESULT>)lpVtbl[7])((ICanvasFactoryNative*)Unsafe.AsPointer(ref this), resource, wrapper);
        }

        public unsafe winmdroot.Foundation.HRESULT UnregisterWrapper(void* resource)
        {
            return ((delegate* unmanaged[Stdcall]<ICanvasFactoryNative*, void*, winmdroot.Foundation.HRESULT>)lpVtbl[8])((ICanvasFactoryNative*)Unsafe.AsPointer(ref this), resource);
        }

        public unsafe winmdroot.Foundation.HRESULT RegisterEffectFactory(Guid* effectId, void* factory)
        {
            return ((delegate* unmanaged[Stdcall]<ICanvasFactoryNative*, Guid*, void*, winmdroot.Foundation.HRESULT>)lpVtbl[9])((ICanvasFactoryNative*)Unsafe.AsPointer(ref this), effectId, factory);
        }

        public unsafe winmdroot.Foundation.HRESULT UnregisterEffectFactory(Guid* effectId)
        {
            return ((delegate* unmanaged[Stdcall]<ICanvasFactoryNative*, Guid*, winmdroot.Foundation.HRESULT>)lpVtbl[10])((ICanvasFactoryNative*)Unsafe.AsPointer(ref this), effectId);
        }

        private void** lpVtbl;

        internal static readonly Guid IID_Guid = new Guid("695C440D-04B3-4EDD-BFD9-63E51E9F7202");

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("695C440D-04B3-4EDD-BFD9-63E51E9F7202")]
        internal interface Interface
        {
            [PreserveSig()]
            unsafe winmdroot.Foundation.HRESULT GetIids(out int iidCount, out nint iids);

            [PreserveSig()]
            unsafe winmdroot.Foundation.HRESULT GetRuntimeClassName(out nint className);

            [PreserveSig()]
            unsafe winmdroot.Foundation.HRESULT GetTrustLevel(out WinRT.TrustLevel trustLevel);

            [PreserveSig()]
            unsafe winmdroot.Foundation.HRESULT GetOrCreate(void* device, void* resource, float dpi, void** wrapper);

            [PreserveSig()]
            unsafe winmdroot.Foundation.HRESULT RegisterWrapper(void* resource, void* wrapper);

            [PreserveSig()]
            unsafe winmdroot.Foundation.HRESULT UnregisterWrapper(void* resource);

            [PreserveSig()]
            unsafe winmdroot.Foundation.HRESULT RegisterEffectFactory(Guid* effectId, void* factory);

            [PreserveSig()]
            unsafe winmdroot.Foundation.HRESULT UnregisterEffectFactory(Guid* effectId);
        }
    }

}
