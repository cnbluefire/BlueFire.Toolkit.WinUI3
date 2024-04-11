using BlueFire.Toolkit.WinUI3.Graphics;
using Microsoft.Graphics.Canvas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using WinRT;

namespace BlueFire.Toolkit.WinUI3.Test
{
    [TestClass]
    public class GraphicsTest
    {
        [TestMethod]
        public void GetInterop()
        {
            var device = CanvasDevice.GetSharedDevice();
            var d2d1Device = Direct2DInterop.GetWrappedResource<Windows.Win32.Graphics.Direct2D.ID2D1Device1>(device);

            var device2 = Direct2DInterop.GetOrCreate<CanvasDevice>(null, d2d1Device);

            var test = ((IWinRTObject)device).NativeObject.ThisPtr;
            Marshal.AddRef(test);
            var before = Marshal.Release(test);

            var punk = Marshal.GetIUnknownForObject(d2d1Device);
            Marshal.AddRef(test);
            var after = Marshal.Release(test);

            Assert.AreEqual(before, after);
        }

        [TestMethod]
        public void GetDXGIDevice()
        {
            var device = CanvasDevice.GetSharedDevice();
            var dxgiDevice = GraphicsHelper.GetInterface<Windows.Win32.Graphics.Dxgi.IDXGIDevice>(device);

            var punk = Marshal.GetIUnknownForObject(dxgiDevice);

            Marshal.AddRef(punk);
            var before = Marshal.Release(punk);

            var dxgiFactory = GraphicsHelper.GetDXGIFactory<Windows.Win32.Graphics.Dxgi.IDXGIFactory>(device);

            Marshal.AddRef(punk);
            var after = Marshal.Release(punk);

            Assert.AreEqual(before, after);
        }
    }
}
