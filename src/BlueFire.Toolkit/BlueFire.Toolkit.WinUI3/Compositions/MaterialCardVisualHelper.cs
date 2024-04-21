using BlueFire.Toolkit.WinUI3.Extensions;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics;
using Windows.System;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Composition.Desktop;
using Windows.Win32;
using Windows.Win32.Foundation;
using WinRT;

namespace BlueFire.Toolkit.WinUI3.Compositions
{
    internal sealed class MaterialCardVisualHelper : IDisposable
    {
        private static readonly bool IsPixelSnappingEnabledSupported = Environment.OSVersion.Version >= new Version(10, 0, 20384, 0);

        private const float MaxBlurRadius = 72;

        private bool disposeValue;

        private Compositor compositor;

        private ContainerVisual rootVisual;

        private SpriteVisual? backgroundShadowHostVisual;
        private DropShadow? backgroundShadow;

        private CompositionGeometricClip? backgroundShadowClip;
        private CompositionPathGeometry? backgroundShadowClipGeometry;

        private CompositionPathGeometry? backgroundShadowHostGeometry;
        private CompositionSpriteShape? backgroundShadowHostSurfaceShape;
        private ShapeVisual? backgroundShadowHostSurfaceVisual;
        private CompositionVisualSurface? backgroundShadowHostSurface;
        private CompositionSurfaceBrush? backgroundShadowHostSurfaceBrush;

        private CompositionVisualSurface backdropSurface;
        private ContainerVisual blurAndShadowContainer;

        private CompositionPathGeometry? hostBrushVisualClipGeometry;
        private SpriteVisual? hostBrushVisual;

        private CompositionPathGeometry? borderGeometry;
        private CompositionColorBrush? borderBrush;
        private CompositionSpriteShape? borderShape;
        private ShapeVisual? borderVisual;

        private Thickness margin = new Thickness(0);
        private double cornerRadius = 8d;
        private double borderThickness = 1d;
        private Color shadowColor = Color.FromArgb(255, 0, 0, 0);
        private double shadowOpacity = 0.8d;
        private double shadowBlurRadius = 12f;
        private Vector2 shadowOffset = new Vector2(0, 3);

        internal const string scaleAnimationSize = "12f";
        internal static readonly TimeSpan opacityAnimationDuration = TimeSpan.FromSeconds(0.27);
        private ExpressionAnimation? centerPointBind;
        private ExpressionAnimation? scaleBind;

        private CompositionBrushProvider? brushProvider;
        private double rasterizationScale = 1d;
        private SizeInt32 hostSizeInPixels;


        public MaterialCardVisualHelper()
        {
            this.compositor = WindowsCompositionHelper.Compositor;

            rootVisual = compositor.CreateContainerVisual();
            rootVisual.RelativeSizeAdjustment = Vector2.One;

            backdropSurface = compositor.CreateVisualSurface();
            blurAndShadowContainer = compositor.CreateContainerVisual();

            InitializeLayoutRootShadow();
            InitializeBrushHostVisual();
            InitializeBorder();

            UpdateVisualSize();

            blurAndShadowContainer.Children.InsertAtTop(backgroundShadowHostVisual);
            blurAndShadowContainer.Children.InsertAtTop(hostBrushVisual);
            blurAndShadowContainer.Children.InsertAtTop(borderVisual);

            rootVisual.Children.InsertAtTop(blurAndShadowContainer);

            //centerPointBind = compositor.CreateExpressionAnimation("Vector3(this.Target.Size.X / 2, this.Target.Size.Y / 2, 0)");
            //blurAndShadowContainer.StartAnimation("CenterPoint", centerPointBind);

            //scaleBind = compositor.CreateExpressionAnimation($"Vector3(({scaleAnimationSize} / this.Target.Size.X) * (1 - this.Target.Opacity) + 1, ({scaleAnimationSize} / this.Target.Size.Y) * (1 - this.Target.Opacity) + 1, 1)");
            //blurAndShadowContainer.StartAnimation("Scale", scaleBind);

            //var imp = compositor.CreateImplicitAnimationCollection();
            //var opacityAn = compositor.CreateScalarKeyFrameAnimation();
            //opacityAn.InsertExpressionKeyFrame(1, "this.FinalValue");
            //opacityAn.Duration = opacityAnimationDuration;
            //opacityAn.Target = "Opacity";
            //imp[opacityAn.Target] = opacityAn;
            //blurAndShadowContainer.ImplicitAnimations = imp;
        }

        public Visual Visual => rootVisual;

        public double RasterizationScale
        {
            get => rasterizationScale;
            set
            {
                if (rasterizationScale != value)
                {
                    if (value < 0 || value > 20) throw new ArgumentException(null, nameof(RasterizationScale));
                    rasterizationScale = value;

                    UpdateRootVisualScale();
                }
            }
        }

        public SizeInt32 HostSizeInPixels
        {
            get => hostSizeInPixels;
            set
            {
                if (hostSizeInPixels != value)
                {
                    if (hostSizeInPixels.Width < 0 || hostSizeInPixels.Height < 0) throw new ArgumentException(null, nameof(HostSizeInPixels));
                    hostSizeInPixels = value;

                    UpdateRootVisualSize();
                }
            }
        }

        public CompositionBrushProvider? BrushProvider
        {
            get => brushProvider;
            set
            {
                if (brushProvider != value)
                {
                    var oldBrushProvider = brushProvider;
                    brushProvider = value;
                    UpdateBrush(value, oldBrushProvider);
                }
            }
        }


        public Thickness Margin
        {
            get => margin;
            set
            {
                if (margin != value)
                {
                    margin = value;
                    UpdateVisualSize();
                }
            }
        }

        public double CornerRadius
        {
            get => cornerRadius;
            set
            {
                if (cornerRadius != value)
                {
                    cornerRadius = value;
                    UpdateVisualSize();
                }
            }
        }

        public double Opacity
        {
            get => blurAndShadowContainer.Opacity;
            set
            {
                var v = (float)value;
                if (blurAndShadowContainer.Opacity != v)
                {
                    if (blurAndShadowContainer.Opacity == 0)
                    {
                        //acrylicHelper?.FlushBrush();
                    }
                    blurAndShadowContainer.Opacity = v;
                }
            }
        }

        public bool Visible
        {
            get => rootVisual.IsVisible;
            set => rootVisual.IsVisible = value;
        }

        public Windows.UI.Color BorderColor
        {
            get => borderBrush!.Color;
            set => borderBrush!.Color = value;
        }

        public double BorderThickness
        {
            get => borderThickness;
            set
            {
                if (borderThickness != value)
                {
                    if (value < 0) throw new ArgumentException(null, nameof(BorderThickness));
                    borderThickness = value;

                    UpdateVisualSize();
                }
            }
        }

        public Color ShadowColor
        {
            get => shadowColor;
            set
            {
                if (shadowColor != value)
                {
                    shadowColor = value;
                    UpdateShadowProperties();
                }
            }
        }

        public double ShadowOpacity
        {
            get => shadowOpacity;
            set
            {
                if (shadowOpacity != value)
                {
                    shadowOpacity = value;
                    UpdateShadowProperties();
                }
            }
        }

        public double ShadowBlurRadius
        {
            get => shadowBlurRadius;
            set
            {
                if (shadowBlurRadius != value)
                {
                    shadowBlurRadius = value;
                    UpdateShadowProperties();
                }
            }
        }

        public Vector2 ShadowOffset
        {
            get => shadowOffset;
            set
            {
                if (shadowOffset != value)
                {
                    shadowOffset = value;
                    UpdateShadowProperties();
                }
            }
        }

        private void UpdateShadowProperties()
        {
            if (backgroundShadow != null)
            {
                backgroundShadow.Offset = new Vector3(ShadowOffset, 0);

                backgroundShadow.Color = ShadowColor;
                backgroundShadow.Opacity = (float)ShadowOpacity;
                backgroundShadow.BlurRadius = (float)ShadowBlurRadius;
            }
        }

        private void InitializeLayoutRootShadow()
        {
            backgroundShadow = compositor.CreateDropShadow();
            backgroundShadow.Offset = new Vector3(0, 3, 0);
            backgroundShadow.BlurRadius = 12;
            backgroundShadow.Color = Windows.UI.Color.FromArgb(255, 0, 0, 0);
            backgroundShadow.Opacity = 0.8f;
            backgroundShadow.SourcePolicy = CompositionDropShadowSourcePolicy.InheritFromVisualContent;

            backgroundShadowClipGeometry = compositor.CreatePathGeometry();
            backgroundShadowClip = compositor.CreateGeometricClip(backgroundShadowClipGeometry);

            backgroundShadowHostGeometry = compositor.CreatePathGeometry();
            backgroundShadowHostSurfaceShape = compositor.CreateSpriteShape(backgroundShadowHostGeometry);
            backgroundShadowHostSurfaceShape.FillBrush = compositor.CreateColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255));

            backgroundShadowHostSurfaceVisual = compositor.CreateShapeVisual();
            backgroundShadowHostSurfaceVisual.Shapes.Add(backgroundShadowHostSurfaceShape);

            backgroundShadowHostSurface = compositor.CreateVisualSurface();
            backgroundShadowHostSurface.SourceVisual = backgroundShadowHostSurfaceVisual;

            backgroundShadowHostSurfaceBrush = compositor.CreateSurfaceBrush(backgroundShadowHostSurface);

            backgroundShadowHostSurfaceBrush.Stretch = CompositionStretch.Uniform;

            backgroundShadowHostVisual = compositor.CreateSpriteVisual();
            backgroundShadowHostVisual.Brush = backgroundShadowHostSurfaceBrush;
            backgroundShadowHostVisual.Shadow = backgroundShadow;
            backgroundShadowHostVisual.Clip = backgroundShadowClip;
        }

        private void InitializeBrushHostVisual()
        {
            hostBrushVisual = compositor.CreateSpriteVisual();
            hostBrushVisual.RelativeSizeAdjustment = Vector2.One;

            hostBrushVisualClipGeometry = compositor.CreatePathGeometry();

            hostBrushVisual.Clip = compositor.CreateGeometricClip(hostBrushVisualClipGeometry);
        }

        private void InitializeBorder()
        {
            borderBrush = compositor.CreateColorBrush(Windows.UI.Color.FromArgb(0, 255, 255, 255));

            borderGeometry = compositor.CreatePathGeometry();

            borderShape = compositor.CreateSpriteShape(borderGeometry);
            borderShape.StrokeBrush = borderBrush;
            borderShape.StrokeThickness = 0;

            borderVisual = compositor.CreateShapeVisual();
            borderVisual.RelativeSizeAdjustment = Vector2.One;
            borderVisual.Shapes.Add(borderShape);

            if (IsPixelSnappingEnabledSupported)
            {
                borderVisual.IsPixelSnappingEnabled = true;
            }
        }

        private void UpdateBrush(CompositionBrushProvider? newBrush, CompositionBrushProvider? oldBrush)
        {
            if (hostBrushVisual != null)
            {
                hostBrushVisual.Brush = newBrush?.Brush;
            }
        }

        private void UpdateVisualSize()
        {
            if (backgroundShadowHostVisual == null
                || backgroundShadowClipGeometry == null
                || backgroundShadowHostGeometry == null
                || hostBrushVisualClipGeometry == null
                || borderGeometry == null
                || borderShape == null) return;

            var scale = rasterizationScale;
            var pixelSize = hostSizeInPixels;

            var margins = (
                left: Math.Max(0, Margin.Left),
                top: Math.Max(0, Margin.Top),
                right: Math.Max(0, Margin.Right),
                bottom: Math.Max(0, Margin.Bottom));

            var width = pixelSize.Width / scale - margins.left - margins.right;
            var height = pixelSize.Height / scale - margins.top - margins.bottom;

            if (width <= 0 || height <= 0)
            {
                backgroundShadowHostVisual.IsVisible = false;
                return;
            }

            var size = new Vector2((float)width, (float)height);
            var offset = new Vector3((float)margins.left, (float)margins.top, 0);

            var radius = (float)(CornerRadius);

            blurAndShadowContainer.Offset = offset;
            blurAndShadowContainer.Size = size;
            backgroundShadowHostVisual.Size = size;
            backgroundShadowHostVisual.IsVisible = true;

            backdropSurface.SourceSize = size;

            var borderThickness = (float)this.borderThickness;

            if (IsPixelSnappingEnabledSupported)
            {
                borderShape.StrokeThickness = borderThickness;
            }
            else
            {
                var borderThicknessPixel = (int)Math.Round(borderThickness * scale);

                borderThickness = (float)(borderThicknessPixel / scale);
                borderShape.StrokeThickness = borderThickness;
            }

            if (backgroundShadowHostSurface != null && backgroundShadowHostSurfaceVisual != null)
            {
                backgroundShadowHostSurface.SourceSize = size;
                backgroundShadowHostSurfaceVisual.Size = size;
            }

            backgroundShadowHostVisual.IsVisible =
                margins.left != 0
                || margins.top != 0
                || margins.right != 0
                || margins.bottom != 0;

            if (width <= 1 || height <= 1)
            {
                backgroundShadowHostGeometry.Path = null;
                backgroundShadowClipGeometry.Path = null;
                hostBrushVisualClipGeometry.Path = null;
                borderGeometry.Path = null;
            }
            else
            {
                using (var geometry1 = CanvasGeometry.CreateRoundedRectangle(null, new Windows.Foundation.Rect(-MaxBlurRadius, -MaxBlurRadius, width + MaxBlurRadius * 2, height + MaxBlurRadius * 2), MaxBlurRadius, MaxBlurRadius))
                using (var geometry2 = CanvasGeometry.CreateRoundedRectangle(null, new Windows.Foundation.Rect(borderThickness, borderThickness, width - borderThickness * 2, height - borderThickness * 2), radius, radius))
                using (var geometry3 = geometry1.CombineWith(geometry2, Matrix3x2.Identity, CanvasGeometryCombine.Exclude))
                {
                    backgroundShadowClipGeometry.Path = new CompositionPath(geometry3);
                }
                using (var geometry = CanvasGeometry.CreateRoundedRectangle(null, new Windows.Foundation.Rect(borderThickness + 0.5, borderThickness + 0.5, width - 2 * (borderThickness + 0.5), height - 2 * (borderThickness + 0.5)), radius, radius))
                {
                    backgroundShadowHostGeometry.Path = new CompositionPath(geometry);
                }

                using (var geometry = CanvasGeometry.CreateRoundedRectangle(null, new Windows.Foundation.Rect(0, 0, width, height), radius, radius))
                {
                    hostBrushVisualClipGeometry.Path = new CompositionPath(geometry);
                }

                if (borderThickness > 0)
                {
                    using (var geometry = CanvasGeometry.CreateRoundedRectangle(null, new Windows.Foundation.Rect(borderThickness / 2, borderThickness / 2, width - borderThickness, height - borderThickness), radius - borderThickness / 2, radius - borderThickness / 2))
                    {
                        // 使用CanvasGeometry.CreateRoundedRectangle创建圆角矩形Geometry
                        // 使用Stroke呈现时，着色是以Geometry的线条为中线向两侧扩展着色
                        // CornerRadius此时是中线的角半径
                        // 如果需要设置外侧角半径为 radius 时，中线半径应为 radius - borderThickness / 2
                        // 此时内测半径为 radius - borderThickness

                        borderGeometry.Path = new CompositionPath(geometry);
                    }
                }
                else
                {
                    borderGeometry.Path = null;
                }

            }
        }


        #region Update Size

        [System.Diagnostics.DebuggerNonUserCode]
        private unsafe void WindowManager_WindowMessageReceived(object? sender, WindowMessageReceivedEventArgs e)
        {
            if (e.MessageId == PInvoke.WM_DPICHANGED)
            {
                compositor.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, UpdateRootVisualScale);
            }
            else if (e.MessageId == PInvoke.WM_SIZE)
            {
                compositor.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, UpdateRootVisualSize);
            }
            else if (e.MessageId == PInvoke.WM_WINDOWPOSCHANGED)
            {
                var wndpos = (Windows.Win32.UI.WindowsAndMessaging.WINDOWPOS*)e.LParam;
                var flag = wndpos->flags;

                if ((flag & (Windows.Win32.UI.WindowsAndMessaging.SET_WINDOW_POS_FLAGS.SWP_NOSIZE)) == 0)
                {
                    compositor.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, UpdateRootVisualSize);
                }
            }
            else if (e.MessageId == PInvoke.WM_SHOWWINDOW)
            {
                if (PInvoke.IsWindowVisible((HWND)Microsoft.UI.Win32Interop.GetWindowFromWindowId(e.WindowId)))
                {
                    compositor.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, UpdateRootVisualScale);
                }
            }
        }

        private void UpdateRootVisualScale()
        {
            var scale = (float)rasterizationScale;
            rootVisual.Scale = new Vector3(scale, scale, 1);

            UpdateRootVisualSize();
        }

        private void UpdateRootVisualSize()
        {
            var scale = (float)rasterizationScale;
            var pixelSize = hostSizeInPixels;

            var width = pixelSize.Width / scale;
            var height = pixelSize.Height / scale;
            rootVisual.Size = new Vector2(width, height);

            UpdateVisualSize();
        }

        #endregion Update Size

        public void Dispose()
        {
            if (!disposeValue)
            {
                disposeValue = true;

                rootVisual.Dispose();
                rootVisual = null!;

                //acrylicHelper?.Dispose();
                //acrylicHelper = null!;
            }
        }
    }
}
