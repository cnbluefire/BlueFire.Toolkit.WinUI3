using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32.Foundation;
using PInvoke = Windows.Win32.PInvoke;
using BlueFire.Toolkit.WinUI3.Compositions;
using Windows.Foundation;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Composition;
using LoadedImageSourceLoadStatus = Microsoft.UI.Xaml.Media.LoadedImageSourceLoadStatus;
using WinRT;

namespace BlueFire.Toolkit.WinUI3.Media
{
    public class CompositionSurfaceLoader : ICompositionSurface, IDisposable
    {
        private static D2DDeviceHolder? softwareD2dDeviceHolder;
        private static CompositionGraphicsDevice? graphicsDevice;

        private CompositionDrawingSurface? surface;
        private CancellationTokenSource cts;

        private bool disposedValue;


        #region Static Create Methods

        public static CompositionSurfaceLoader StartLoadFromStream(IRandomAccessStream randomAccessStream, Size desiredMaxSize)
        {
            return new CompositionSurfaceLoader(randomAccessStream, desiredMaxSize);
        }

        public static CompositionSurfaceLoader StartLoadFromStream(IRandomAccessStream randomAccessStream)
        {
            return new CompositionSurfaceLoader(randomAccessStream, new Size(0, 0));
        }

        public static CompositionSurfaceLoader StartLoadFromUri(Uri uri, Size desiredMaxSize)
        {
            return new CompositionSurfaceLoader(uri, desiredMaxSize);
        }

        public static CompositionSurfaceLoader StartLoadFromUri(Uri uri)
        {
            return new CompositionSurfaceLoader(uri, new Size(0, 0));
        }

        #endregion Static Create Methods

        private CompositionSurfaceLoader()
        {
            CreateDevice();
            CreateSurface();
            cts = new CancellationTokenSource();
        }

        private CompositionSurfaceLoader(IRandomAccessStream randomAccessStream, Size desiredMaxSize) : this()
        {
            graphicsDevice!.DispatcherQueue.TryEnqueue(Windows.System.DispatcherQueuePriority.Low, async () =>
            {
                try
                {
                    await DrawImageCore(randomAccessStream, desiredMaxSize, cts.Token);
                    LoadCompleted?.Invoke(this, new SurfaceLoadCompletedEventArgs(LoadedImageSourceLoadStatus.Success, null));
                }
                catch (Exception ex)
                {
                    LoadCompleted?.Invoke(this, new SurfaceLoadCompletedEventArgs(ConvertExceptionToStatus(ex), ex));
                }
            });
        }

        private CompositionSurfaceLoader(Uri uri, Size desiredMaxSize) : this()
        {
            graphicsDevice!.DispatcherQueue.TryEnqueue(Windows.System.DispatcherQueuePriority.Low, async () =>
            {
                try
                {
                    var streamRef = RandomAccessStreamReference.CreateFromUri(uri);
                    using var stream = await streamRef.OpenReadAsync().AsTask(cts.Token);
                    await DrawImageCore(stream, desiredMaxSize, cts.Token);
                    LoadCompleted?.Invoke(this, new SurfaceLoadCompletedEventArgs(LoadedImageSourceLoadStatus.Success, null));
                }
                catch (Exception ex)
                {
                    LoadCompleted?.Invoke(this, new SurfaceLoadCompletedEventArgs(ConvertExceptionToStatus(ex), ex));
                }
            });
        }

        #region Create Resource

        private void CreateDevice()
        {
            if (softwareD2dDeviceHolder == null)
            {
                softwareD2dDeviceHolder = new D2DDeviceHolder(true);

                var compositor = WindowsCompositionHelper.Compositor;
                var interop = compositor.As<Windows.Win32.System.WinRT.Composition.ICompositorInterop>();

                interop.CreateGraphicsDevice(softwareD2dDeviceHolder.D2D1Device, out graphicsDevice);
            }
        }

        private void CreateSurface()
        {
            surface = graphicsDevice!.CreateDrawingSurface2(
                new(0, 0),
                Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized,
                Windows.Graphics.DirectX.DirectXAlphaMode.Premultiplied);
        }

        #endregion Create Resource

        #region Draw Images

        private async Task DrawImageCore(IRandomAccessStream stream, Size desiredMaxSize, CancellationToken cancellationToken)
        {
            stream.Seek(0);

            var decoder = await BitmapDecoder.CreateAsync(stream)
                .AsTask(cancellationToken);

            var width = decoder.PixelWidth;
            var height = decoder.PixelHeight;

            var maxPixelWidth = desiredMaxSize.Width * decoder.DpiX / 96;
            var maxPixelHeight = desiredMaxSize.Height * decoder.DpiX / 96;

            var dWidth = 0d;
            var dHeight = 0d;

            if (maxPixelWidth == 0 && maxPixelHeight == 0)
            {
                dWidth = width;
                dHeight = height;
            }
            else if (maxPixelWidth != 0 && maxPixelHeight != 0)
            {
                dWidth = maxPixelWidth;
                dHeight = height / width * maxPixelWidth;

                if (dHeight > maxPixelHeight)
                {
                    dHeight = maxPixelHeight;
                    dWidth = width / height * maxPixelHeight;
                }
            }
            else if (maxPixelWidth != 0)
            {
                dWidth = maxPixelWidth;
                dHeight = height / width * maxPixelWidth;
            }
            else if (maxPixelHeight != 0)
            {
                dHeight = maxPixelHeight;
                dWidth = width / height * maxPixelHeight;
            }

            if (dWidth > maxPixelWidth || dHeight > maxPixelHeight)
            {
                dWidth = width;
                dHeight = height;
            }

            var dScaledWidth = dWidth * 96 / decoder.DpiX;
            var dScaledHeight = dHeight * 96 / decoder.DpiY;

            NaturalSize = new Size(width, height);
            DecodedPhysicalSize = new Size(dWidth, dHeight);
            DecodedSize = new Size(dScaledWidth, dScaledHeight);

            var transform = new BitmapTransform()
            {
                ScaledWidth = (uint)dWidth,
                ScaledHeight = (uint)dHeight
            };

            NaturalSize = new Size(width, height);

            var image = await decoder.GetSoftwareBitmapAsync(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Premultiplied,
                transform,
                ExifOrientationMode.RespectExifOrientation,
                ColorManagementMode.DoNotColorManage).AsTask(cancellationToken);

            var interop = surface.As<Windows.Win32.System.WinRT.Composition.ICompositionDrawingSurfaceInterop>();

            interop.Resize(new SIZE(image.PixelWidth, image.PixelHeight));

            DrawImage(interop, image, cancellationToken);

            unsafe static void DrawImage(Windows.Win32.System.WinRT.Composition.ICompositionDrawingSurfaceInterop _interop, SoftwareBitmap _softwareBitmap, CancellationToken _cancellationToken)
            {
                lock (graphicsDevice!)
                {
                    var iid = typeof(Windows.Win32.Graphics.Direct2D.ID2D1DeviceContext).GUID;
                    System.Drawing.Point updateOffset = default;

                    _interop.BeginDraw((RECT*)0, &iid, out var updateObject, &updateOffset);

                    try
                    {
                        var deviceContext = updateObject.As<Windows.Win32.Graphics.Direct2D.ID2D1DeviceContext>();

                        var offsetX = updateOffset.X * 96 / (float)_softwareBitmap.DpiX;
                        var offsetY = updateOffset.Y * 96 / (float)_softwareBitmap.DpiY;

                        Windows.Win32.Graphics.Direct2D.Common.D2D_MATRIX_3X2_F transform = default;
                        transform.Anonymous.Anonymous1.m11 = 1f;
                        transform.Anonymous.Anonymous1.m22 = 1f;
                        transform.Anonymous.Anonymous1.dx = offsetX;
                        transform.Anonymous.Anonymous1.dy = offsetY;

                        deviceContext.SetTransform(&transform);
                        deviceContext.SetDpi((float)_softwareBitmap.DpiX, (float)_softwareBitmap.DpiY);

                        _cancellationToken.ThrowIfCancellationRequested();

                        var bitmap = CreateBitmap(_softwareBitmap, deviceContext);

                        _cancellationToken.ThrowIfCancellationRequested();

                        deviceContext.DrawBitmap(
                            bitmap,
                            (Windows.Win32.Graphics.Direct2D.Common.D2D_RECT_F*)0,
                            1,
                            Windows.Win32.Graphics.Direct2D.D2D1_BITMAP_INTERPOLATION_MODE.D2D1_BITMAP_INTERPOLATION_MODE_LINEAR,
                            (Windows.Win32.Graphics.Direct2D.Common.D2D_RECT_F*)0);
                    }
                    finally
                    {
                        _interop.EndDraw();
                    }
                }
            }

            unsafe static Windows.Win32.Graphics.Direct2D.ID2D1Bitmap1 CreateBitmap(SoftwareBitmap _softwareBitmap, Windows.Win32.Graphics.Direct2D.ID2D1DeviceContext _deviceContext)
            {
                var _props = new Windows.Win32.Graphics.Direct2D.D2D1_BITMAP_PROPERTIES()
                {
                    pixelFormat = new Windows.Win32.Graphics.Direct2D.Common.D2D1_PIXEL_FORMAT()
                    {
                        alphaMode = Windows.Win32.Graphics.Direct2D.Common.D2D1_ALPHA_MODE.D2D1_ALPHA_MODE_PREMULTIPLIED,
                        format = Windows.Win32.Graphics.Dxgi.Common.DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM,
                    },
                    dpiX = (float)_softwareBitmap.DpiX,
                    dpiY = (float)_softwareBitmap.DpiY,
                };

                using var _buffer = _softwareBitmap.LockBuffer(BitmapBufferAccessMode.Read);

                var _reference = _buffer.CreateReference();
                var _bitmapDesc = _buffer.GetPlaneDescription(0);

                var _pBuffer = (byte*)0;

                _reference.As<Windows.Win32.System.WinRT.IMemoryBufferByteAccess>().GetBuffer(&_pBuffer, out uint _bufferSize);

                _deviceContext.CreateBitmap(
                    new Windows.Win32.Graphics.Direct2D.Common.D2D_SIZE_U()
                    {
                        width = (uint)_softwareBitmap.PixelWidth,
                        height = (uint)_softwareBitmap.PixelHeight
                    },
                    (void*)(_pBuffer + _bitmapDesc.StartIndex),
                    (uint)_bitmapDesc.Stride,
                    &_props,
                    out var _bitmap);

                return (Windows.Win32.Graphics.Direct2D.ID2D1Bitmap1)_bitmap;
            }
        }

        #endregion Draw Images


        public ICompositionSurface Surface => surface!;

        public Size DecodedPhysicalSize { get; private set; }

        public Size DecodedSize { get; private set; }

        public Size NaturalSize { get; private set; }

        public event SurfaceLoadCompletedEventHandler? LoadCompleted;


        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                cts?.Cancel();
                surface?.Dispose();
                disposedValue = true;
            }
        }

        ~CompositionSurfaceLoader()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }


        #region Utilities

        private static LoadedImageSourceLoadStatus ConvertExceptionToStatus(Exception ex)
        {
            if (ex is HttpRequestException) return LoadedImageSourceLoadStatus.NetworkError;
            else if (ex.HResult == unchecked((int)0x88982f07)) return LoadedImageSourceLoadStatus.InvalidFormat;

            return LoadedImageSourceLoadStatus.Other;
        }

        #endregion Utilities
    }
    internal class D2DDeviceHolder
    {
        private Windows.Win32.Graphics.Direct2D.ID2D1Factory2 d2D1Factory2;
        private Windows.Win32.Graphics.Direct3D11.ID3D11Device d3d11Device;

        public unsafe D2DDeviceHolder(bool useSoftwareRenderer)
        {
            var options = new Windows.Win32.Graphics.Direct2D.D2D1_FACTORY_OPTIONS()
            {
                debugLevel = Windows.Win32.Graphics.Direct2D.D2D1_DEBUG_LEVEL.D2D1_DEBUG_LEVEL_NONE,
            };

            ExceptionHelpers.ThrowExceptionForHR(PInvoke.D2D1CreateFactory(
                Windows.Win32.Graphics.Direct2D.D2D1_FACTORY_TYPE.D2D1_FACTORY_TYPE_MULTI_THREADED,
                typeof(Windows.Win32.Graphics.Direct2D.ID2D1Factory2).GUID,
                options,
                out var raw));

            d2D1Factory2 = (Windows.Win32.Graphics.Direct2D.ID2D1Factory2)raw;

            Windows.Win32.Graphics.Direct3D.D3D_DRIVER_TYPE driverType = useSoftwareRenderer ?
                Windows.Win32.Graphics.Direct3D.D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_WARP :
                Windows.Win32.Graphics.Direct3D.D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_HARDWARE;

            var flag = Windows.Win32.Graphics.Direct3D11.D3D11_CREATE_DEVICE_FLAG.D3D11_CREATE_DEVICE_BGRA_SUPPORT;

            var featureLevels = new[]
            {
                Windows.Win32.Graphics.Direct3D.D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_1,
                Windows.Win32.Graphics.Direct3D.D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_0,
                Windows.Win32.Graphics.Direct3D.D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_10_1,
                Windows.Win32.Graphics.Direct3D.D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_10_0,
                Windows.Win32.Graphics.Direct3D.D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_9_3,
                Windows.Win32.Graphics.Direct3D.D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_9_2,
                Windows.Win32.Graphics.Direct3D.D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_9_1
            };

            Windows.Win32.Graphics.Direct3D.D3D_FEATURE_LEVEL outFeatureLevel = default;

            fixed (Windows.Win32.Graphics.Direct3D.D3D_FEATURE_LEVEL* pFeatureLevels = featureLevels)
            {
                ExceptionHelpers.ThrowExceptionForHR(PInvoke.D3D11CreateDevice(
                    null,
                    driverType,
                    HMODULE.Null,
                    flag,
                    pFeatureLevels,
                    (uint)featureLevels.Length,
                    PInvoke.D3D11_SDK_VERSION,
                    out d3d11Device,
                    &outFeatureLevel,
                    out _));
            }

            DXGIDevice = d3d11Device.As<Windows.Win32.Graphics.Dxgi.IDXGIDevice3>();

            D2D1Factory2.CreateDevice(DXGIDevice, out Windows.Win32.Graphics.Direct2D.ID2D1Device1 d2d1Device);
            D2D1Device = d2d1Device;
        }

        public Windows.Win32.Graphics.Direct2D.ID2D1Factory2 D2D1Factory2 => d2D1Factory2;

        public Windows.Win32.Graphics.Direct3D11.ID3D11Device D3D11Device => d3d11Device;

        public Windows.Win32.Graphics.Dxgi.IDXGIDevice3 DXGIDevice { get; }

        public Windows.Win32.Graphics.Direct2D.ID2D1Device1 D2D1Device { get; }
    }

    public delegate void SurfaceLoadCompletedEventHandler(CompositionSurfaceLoader sender, SurfaceLoadCompletedEventArgs args);

    public record SurfaceLoadCompletedEventArgs(LoadedImageSourceLoadStatus Status, Exception? Exception);

}
