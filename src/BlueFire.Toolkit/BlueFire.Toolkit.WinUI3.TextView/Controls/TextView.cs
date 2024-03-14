using BlueFire.Toolkit.WinUI3.Controls.FormattedTextRenderers;
using BlueFire.Toolkit.WinUI3.Core.Extensions;
using BlueFire.Toolkit.WinUI3.Graphics;
using BlueFire.Toolkit.WinUI3.Text;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Composition;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Portable;
using Windows.Foundation;
using Windows.Graphics;
using Windows.UI;
using WinRT;
using WinDispatcherQueueController = Windows.System.DispatcherQueueController;

namespace BlueFire.Toolkit.WinUI3.Controls
{
    public partial class TextView : FrameworkElement
    {
        /*
         RootVisual中承载每一行的Visual，每个Visual都使用MaskBrush做渐变 
         AlphaMaskSurface绘制纯白的完整文本
         */

        private FormattedText? formattedText;
        private CompositionDrawingSurface? alphaMaskSurface;
        private CompositionSurfaceBrush? alphaMaskSurfaceBrush;
        private CompositionGraphicsDeviceHolder graphicsDeviceHolder = CompositionGraphicsDeviceHolder.GlobalDeviceHolder;
        private FormattedTextProperties? currentTextProps;
        private DropShadow? dropShadow;
        //private IFormattedTextRenderer textRenderer;
        private bool drawFlag;

        private TextViewContentRenderer? contentRenderer;

        public TextView()
        {
            this.HorizontalAlignment = HorizontalAlignment.Left;
            this.VerticalAlignment = VerticalAlignment.Top;

            CreateCompositionResources();
            this.Loading += TextView_Loading;
            this.Unloaded += TextView_Unloaded;

            RegisterPropertyChangedCallback(FlowDirectionProperty, OnTextPropertyChanged);
            RegisterPropertyChangedCallback(UseLayoutRoundingProperty, OnTextPropertyChanged);

            CompositeFontManager.CompositeFontsChanged +=
                (new WeakEventListener<TextView, object, EventArgs>(
                    this,
                    (that, _sender, _args) => that.CompositeFontManager_CompositeFontsChanged(_sender, _args),
                    (weakEvent) => CompositeFontManager.CompositeFontsChanged -= weakEvent.OnEvent))
                .OnEvent;
        }

        private void TextView_Loading(FrameworkElement sender, object args)
        {
            CreateCompositionResources();
            graphicsDeviceHolder.CompositionGraphicsDevice.RenderingDeviceReplaced -= CompositionGraphicsDevice_RenderingDeviceReplaced;
            graphicsDeviceHolder.CompositionGraphicsDevice.RenderingDeviceReplaced += CompositionGraphicsDevice_RenderingDeviceReplaced;
            XamlRoot.Changed -= XamlRoot_Changed;
            XamlRoot.Changed += XamlRoot_Changed;
        }

        private void TextView_Unloaded(object sender, RoutedEventArgs e)
        {
            XamlRoot.Changed -= XamlRoot_Changed;
            graphicsDeviceHolder.CompositionGraphicsDevice.RenderingDeviceReplaced -= CompositionGraphicsDevice_RenderingDeviceReplaced;
        }

        private void XamlRoot_Changed(XamlRoot sender, XamlRootChangedEventArgs args)
        {
            var prop = currentTextProps;
            if (!prop.HasValue || prop.Value.ImmutableProperties.RasterizationScale != sender.RasterizationScale)
            {
                // DPI Changed
                MakeDirty();
            }
        }

        [MemberNotNull(nameof(alphaMaskSurface), nameof(alphaMaskSurfaceBrush), nameof(contentRenderer)/*, nameof(textRenderer)*/)]
        private void CreateCompositionResources()
        {
            if (alphaMaskSurface == null)
            {
                alphaMaskSurface = graphicsDeviceHolder.CompositionGraphicsDevice
                    .CreateDrawingSurface(
                        new Size(0, 0),
                        Microsoft.Graphics.DirectX.DirectXPixelFormat.R8G8B8A8UIntNormalized,
                        Microsoft.Graphics.DirectX.DirectXAlphaMode.Premultiplied);
            }

            if (alphaMaskSurfaceBrush == null)
            {
                alphaMaskSurfaceBrush = graphicsDeviceHolder.Compositor
                    .CreateSurfaceBrush(alphaMaskSurface);

                alphaMaskSurfaceBrush.Stretch = CompositionStretch.None;
                alphaMaskSurfaceBrush.HorizontalAlignmentRatio = 0;
                alphaMaskSurfaceBrush.VerticalAlignmentRatio = 0;
            }

            if (dropShadow == null)
            {
                dropShadow = graphicsDeviceHolder.Compositor.CreateDropShadow();
                dropShadow.SourcePolicy = CompositionDropShadowSourcePolicy.InheritFromVisualContent;
            }

            //if (textRenderer == null)
            //{
            //    //textRenderer = new FormattedTextRenderer<ShapeVisualRendererLineVisual>(graphicsDeviceHolder);
            //    textRenderer = new FormattedTextRenderer<DrawingSurfaceRendererLineVisual>(graphicsDeviceHolder);
            //    textRenderer.DropShadow = dropShadow;
            //    ElementCompositionPreview.SetElementChildVisual(this, textRenderer.RootVisual);
            //}

            if (contentRenderer == null)
            {
                contentRenderer = new TextViewContentRenderer(this, graphicsDeviceHolder);
                ElementCompositionPreview.SetElementChildVisual(this, contentRenderer.RootVisual);
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            using (graphicsDeviceHolder.Lock())
            {
                // 计算布局尺寸，行信息
                return UpdateTextLayout(availableSize);
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (formattedText != null)
            {
                var newSize = new Size(formattedText.Width + StrokeThickness * 2, formattedText.Height + StrokeThickness * 2);

                var prop = currentTextProps;
                if (prop.HasValue)
                {
                    contentRenderer?.UpdateTextProperties(formattedText, prop.Value);
                }

                return newSize;
            }

            return new Size(0, 0);

            //using (graphicsDeviceHolder.Lock())
            //{
            //    // TODO: 更新IsTrimmed属性
            //    return UpdateLineVisuals();
            //}
        }

        public CompositionBrush? GetAlphaMask()
        {
            return alphaMaskSurfaceBrush;
        }

        internal FormattedText? GetFormattedTextInternal() => formattedText;

        private Size UpdateTextLayout(Size availableSize)
        {
            ThrowOnUnlocked();

            if (XamlRoot == null) return new Size(0, 0);

            var device = graphicsDeviceHolder.CanvasDevice;
            if (device == null || device.IsDeviceLost()) return new Size(0, 0);
            device.Trim();

            var props = GetCurrentTextProperties();

            if (props.ImmutableProperties.Text == null) throw new ArgumentException(nameof(Text));

            if (ShouldRecreateFormattedText(in currentTextProps, props))
            {
                formattedText?.Dispose();
                formattedText = null;
            }
            else if (currentTextProps != props)
            {
                drawFlag = true;
            }

            currentTextProps = props;
            bool createFlag = false;

            var textLayoutSize = new Size(availableSize.Width - props.ImmutableProperties.StrokeThickness * 2,
                availableSize.Height - props.ImmutableProperties.StrokeThickness * 2);

            if (formattedText == null)
            {
                formattedText = new FormattedText(
                    props.ImmutableProperties.Text,
                    null,
                    props.ImmutableProperties.FlowDirection,
                    props.ImmutableProperties.Typeface,
                    props.ImmutableProperties.FontSize,
                    true,
                    props.ImmutableProperties.IsColorFontEnabled)
                {
                    MaxTextWidth = textLayoutSize.Width,
                    MaxTextHeight = textLayoutSize.Height
                };

                createFlag = true;
                drawFlag = true;
            }

            formattedText.TextWrapping = props.ImmutableProperties.TextWrapping;
            formattedText.TextTrimming = props.ImmutableProperties.TextTrimming;
            formattedText.TextAlignment = props.ImmutableProperties.HorizontalTextAlignment;

            var oldSize = new Size(formattedText.Width + StrokeThickness * 2, formattedText.Height + StrokeThickness * 2);
            if (!createFlag)
            {
                formattedText.MaxTextWidth = textLayoutSize.Width;
                formattedText.MaxTextHeight = textLayoutSize.Height;
            }

            var newSize = new Size(formattedText.Width + StrokeThickness * 2, formattedText.Height + StrokeThickness * 2);

            if (oldSize != newSize)
            {
                drawFlag = true;
            }

            return newSize;
        }

        //private Size UpdateLineVisuals()
        //{
        //    ThrowOnUnlocked();

        //    //if (rootVisual == null) return new Size(0, 0);
        //    if (textRenderer == null) return new Size(0, 0);
        //    if (formattedText == null) return new Size(0, 0);

        //    var propsWrapper = currentTextProps;
        //    if (!propsWrapper.HasValue) return new Size(0, 0);

        //    var props = propsWrapper.Value;
        //    var size = new Size(formattedText.Width + StrokeThickness * 2, formattedText.Height + StrokeThickness * 2);

        //    var device = graphicsDeviceHolder.CanvasDevice;
        //    if (device == null || device.IsDeviceLost()) return new Size(0, 0);

        //    if (!drawFlag)
        //    {
        //        return size;
        //    }

        //    try
        //    {
        //        if (XamlRoot == null) return new Size(0, 0);
        //        if (alphaMaskSurface == null) return new Size(0, 0);
        //        if (alphaMaskSurfaceBrush == null) return new Size(0, 0);

        //        var scale = (float)props.ImmutableProperties.RasterizationScale;
        //        var dpi = (float)(96 * scale);
        //        var pixelWidth = GetPixelSize(size.Width, dpi);
        //        var pixelHeight = GetPixelSize(size.Height, dpi);

        //        //alphaMaskSurface.Resize(new SizeInt32(pixelWidth, pixelHeight));

        //        //using (var ds = CanvasComposition.CreateDrawingSession(
        //        //    alphaMaskSurface,
        //        //    new Rect(0, 0, pixelWidth, pixelHeight),
        //        //    dpi))
        //        //{
        //        //    ds.DrawTextLayout(
        //        //        formattedText.GetInternalCanvasTextLayout(),
        //        //        default,
        //        //        Color.FromArgb(255, 255, 255, 255));
        //        //}

        //        //alphaMaskSurfaceBrush.Scale = new Vector2(1 / scale, 1 / scale);

        //        var lineGlyphRuns = formattedText.LineGlyphRuns;

        //        textRenderer.Update(
        //            lineGlyphRuns,
        //            size,
        //            props.MutableProperties.TextColor,
        //            props.MutableProperties.TextSecondaryColor,
        //            props.MutableProperties.TextStrokeColor,
        //            props.MutableProperties.TextStrokeSecondaryColor,
        //            props.ImmutableProperties.StrokeThickness,
        //            props.ImmutableProperties.RasterizationScale,
        //            props.ImmutableProperties.IsColorFontEnabled);

        //        drawFlag = false;
        //        return size;
        //    }
        //    catch (Exception ex) when (device.IsDeviceLost(ex.HResult))
        //    {
        //    }

        //    return new Size(0, 0);

        //    int GetPixelSize(double pointSize, float dpi)
        //    {
        //        return (int)(pointSize * dpi / 96);
        //    }
        //}

        private void CompositionGraphicsDevice_RenderingDeviceReplaced(CompositionGraphicsDevice sender, RenderingDeviceReplacedEventArgs args)
        {
            MakeDirty();
        }

        private void CompositeFontManager_CompositeFontsChanged(object? sender, EventArgs e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                MakeDirty();
            });
        }

        private void MakeDirty()
        {
            currentTextProps = null;
            contentRenderer?.MakeDirty();

            // TODO: 合并多次调用
            InvalidateMeasure();
        }

        private void Redraw()
        {
            // 假定当前Visual信息是正确的，向Visual的Surface绘制行图像
            // 向AlphaMask的Surface绘制图像
            // 处理ColorFont
            InvalidateMeasure();
        }

        [Conditional("DEBUG")]
        private void ThrowOnUnlocked()
        {
            if (!graphicsDeviceHolder.IsLocked)
            {
                throw new InvalidOperationException(nameof(graphicsDeviceHolder.Lock));
            }
        }
    }
}
