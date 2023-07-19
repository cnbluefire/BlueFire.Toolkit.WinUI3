using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using WinRT;
using PInvoke = Windows.Win32.PInvoke;
using HICON = Windows.Win32.UI.WindowsAndMessaging.HICON;
using ICONINFO = Windows.Win32.UI.WindowsAndMessaging.ICONINFO;
using HDC = Windows.Win32.Graphics.Gdi.HDC;
using HBITMAP = Windows.Win32.Graphics.Gdi.HBITMAP;
using System.Runtime.InteropServices;
using BlueFire.Toolkit.WinUI3.Extensions;
using Microsoft.UI.Dispatching;
using BlueFire.Toolkit.WinUI3.Compositions;

namespace BlueFire.Toolkit.WinUI3.Icons.InternalIcons
{
    internal class RandomAccessStreamIcon : ComposedIcon
    {
        private readonly Guid? decoderId;
        private IRandomAccessStream stream;
        private BitmapDecoder? decoder;
        private Dictionary<SizeInt32, SafeHIconHandle> icons = new Dictionary<SizeInt32, SafeHIconHandle>();
        private bool notSupport;

        internal RandomAccessStreamIcon(IRandomAccessStream stream, Guid? decoderId = null)
        {
            this.stream = stream;
            this.decoderId = decoderId;
        }

        protected internal override nint GetIconCore(SizeInt32 size)
        {
            if (notSupport) return 0;

            if (size.Width == 0 || size.Height == 0) return 0;

            if (icons.TryGetValue(size, out var icon)) return icon.DangerousGetHandle();

            nint icon2 = 0;
            try
            {
                icon2 = GetIconFromStream(size.Width, size.Height);
            }
            catch { }

            if (icon2 == 0)
            {
                notSupport = true;
            }
            else
            {
                icons[size] = new SafeHIconHandle(icon2);
            }

            return icon2;
        }

        private nint GetIconFromStream(int width, int height)
        {
            var softwareBitmap = WindowsCompositionHelper.DispatcherQueue.RunSync(async () =>
            {
                if (decoder == null)
                {
                    decoder = decoderId.HasValue ?
                        await BitmapDecoder.CreateAsync(decoderId.Value, stream) :
                        await BitmapDecoder.CreateAsync(stream);
                }

                return await decoder!.GetSoftwareBitmapAsync(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Premultiplied,
                    new BitmapTransform()
                    {
                        Bounds = new BitmapBounds(0, 0, decoder.PixelWidth, decoder.PixelHeight),
                        InterpolationMode = BitmapInterpolationMode.Fant,
                        ScaledWidth = (uint)width,
                        ScaledHeight = (uint)height
                    },
                    ExifOrientationMode.RespectExifOrientation,
                    ColorManagementMode.ColorManageToSRgb);

            });

            using var buffer = softwareBitmap.LockBuffer(BitmapBufferAccessMode.Read);

            return GetHIconFromBitmapBuffer(buffer);
        }

        private static unsafe nint GetHIconFromBitmapBuffer(BitmapBuffer bitmapBuffer)
        {
            using var reference = bitmapBuffer.CreateReference();

            var pBytes = (byte*)0;

            reference.As<Windows.Win32.System.WinRT.IMemoryBufferByteAccess>().GetBuffer(&pBytes, out var capacity);

            if (capacity > 0)
            {
                var span = new Span<byte>(pBytes, (int)capacity);
                var desc = bitmapBuffer.GetPlaneDescription(0);

                HBITMAP hBitmap = default;

                try
                {
                    hBitmap = PInvoke.CreateBitmap(desc.Width, desc.Height, 1, 32, pBytes);

                    var iconInfo = new ICONINFO()
                    {
                        fIcon = true,
                        hbmColor = hBitmap,
                        hbmMask = hBitmap
                    };

                    var icon = PInvoke.CreateIconIndirect(&iconInfo);

                    return icon.Value;
                }
                finally
                {
                    if (!hBitmap.IsNull)
                    {
                        PInvoke.DeleteObject(hBitmap);
                    }
                }
            }

            return 0;
        }
    }
}
