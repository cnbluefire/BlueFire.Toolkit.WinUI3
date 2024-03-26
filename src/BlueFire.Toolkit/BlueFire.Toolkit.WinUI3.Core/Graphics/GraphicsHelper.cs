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
    /// <summary>
    /// <see cref="Windows.Graphics.DirectX.Direct3D11.IDirect3DDevice"/> interop helper.
    /// </summary>
    public unsafe static class GraphicsHelper
    {
        private const int E_POINTER = unchecked((int)0x80004003);

        /// <summary>
        /// Retrieves the DXGI interface that is wrapped by the IDirect3DDxgiInterfaceAccess object.
        /// <para>
        /// see also <seealso href="https://learn.microsoft.com/en-us/windows/win32/api/windows.graphics.directx.direct3d11.interop/nf-windows-graphics-directx-direct3d11-interop-idirect3ddxgiinterfaceaccess-getinterface"/>
        /// </para>
        /// </summary>
        /// <typeparam name="T">Managed com interface.</typeparam>
        /// <param name="device"></param>
        /// <returns></returns>
        public static unsafe T? GetInterface<T>(IDirect3DDevice? device) where T : class
        {
            var iid = typeof(T).GetGuidType().GUID;
            nint pvObject = 0;
            var hr = (Windows.Win32.Foundation.HRESULT)GetInterface(device, ref iid, (void**)(&pvObject));

            if (hr.Succeeded)
            {
                using var objRef = ObjectReference<IUnknownVftbl>.Attach(ref pvObject);
                return objRef.AsInterface<T>();
            }

            return null;
        }

        internal static unsafe ComPtr<T> GetInterfacePtr<T>(IDirect3DDevice? device) where T : unmanaged
        {
            var iid = typeof(T).GetGuidType().GUID;
            void* pvObject = null;
            var hr = (Windows.Win32.Foundation.HRESULT)GetInterface(device, ref iid, &pvObject);

            if (hr.Failed) return new ComPtr<T>();

            return ComPtr<T>.Attach(&pvObject);
        }

        /// <summary>
        /// Retrieves the DXGI interface that is wrapped by the IDirect3DDxgiInterfaceAccess object.
        /// <para>
        /// see also <seealso href="https://learn.microsoft.com/en-us/windows/win32/api/windows.graphics.directx.direct3d11.interop/nf-windows-graphics-directx-direct3d11-interop-idirect3ddxgiinterfaceaccess-getinterface"/>
        /// </para>
        /// </summary>
        /// <param name="device"></param>
        /// <param name="iid">A reference to the globally unique identifier (GUID) of the interface that you wish to be returned in p.</param>
        /// <param name="ppvObject">A pointer to a memory block that receives a pointer to the DXGI interface.</param>
        /// <returns></returns>
        public static unsafe int GetInterface(IDirect3DDevice? device, ref Guid iid, void** ppvObject)
        {
            if (device == null) return E_POINTER;

            var hr = ComObjectHelper.QueryInterface<IDirect3DDxgiInterfaceAccess>(
                device,
                IDirect3DDxgiInterfaceAccess.IID_Guid,
                out var access);

            if (hr.Failed) return hr;

            using (access)
            {
                fixed (Guid* riid = &iid)
                {
                    hr = access.Value.GetInterface(riid, ppvObject);

                    if (hr.Failed)
                    {
                        return hr;
                    };

                    return 0;
                }
            }
        }

        /// <summary>
        /// Get IDXGIFactory for IDirect3DDevice.
        /// </summary>
        /// <typeparam name="T">Managed com interface.</typeparam>
        /// <param name="device"></param>
        /// <returns></returns>
        public static T? GetDXGIFactory<T>(IDirect3DDevice? device) where T : class
        {
            var iid = typeof(T).GetGuidType().GUID;
            nint pvObject = 0;
            var hr = (Windows.Win32.Foundation.HRESULT)GetDXGIFactory(device, ref iid, (void**)(&pvObject));

            if (hr.Succeeded)
            {
                using var objRef = ObjectReference<IUnknownVftbl>.Attach(ref pvObject);
                return objRef.AsInterface<T>();
            }

            return null;
        }

        internal static ComPtr<T> GetDXGIFactoryPtr<T>(IDirect3DDevice? device) where T : unmanaged
        {
            var iid = typeof(T).GetGuidType().GUID;
            void* pvObject = null;
            var hr = (Windows.Win32.Foundation.HRESULT)GetDXGIFactory(device, ref iid, &pvObject);

            if (hr.Failed) return new ComPtr<T>();

            return ComPtr<T>.Attach((void**)&pvObject);
        }

        /// <summary>
        /// Get IDXGIFactory for IDirect3DDevice.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="iid">A reference to the globally unique identifier (GUID) of the interface that you wish to be returned in p.</param>
        /// <param name="ppvObject">A pointer to a memory block that receives a pointer to the DXGI interface.</param>
        /// <returns></returns>
        public static int GetDXGIFactory(IDirect3DDevice? device, ref Guid iid, void** ppvObject)
        {
            if (device == null) return E_POINTER;

            var IID_DXGIDevice = IDXGIDevice.IID_Guid;
            IDXGIDevice* pDXGIDevice = null;

            var hr = (Windows.Win32.Foundation.HRESULT)GetInterface(device, ref IID_DXGIDevice, (void**)(&pDXGIDevice));
            if (hr.Failed) return hr;

            using (var dxgiDevice = ComPtr<IDXGIDevice>.Attach((void**)(&pDXGIDevice)))
            using (ComPtr<IDXGIAdapter> adapter = default)
            {
                fixed (Guid* riid = &iid)
                {
                    try
                    {
                        hr = dxgiDevice.Value.GetAdapter(adapter.TypedPointerRef);
                        if (hr.Failed) return hr;

                        return adapter.Value.GetParent(riid, ppvObject).Value;
                    }
                    catch (Exception ex)
                    {
                        return ex.HResult;
                    }
                }
            }
        }
    }
}
