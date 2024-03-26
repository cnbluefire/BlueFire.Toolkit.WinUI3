using BlueFire.Toolkit.WinUI3.Extensions;
using Microsoft.Graphics.Canvas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.DirectX.Direct3D11;
using WinRT;

namespace BlueFire.Toolkit.WinUI3.Graphics
{
    /// <summary>
    /// <see href="https://microsoft.github.io/Win2D/WinUI3/html/Interop.htm"/>
    /// </summary>
    public static class Direct2DInterop
    {
        private const string IID_ICanvasDeviceString = "A27F0B5D-EC2C-4D4F-948F-0AA1E95E33E6";
        private static readonly Guid IID_ICanvasDevice = new Guid(IID_ICanvasDeviceString);

        private static bool canvasDeviceFactoryNotSupported;
        private static ComPtr<ICanvasFactoryNative>? canvasFactoryNative;

        // https://microsoft.github.io/Win2D/WinUI3/html/Interop.htm
        // Microsoft.Graphics.Canvas.native.h

        public static T? GetWrappedResource<T>(object wrapper) where T : class
        {
            return GetWrappedResource<T>(null, wrapper, 0);
        }

        public static T? GetWrappedResource<T>(CanvasDevice? canvasDevice, object wrapper) where T : class
        {
            return GetWrappedResource<T>(canvasDevice, wrapper, 0);
        }

        public static unsafe T? GetWrappedResource<T>(CanvasDevice? canvasDevice, object wrapper, float dpi) where T : class
        {
            if (wrapper == null) throw new ArgumentException(null, nameof(wrapper));

            var type = typeof(T);
            if (type.GetCustomAttribute<ComImportAttribute>(true) == null)
                throw new InvalidCastException(type.FullName);

            var guid = type.GetGuidType().GUID;

            nint resource = 0;

            var hr = GetNativeResource(canvasDevice, wrapper, dpi, &guid, (void**)(&resource));

            if (hr.Succeeded)
            {
                using var objRef = ObjectReference<WinRT.Interop.IUnknownVftbl>.Attach(ref resource);
                return objRef.AsInterface<T>();
            }

            return null;
        }

        internal static ComPtr<T> GetWrappedResourcePtr<T>(object wrapper) where T : unmanaged
        {
            return GetWrappedResourcePtr<T>(null, wrapper, 0);
        }

        internal static ComPtr<T> GetWrappedResourcePtr<T>(CanvasDevice? canvasDevice, object wrapper) where T : unmanaged
        {
            return GetWrappedResourcePtr<T>(canvasDevice, wrapper, 0);
        }

        internal static unsafe ComPtr<T> GetWrappedResourcePtr<T>(CanvasDevice? canvasDevice, object wrapper, float dpi) where T : unmanaged
        {
            if (wrapper == null) throw new ArgumentException(null, nameof(wrapper));

            var guid = typeof(T).GetGuidType().GUID;

            ComPtr<T> comPtr = default;

            GetNativeResource(canvasDevice, wrapper, dpi, &guid, comPtr.PointerRef)
                .ThrowOnFailure();

            return comPtr;
        }

        internal static unsafe Windows.Win32.Foundation.HRESULT GetNativeResource(CanvasDevice? canvasDevice, object wrapper, float dpi, Guid* iid, void** resource)
        {
            if (wrapper == null) return ComObjectHelper.E_POINTER;

            ComPtr<ICanvasResourceWrapperNative> pFactoryNative = default;

            try
            {
                var hr = ComObjectHelper.QueryInterface<ICanvasResourceWrapperNative>(
                    wrapper,
                    ICanvasResourceWrapperNative.IID_Guid,
                    out pFactoryNative);

                if (hr.Failed) return hr;

                using var objRef = GetCanvasDeviceReference(canvasDevice);

                hr = pFactoryNative.Value.GetNativeResource((void*)(objRef?.ThisPtr ?? IntPtr.Zero), dpi, iid, resource);
                return hr;
            }
            finally
            {
                pFactoryNative.Release();
            }
        }
        public static T? GetOrCreateFromPtr<T>(CanvasDevice? device, nint resource) where T : class, IWinRTObject
        {
            return GetOrCreateFromPtr<T>(device, resource, 0);
        }

        public static T? GetOrCreateFromPtr<T>(nint resource) where T : class, IWinRTObject
        {
            return GetOrCreateFromPtr<T>(null, resource, 0);
        }

        public static unsafe T? GetOrCreateFromPtr<T>(CanvasDevice? device, nint resource, float dpi) where T : class, IWinRTObject
        {
            using var objRef = GetCanvasDeviceReference(device);

            var factoryNative = GetCanvasDeviceFactory();
            if (!factoryNative.HasValue) return null;

            nint wrapper = 0;

            var hr = factoryNative.Value.Value.GetOrCreate((void*)(objRef?.ThisPtr ?? IntPtr.Zero), (void*)resource, dpi, (void**)(&wrapper));

            if (hr.Succeeded)
            {
                return MarshalInspectable<T>.FromAbi(wrapper);
            }


            return null;
        }

        public static T? GetOrCreate<T>(CanvasDevice? device, object resource) where T : class, IWinRTObject
        {
            return GetOrCreate<T>(device, resource, 0);
        }

        public static T? GetOrCreate<T>(object resource) where T : class, IWinRTObject
        {
            return GetOrCreate<T>(null, resource, 0);
        }

        public static T? GetOrCreate<T>(CanvasDevice? device, object resource, float dpi) where T : class, IWinRTObject
        {
            if (resource == null) return default;

            nint punk = Marshal.GetIUnknownForObject(resource);
            if (punk == 0) throw new InvalidCastException(resource.GetType().FullName);

            try
            {
                return GetOrCreateFromPtr<T>(device, punk, dpi);
            }
            finally
            {
                Marshal.Release(punk);
            }
        }


        //[ComImport]
        //[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        //[Guid("5F10688D-EA55-4D55-A3B0-4DDB55C0C20A")]
        //private interface ICanvasResourceWrapperNative
        //{
        //    int GetNativeResource(IntPtr device, float dpi, ref Guid iid, out nint resource);
        //}

        //[ComImport]
        //[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        //[Guid("695C440D-04B3-4EDD-BFD9-63E51E9F7202")]
        //private interface ICanvasFactoryNative
        //{
        //    void GetIids(out int iidCount, out nint iids);

        //    void GetRuntimeClassName(out nint className);

        //    void GetTrustLevel(out WinRT.TrustLevel trustLevel);

        //    void GetOrCreate(IntPtr device, nint resource, float dpi, out nint wrapper);

        //    void RegisterWrapper(nint resource, nint wrapper);

        //    void UnregisterWrapper(nint resource);

        //    void RegisterEffectFactory(ref Guid effectId, nint factory);

        //    void UnregisterEffectFactory(ref Guid effectId);
        //}

        private static void InitializeCanvasDeviceFactory()
        {
            if (canvasFactoryNative == null)
            {
                lock (typeof(Direct2DInterop))
                {
                    if (canvasFactoryNative == null)
                    {
                        ICanvasFactoryNative.Interface? rcw = null;
                        try
                        {
                            rcw = CanvasDevice.As<ICanvasFactoryNative.Interface>();

                            if (rcw != null)
                            {
                                var hr = ComObjectHelper.QueryInterface<ICanvasFactoryNative>(rcw, ICanvasFactoryNative.IID_Guid, out var result);
                                if (hr.Succeeded)
                                {
                                    canvasFactoryNative = result;
                                }
                            }
                        }
                        catch { }
                        finally
                        {
                            if (rcw != null) Marshal.ReleaseComObject(rcw);
                        }
                    }
                }
            }
        }

        private static ComPtr<ICanvasFactoryNative>? GetCanvasDeviceFactory()
        {
            if (!canvasFactoryNative.HasValue && !canvasDeviceFactoryNotSupported)
            {
                lock (typeof(Direct2DInterop))
                {
                    if (!canvasFactoryNative.HasValue && !canvasDeviceFactoryNotSupported)
                    {
                        InitializeCanvasDeviceFactory();
                    }

                    if (!canvasFactoryNative.HasValue)
                    {
                        canvasDeviceFactoryNotSupported = true;
                    }
                }
            }

            return canvasFactoryNative;
        }

        private static IObjectReference? GetCanvasDeviceReference(IDirect3DDevice? device)
        {
            IObjectReference? objRef = null;

            if (device != null)
            {
                try
                {
                    if (ComWrappersSupport.TryUnwrapObject(device, out var tmpObjRef))
                    {
                        objRef = tmpObjRef.As(IID_ICanvasDevice);
                    }
                    else
                    {
                        nint punk = 0;
                        try
                        {
                            punk = Marshal.GetIUnknownForObject(device);
                            using var tmpObjRef2 = ComWrappersSupport.GetObjectReferenceForInterface(punk);

                            objRef = tmpObjRef2.As(IID_ICanvasDevice);
                        }
                        finally
                        {
                            Marshal.Release(punk);
                        }
                    }
                }
                catch { }
            }

            return objRef;
        }
    }

    /*
        class __declspec(uuid("5F10688D-EA55-4D55-A3B0-4DDB55C0C20A"))
        ICanvasResourceWrapperNative : public IUnknown
        {
        public:
            IFACEMETHOD(GetNativeResource)(ICanvasDevice* device, float dpi, REFIID iid, void** resource) = 0;
        };
     

        class __declspec(uuid("695C440D-04B3-4EDD-BFD9-63E51E9F7202"))
        ICanvasFactoryNative : public IInspectable
        {
        public:
            IFACEMETHOD(GetOrCreate)(ICanvasDevice* device, IUnknown* resource, float dpi, IInspectable** wrapper) = 0;
            IFACEMETHOD(RegisterWrapper)(IUnknown* resource, IInspectable* wrapper) = 0;
            IFACEMETHOD(UnregisterWrapper)(IUnknown* resource) = 0;
            IFACEMETHOD(RegisterEffectFactory)(REFIID effectId, ICanvasEffectFactoryNative* factory) = 0;
            IFACEMETHOD(UnregisterEffectFactory)(REFIID effectId) = 0;
        };

     */
}
