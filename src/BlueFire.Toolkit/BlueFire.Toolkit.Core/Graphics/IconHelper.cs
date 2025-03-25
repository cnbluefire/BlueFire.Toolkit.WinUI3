using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace BlueFire.Toolkit.Graphics
{
    public static class IconHelper
    {
        public static async Task ConvertBitmapToIconStreamAsync(SoftwareBitmap bitmap, IReadOnlyList<int> sizes, Stream outputStream)
        {
            var hashSet = new HashSet<int>();
            for (int i = 0; i < sizes.Count; i++)
            {
                if (sizes[i] > 0)
                {
                    if (sizes[i] > 256) hashSet.Add(256);
                    else hashSet.Add(sizes[i]);
                }
            }

            if (hashSet.Count == 0)
            {
                ThrowException(nameof(sizes));
            }
            await ConvertBitmapToIconStreamAsync(hashSet.OrderBy(c => c).Select(c => new ImageData(c, bitmap)).ToArray(), outputStream);

            [DoesNotReturn]
            void ThrowException(string propName)
            {
                throw new InvalidOperationException(propName ?? "");
            }
        }

        public static async Task ConvertBitmapToIconStreamAsync(ImageData[] images, Stream outputStream)
        {
            if (outputStream.CanSeek) outputStream.Seek(0, SeekOrigin.Begin);

            using (var iconWriter = new BinaryWriter(outputStream, Encoding.ASCII, leaveOpen: true))
            using (var imagesBuffer = new MemoryStream())
            using (var tmpStream = new MemoryStream())
            {
                // Write header
                // 0-1 reserved
                iconWriter.Write((byte)0);
                iconWriter.Write((byte)0);

                // 2-3 image type, 1 = icon, 2 = cursor
                iconWriter.Write((short)1);

                // 4-5 number of images
                iconWriter.Write((short)images.Length);

                // image data offset
                // ico header (6 bytes) + image directory (16 bytes per image)
                long offset = 6 + (16 * images.Length);

                // Write image directory
                foreach (var image in images)
                {
                    tmpStream.Position = 0;
                    tmpStream.SetLength(0);


                    SoftwareBitmap bitmap = image.Image;
                    bool disposeFlag = false;

                    var size = image.Size;
                    if (size > 256) size = 256;

                    try
                    {
                        if (image.Image.BitmapPixelFormat != BitmapPixelFormat.Bgra8)
                        {
                            bitmap = SoftwareBitmap.Convert(image.Image, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                            disposeFlag = true;
                        }

                        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, tmpStream.AsRandomAccessStream());

                        if (bitmap.PixelWidth != bitmap.PixelHeight
                            || bitmap.PixelWidth != image.Size)
                        {
                            encoder.BitmapTransform.ScaledWidth = (uint)size;
                            encoder.BitmapTransform.ScaledHeight = (uint)size;
                        }
                        encoder.SetSoftwareBitmap(image.Image);
                        await encoder.FlushAsync();
                    }
                    finally
                    {
                        if (disposeFlag && bitmap != null) bitmap.Dispose();
                    }

                    tmpStream.Position = 0;
                    tmpStream.CopyTo(imagesBuffer);

                    var dataLength = tmpStream.Length;

                    // 0 image width
                    iconWriter.Write((byte)(size));

                    // 1 image height
                    iconWriter.Write((byte)(size));

                    // 2 number of colors, 0 if the image does not use a color palette
                    iconWriter.Write((byte)0);

                    // 3 reserved
                    iconWriter.Write((byte)0);

                    // 4-5 color planes
                    iconWriter.Write((short)0);

                    // 6-7 bits per pixel, png use 4 bytes per pixel (argb)
                    iconWriter.Write((short)32);

                    // 8-11 size of image data
                    iconWriter.Write((uint)dataLength);

                    // 12-15 offset of image data
                    iconWriter.Write((uint)offset);

                    offset += dataLength;
                }

                imagesBuffer.Position = 0;
                imagesBuffer.CopyTo(iconWriter.BaseStream);
            }
        }

        public class ImageData : IDisposable
        {
            private bool disposedValue;

            public ImageData(int size, SoftwareBitmap image)
            {
                Size = size;
                Image = image;
            }

            public int Size { get; }

            public SoftwareBitmap Image { get; }

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    Image.Dispose();
                    disposedValue = true;
                }
            }

            ~ImageData()
            {
                Dispose(disposing: false);
            }

            public void Dispose()
            {
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }

    }
}
