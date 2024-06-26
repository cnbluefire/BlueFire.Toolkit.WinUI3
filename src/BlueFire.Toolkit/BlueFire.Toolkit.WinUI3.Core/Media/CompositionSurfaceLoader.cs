﻿using System;
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
using BlueFire.Toolkit.WinUI3.Extensions;
using Windows.Win32.System.Com;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage;

namespace BlueFire.Toolkit.WinUI3.Media
{
    /// <summary>
    /// Represents a <see cref="Windows.UI.Composition.ICompositionSurface"/> that an image can be downloaded, decoded and loaded onto. You can load an image using a Uniform Resource Identifier (URI) that references an image source file, or supplying a IRandomAccessStream.
    /// <para>
    /// see also <seealso cref="Microsoft.UI.Xaml.Media.LoadedImageSurface"/>
    /// </para>
    /// </summary>
    public class CompositionSurfaceLoader : IDisposable
    {
        private const int MaxTextureSizeFallback = 2045;
        private const int TileSize = 504;
        private const int HardMaximumTileCount = 150;

        private bool isVirtual;

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

        private CompositionSurfaceLoader(Size desiredMaxSize)
        {
            CreateDevice();
            CreateSurface(desiredMaxSize);
            cts = new CancellationTokenSource();
        }

        private CompositionSurfaceLoader(IRandomAccessStream randomAccessStream, Size desiredMaxSize) : this(desiredMaxSize)
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

        private CompositionSurfaceLoader(Uri uri, Size desiredMaxSize) : this(desiredMaxSize)
        {
            graphicsDevice!.DispatcherQueue.TryEnqueue(Windows.System.DispatcherQueuePriority.Low, async () =>
            {
                try
                {
                    var streamRef = await CreateStreamReferenceFromUri(uri);
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

        private static async Task<RandomAccessStreamReference> CreateStreamReferenceFromUri(Uri uri)
        {
            if (string.Equals(uri.Scheme, "ms-resource", StringComparison.OrdinalIgnoreCase))
            {
                var candidate = Resources.ResourceManagerFactory.ResourceManager.MainResourceMap.GetValue(uri.AbsolutePath);
                if (candidate != null)
                {
                    if (candidate.Kind == Microsoft.Windows.ApplicationModel.Resources.ResourceCandidateKind.EmbeddedData)
                    {
                        var bytes = candidate.ValueAsBytes;
                        var stream = new InMemoryRandomAccessStream();                        
                        await stream.WriteAsync(bytes.AsBuffer());
                        await stream.FlushAsync();
                        stream.Seek(0);
                        return RandomAccessStreamReference.CreateFromStream(stream);
                    }
                    else if (candidate.Kind == Microsoft.Windows.ApplicationModel.Resources.ResourceCandidateKind.FilePath)
                    {
                        var file = await StorageFile.GetFileFromPathAsync(candidate.ValueAsString);
                        return RandomAccessStreamReference.CreateFromFile(file);
                    }
                }
                throw new FileNotFoundException(uri.ToString());
            }
            else
            {
                return RandomAccessStreamReference.CreateFromUri(uri);
            }
        }

        #region Create Resource

        private unsafe void CreateDevice()
        {
            if (softwareD2dDeviceHolder == null)
            {
                softwareD2dDeviceHolder = new D2DDeviceHolder(true);

                ComPtr<Windows.Win32.System.WinRT.Composition.ICompositorInterop> interop = default;
                nint pGraphicsDevice = 0;

                try
                {
                    ComObjectHelper.QueryInterface(
                        WindowsCompositionHelper.Compositor,
                        Windows.Win32.System.WinRT.Composition.ICompositorInterop.IID_Guid,
                        out interop);


                    ((delegate* unmanaged[Stdcall]<Windows.Win32.System.WinRT.Composition.ICompositorInterop*, void*, void**, HRESULT>)(*(void***)interop.AsPointer())[5])(
                        interop.AsTypedPointer(),
                        softwareD2dDeviceHolder.D2D1Device.AsPointer(),
                        (void**)(&pGraphicsDevice));

                    graphicsDevice = CompositionGraphicsDevice.FromAbi(pGraphicsDevice);
                }
                finally
                {
                    ComPtr<IUnknown>.Attach(pGraphicsDevice).Release();
                    interop.Release();
                }
            }
        }

        private void CreateSurface(Size desiredMaxSize)
        {
            isVirtual = IsVirtualPossible(desiredMaxSize);

            if (isVirtual)
            {
                surface = graphicsDevice!.CreateVirtualDrawingSurface(
                    new(0, 0),
                    Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized,
                    Windows.Graphics.DirectX.DirectXAlphaMode.Premultiplied);
            }
            else
            {
                surface = graphicsDevice!.CreateDrawingSurface2(
                    new(0, 0),
                    Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized,
                    Windows.Graphics.DirectX.DirectXAlphaMode.Premultiplied);
            }
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

            ComPtr<Windows.Win32.System.WinRT.Composition.ICompositionDrawingSurfaceInterop> interop = default;

            if (surface != null)
            {
                try
                {
                    ComObjectHelper.QueryInterface(surface, Windows.Win32.System.WinRT.Composition.ICompositionDrawingSurfaceInterop.IID_Guid, out interop);

                    interop.Value.Resize(new SIZE(image.PixelWidth, image.PixelHeight));

                    DrawImage(interop, image, isVirtual, cancellationToken);
                }
                finally
                {
                    interop.Release();
                }
            }

            unsafe static void DrawImage(ComPtr<Windows.Win32.System.WinRT.Composition.ICompositionDrawingSurfaceInterop> _interop, SoftwareBitmap _softwareBitmap, bool _isVirtual, CancellationToken _cancellationToken)
            {
                lock (graphicsDevice!)
                {
                    var iid = typeof(Windows.Win32.Graphics.Direct2D.ID2D1DeviceContext).GUID;
                    System.Drawing.Point updateOffset = default;

                    ComPtr<Windows.Win32.Graphics.Direct2D.ID2D1Bitmap1> bitmap = default;
                    try
                    {
                        var imageWidth = _softwareBitmap.PixelWidth;
                        var imageHeight = _softwareBitmap.PixelHeight;

                        var maxTileSize = TileSize;
                        if (!_isVirtual)
                        {
                            maxTileSize = MaxTextureSizeFallback;
                        }

                        for (int tileY = 0; tileY < imageHeight; tileY += maxTileSize)
                        {
                            var tileHeight = Math.Min(imageHeight - tileY, maxTileSize);

                            for (int tileX = 0; tileX < imageWidth; tileX += maxTileSize)
                            {
                                var tileWidth = Math.Min(imageWidth - tileX, maxTileSize);

                                var rect = new Windows.Win32.Graphics.Direct2D.Common.D2D_RECT_F()
                                {
                                    left = tileX,
                                    top = tileY,
                                    right = tileX + tileWidth,
                                    bottom = tileY + tileHeight
                                };

                                var updateRect = new Windows.Win32.Foundation.RECT(tileX, tileY, tileX + tileWidth, tileY + tileHeight);

                                ComPtr<Windows.Win32.Graphics.Direct2D.ID2D1DeviceContext> deviceContext = default;
                                try
                                {
                                    _interop.Value.BeginDraw(&updateRect, &iid, deviceContext.PointerRef, &updateOffset)
                                        .ThrowOnFailure();

                                    var offsetX = updateOffset.X * 96 / (float)_softwareBitmap.DpiX;
                                    var offsetY = updateOffset.Y * 96 / (float)_softwareBitmap.DpiY;

                                    Windows.Win32.Graphics.Direct2D.Common.D2D_MATRIX_3X2_F transform = default;
                                    transform.Anonymous.Anonymous1.m11 = 1f;
                                    transform.Anonymous.Anonymous1.m22 = 1f;
                                    transform.Anonymous.Anonymous1.dx = offsetX;
                                    transform.Anonymous.Anonymous1.dy = offsetY;

                                    deviceContext.Value.SetTransform(&transform);
                                    deviceContext.Value.SetDpi((float)_softwareBitmap.DpiX, (float)_softwareBitmap.DpiY);

                                    if (!bitmap.HasValue)
                                    {
                                        bitmap = CreateBitmap(_softwareBitmap, deviceContext);
                                    }

                                    _cancellationToken.ThrowIfCancellationRequested();

                                    var destRect = rect;
                                    destRect.right -= destRect.left;
                                    destRect.bottom -= destRect.top;
                                    destRect.left = 0;
                                    destRect.top = 0;

                                    deviceContext.Value.DrawBitmap(
                                        bitmap.AsTypedPointer<Windows.Win32.Graphics.Direct2D.ID2D1Bitmap>(),
                                        &destRect,
                                        1,
                                        Windows.Win32.Graphics.Direct2D.D2D1_BITMAP_INTERPOLATION_MODE.D2D1_BITMAP_INTERPOLATION_MODE_LINEAR,
                                        &rect);
                                }
                                finally
                                {
                                    deviceContext.Release();
                                    deviceContext = default;

                                    _interop.Value.EndDraw();
                                }
                            }
                        }
                    }
                    finally
                    {
                        bitmap.Release();
                    }
                }
            }

            unsafe static ComPtr<Windows.Win32.Graphics.Direct2D.ID2D1Bitmap1> CreateBitmap(SoftwareBitmap _softwareBitmap, ComPtr<Windows.Win32.Graphics.Direct2D.ID2D1DeviceContext> _deviceContext)
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
                uint _bufferSize = 0;

                ComPtr<Windows.Win32.System.WinRT.IMemoryBufferByteAccess> _memoryBufferByteAccess = default;

                try
                {
                    ComObjectHelper.QueryInterface<Windows.Win32.System.WinRT.IMemoryBufferByteAccess>(
                        _reference,
                        Windows.Win32.System.WinRT.IMemoryBufferByteAccess.IID_Guid,
                        out _memoryBufferByteAccess);

                    _memoryBufferByteAccess.Value.GetBuffer(&_pBuffer, &_bufferSize);

                    using (ComPtr<Windows.Win32.Graphics.Direct2D.ID2D1Bitmap> bitmap = default)
                    {
                        _deviceContext.Value.CreateBitmap(
                            new Windows.Win32.Graphics.Direct2D.Common.D2D_SIZE_U()
                            {
                                width = (uint)_softwareBitmap.PixelWidth,
                                height = (uint)_softwareBitmap.PixelHeight
                            },
                            (void*)(_pBuffer + _bitmapDesc.StartIndex),
                            (uint)_bitmapDesc.Stride,
                            &_props,
                            bitmap.TypedPointerRef);

                        fixed (Guid* IID_ID2D1Bitmap1 = &Windows.Win32.Graphics.Direct2D.ID2D1Bitmap1.IID_Guid)
                        {
                            nint result = 0;
                            bitmap.QueryInterface(IID_ID2D1Bitmap1, (void**)(&result));

                            return ComPtr<Windows.Win32.Graphics.Direct2D.ID2D1Bitmap1>.Attach(result);
                        }
                    }
                }
                finally
                {
                    _memoryBufferByteAccess.Release();
                }
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

        private static bool IsVirtualPossible(Size maxKnownPixelSize)
        {
            bool assumeVirtual = true;

            if (maxKnownPixelSize.Width > 0 && maxKnownPixelSize.Height > 0)
            {
                assumeVirtual = maxKnownPixelSize.Width > MaxTextureSizeFallback ||
                    maxKnownPixelSize.Height > MaxTextureSizeFallback;
            }

            return assumeVirtual;
        }

        #endregion Utilities
    }


    internal class D2DDeviceHolder
    {
        private ComPtr<Windows.Win32.Graphics.Direct2D.ID2D1Factory2> d2D1Factory2;
        private ComPtr<Windows.Win32.Graphics.Direct3D11.ID3D11Device> d3d11Device;
        private ComPtr<Windows.Win32.Graphics.Dxgi.IDXGIDevice3> dxgiDevice;
        private ComPtr<Windows.Win32.Graphics.Direct2D.ID2D1Device1> d2d1Device;

        public unsafe D2DDeviceHolder(bool useSoftwareRenderer)
        {
            var options = new Windows.Win32.Graphics.Direct2D.D2D1_FACTORY_OPTIONS()
            {
                debugLevel = Windows.Win32.Graphics.Direct2D.D2D1_DEBUG_LEVEL.D2D1_DEBUG_LEVEL_NONE,
            };

            fixed (Guid* IID_ID2D1Factory2 = &Windows.Win32.Graphics.Direct2D.ID2D1Factory2.IID_Guid)
            {
                PInvoke.D2D1CreateFactory(
                    Windows.Win32.Graphics.Direct2D.D2D1_FACTORY_TYPE.D2D1_FACTORY_TYPE_MULTI_THREADED,
                    IID_ID2D1Factory2,
                    &options,
                    d2D1Factory2.PointerRef).ThrowOnFailure();
            }

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
                PInvoke.D3D11CreateDevice(
                    null,
                    driverType,
                    HMODULE.Null,
                    flag,
                    pFeatureLevels,
                    (uint)featureLevels.Length,
                    PInvoke.D3D11_SDK_VERSION,
                    d3d11Device.TypedPointerRef,
                    &outFeatureLevel,
                    (Windows.Win32.Graphics.Direct3D11.ID3D11DeviceContext**)0).ThrowOnFailure();
            }

            fixed (Guid* IID_IDXGIDevice3 = &Windows.Win32.Graphics.Dxgi.IDXGIDevice3.IID_Guid)
            {
                d3d11Device.QueryInterface(IID_IDXGIDevice3, dxgiDevice.PointerRef);
            }

            d2D1Factory2.Value.CreateDevice(dxgiDevice.AsTypedPointer<Windows.Win32.Graphics.Dxgi.IDXGIDevice>(), d2d1Device.TypedPointerRef);
        }

        public ComPtr<Windows.Win32.Graphics.Direct2D.ID2D1Factory2> D2D1Factory2 => d2D1Factory2;

        public ComPtr<Windows.Win32.Graphics.Direct3D11.ID3D11Device> D3D11Device => d3d11Device;

        public ComPtr<Windows.Win32.Graphics.Dxgi.IDXGIDevice3> DXGIDevice => dxgiDevice;

        public ComPtr<Windows.Win32.Graphics.Direct2D.ID2D1Device1> D2D1Device => d2d1Device;
    }

    public delegate void SurfaceLoadCompletedEventHandler(CompositionSurfaceLoader sender, SurfaceLoadCompletedEventArgs args);

    public record SurfaceLoadCompletedEventArgs(LoadedImageSourceLoadStatus Status, Exception? Exception);

}
