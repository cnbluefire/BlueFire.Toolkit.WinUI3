using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.DirectX.Direct3D11;
using WinRT;
using System.Runtime.InteropServices;
using IDirect3DDxgiInterfaceAccess = Windows.Win32.System.WinRT.Direct3D11.IDirect3DDxgiInterfaceAccess;
using IDXGIDevice = Windows.Win32.Graphics.Dxgi.IDXGIDevice;
using IDXGIAdapter = Windows.Win32.Graphics.Dxgi.IDXGIAdapter;
using BlueFire.Toolkit.WinUI3.Extensions;
using WinRT.Interop;

namespace BlueFire.Toolkit.WinUI3.Graphics
{
    public unsafe static class GraphicsHelper
    {
        public static unsafe T? GetInterface<T>(IDirect3DDevice? device) where T : class
        {
            var iid = typeof(T).GetGuidType().GUID;
            var result = GetInterface(device, ref iid);

            if (result != 0)
            {
                using var objRef = ObjectReference<IUnknownVftbl>.Attach(ref result);
                return objRef.AsInterface<T>();
            }

            return null;
        }

        internal static unsafe ComPtr<T> GetInterfacePtr<T>(IDirect3DDevice? device) where T : unmanaged
        {
            var iid = typeof(T).GetGuidType().GUID;
            var result = GetInterface(device, ref iid);

            return ComPtr<T>.Attach(ref result);
        }

        internal static unsafe nint GetInterface(IDirect3DDevice? device, ref Guid iid)
        {
            if (device == null) return default;

            var hr = ComObjectHelper.QueryInterface<IDirect3DDxgiInterfaceAccess>(
                device,
                IDirect3DDxgiInterfaceAccess.IID_Guid,
                out var access);

            if (hr.Failed) return default;

            using (access)
            {
                fixed (Guid* riid = &iid)
                {
                    nint result = 0;
                    hr = access.Value.GetInterface(riid, (void**)(&result));

                    if (hr.Failed)
                    {
                        result = 0;
                    };

                    return result;
                }
            }
        }

        public static T? GetDXGIFactory<T>(IDirect3DDevice? device) where T : class
        {
            var iid = typeof(T).GetGuidType().GUID;
            var result = GetDXGIFactory(device, ref iid);

            if (result != 0)
            {
                using var objRef = ObjectReference<IUnknownVftbl>.Attach(ref result);
                return objRef.AsInterface<T>();
            }

            return null;
        }

        internal static ComPtr<T> GetDXGIFactoryPtr<T>(IDirect3DDevice? device) where T : unmanaged
        {
            var iid = typeof(T).GetGuidType().GUID;
            var result = GetDXGIFactory(device, ref iid);

            return ComPtr<T>.Attach(ref result);
        }

        private static nint GetDXGIFactory(IDirect3DDevice? device, ref Guid iid)
        {
            if (device == null) return 0;

            using var dxgiDevice = GetInterfacePtr<IDXGIDevice>(device);

            if (dxgiDevice.HasValue)
            {
                using (ComPtr<IDXGIAdapter> adapter = default)
                {
                    fixed (Guid* riid = &iid)
                    {
                        try
                        {
                            var hr = dxgiDevice.Value.GetAdapter(adapter.TypedPointerRef);
                            if (hr.Succeeded)
                            {
                                nint result = 0;

                                adapter.Value.GetParent(riid, (void**)(&result));

                                return result;
                            }
                        }
                        catch { }
                    }
                }
            }
            return 0;
        }
    }
}
