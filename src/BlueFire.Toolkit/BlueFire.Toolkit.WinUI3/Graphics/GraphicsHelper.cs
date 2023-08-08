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

namespace BlueFire.Toolkit.WinUI3.Graphics
{
    public unsafe static class GraphicsHelper
    {
        public static T? GetInterface<T>(IDirect3DDevice? device) where T : class
        {
            if (device == null) return null;

            var access = device.As<IDirect3DDxgiInterfaceAccess>();

            var type = typeof(T);

            var iid = type.GetGuidType().GUID;

            try
            {
                access.GetInterface(&iid, out var d3d11Device);
                return (T?)d3d11Device;
            }
            catch { }

            return null;
        }

        public static T? GetDXGIFactory<T>(IDirect3DDevice? device) where T : class
        {
            if (device == null) return null;

            var dxgiDevice = GetInterface<IDXGIDevice>(device);
            if (dxgiDevice != null)
            {
                var iid = typeof(T).GetGuidType().GUID;

                try
                {
                    dxgiDevice.GetAdapter(out var adapter);
                    adapter.GetParent(&iid, out var parent);

                    return (T?)parent;
                }
                catch { }
            }
            return null;
        }
    }
}
