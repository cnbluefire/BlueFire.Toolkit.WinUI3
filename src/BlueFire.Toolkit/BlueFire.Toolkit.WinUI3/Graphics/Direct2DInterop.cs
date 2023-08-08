using BlueFire.Toolkit.WinUI3.Extensions;
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
    public static class Direct2DInterop
    {
        private const string IID_ICanvasDeviceString = "A27F0B5D-EC2C-4D4F-948F-0AA1E95E33E6";
        private static readonly Guid IID_ICanvasDevice = new Guid(IID_ICanvasDeviceString);

        private static IObjectReference? canvasDeviceFactory;
        private static bool canvasDeviceFactoryNotSupported;

        // https://microsoft.github.io/Win2D/WinUI3/html/Interop.htm
        // Microsoft.Graphics.Canvas.native.h

        public static T? GetWrappedResource<T>(object wrapper) where T : class
        {
            return GetWrappedResource<T>(null, wrapper, 0);
        }

        public static T? GetWrappedResource<T>(IDirect3DDevice? canvasDevice, object wrapper) where T : class
        {
            return GetWrappedResource<T>(canvasDevice, wrapper, 0);
        }

        public static T? GetWrappedResource<T>(IDirect3DDevice? canvasDevice, object wrapper, float dpi) where T : class
        {
            if (wrapper == null) throw new ArgumentException(null, nameof(wrapper));

            var type = typeof(T);
            if (type.GetCustomAttribute<ComImportAttribute>(true) == null)
                throw new InvalidCastException(type.FullName);

            var factoryNative = wrapper.As<ICanvasResourceWrapperNative>();

            if (factoryNative == null) throw new InvalidCastException(wrapper.GetType().FullName);

            var guid = type.GetGuidType().GUID;

            using var objRef = GetCanvasDeviceReference(canvasDevice);

            var hr = factoryNative.GetNativeResource(objRef?.ThisPtr ?? IntPtr.Zero, dpi, ref guid, out var resource);
            if (hr >= 0)
            {
                using var objRef2 = ObjectReference<WinRT.Interop.IUnknownVftbl>.Attach(ref resource);
                return objRef2.AsInterface<T>();
            }

            return null;
        }

        public static T? GetOrCreateFromPtr<T>(IDirect3DDevice? device, nint resource) where T : class, IWinRTObject
        {
            return GetOrCreateFromPtr<T>(device, resource, 0);
        }

        public static T? GetOrCreateFromPtr<T>(nint resource) where T : class, IWinRTObject
        {
            return GetOrCreateFromPtr<T>(null, resource, 0);
        }

        public static T? GetOrCreateFromPtr<T>(IDirect3DDevice? device, nint resource, float dpi) where T : class, IWinRTObject
        {
            var factory = GetCanvasDeviceFactory();
            if (factory == null) return null;

            var factoryNative = factory.AsInterface<ICanvasFactoryNative>();

            using var objRef = GetCanvasDeviceReference(device);

            factoryNative.GetOrCreate(objRef?.ThisPtr ?? IntPtr.Zero, resource, dpi, out nint wrapper);

            return MarshalInspectable<T>.FromAbi(wrapper);
        }

        public static T? GetOrCreate<T>(IDirect3DDevice? device, object resource) where T : class, IWinRTObject
        {
            return GetOrCreate<T>(device, resource, 0);
        }

        public static T? GetOrCreate<T>(object resource) where T : class, IWinRTObject
        {
            return GetOrCreate<T>(null, resource, 0);
        }

        public static T? GetOrCreate<T>(IDirect3DDevice? device, object resource, float dpi) where T : class, IWinRTObject
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


        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("5F10688D-EA55-4D55-A3B0-4DDB55C0C20A")]
        private interface ICanvasResourceWrapperNative
        {
            int GetNativeResource(IntPtr device, float dpi, ref Guid iid, out nint resource);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("695C440D-04B3-4EDD-BFD9-63E51E9F7202")]
        private interface ICanvasFactoryNative
        {
            void GetIids(out int iidCount, out nint iids);

            void GetRuntimeClassName(out nint className);

            void GetTrustLevel(out WinRT.TrustLevel trustLevel);

            void GetOrCreate(IntPtr device, nint resource, float dpi, out nint wrapper);

            void RegisterWrapper(nint resource, nint wrapper);

            void UnregisterWrapper(nint resource);

            void RegisterEffectFactory(ref Guid effectId, nint factory);

            void UnregisterEffectFactory(ref Guid effectId);
        }

        private static IObjectReference? GetCanvasDeviceFactory()
        {
            if (canvasDeviceFactory == null && !canvasDeviceFactoryNotSupported)
            {
                lock (typeof(Direct2DInterop))
                {
                    if (canvasDeviceFactory == null && !canvasDeviceFactoryNotSupported)
                    {
                        var type = Type.GetType("Microsoft.Graphics.Canvas.CanvasDevice, Microsoft.Graphics.Canvas.Interop");
                        var canvasDeviceFactoryType = Type.GetType("Microsoft.Graphics.Canvas.ICanvasDeviceFactory, Microsoft.Graphics.Canvas.Interop");

                        if (type != null && canvasDeviceFactoryType != null)
                        {
                            var methodInfo = type.GetMethod(
                                "As",
                                1,
                                BindingFlags.Static | BindingFlags.Public,
                                null,
                                Type.EmptyTypes,
                                null);

                            if (methodInfo != null)
                            {
                                var factoryObj = methodInfo.MakeGenericMethod(canvasDeviceFactoryType).Invoke(null, null);

                                if (factoryObj is WinRT.IWinRTObject factory)
                                {
                                    canvasDeviceFactory = factory.NativeObject;
                                }
                            }
                        }

                        if (canvasDeviceFactory == null)
                        {
                            canvasDeviceFactoryNotSupported = true;
                        }
                    }
                }
            }

            return canvasDeviceFactory;
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
                        var punk = Marshal.GetIUnknownForObject(device);
                        using var tmpObjRef2 = ComWrappersSupport.GetObjectReferenceForInterface(punk);

                        objRef = tmpObjRef2.As(IID_ICanvasDevice);
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
