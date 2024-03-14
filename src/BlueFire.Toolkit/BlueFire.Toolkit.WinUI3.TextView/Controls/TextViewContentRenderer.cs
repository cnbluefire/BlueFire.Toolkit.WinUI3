using BlueFire.Toolkit.WinUI3.Graphics;
using BlueFire.Toolkit.WinUI3.Text;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Composition;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics;
using Windows.Graphics.Effects;

namespace BlueFire.Toolkit.WinUI3.Controls
{
    internal class TextViewContentRenderer : IDisposable
    {
        private readonly CompositionGraphicsDeviceHolder graphicsDeviceHolder;

        private bool disposedValue;

        private readonly TextView textView;

        private TextViewDrawingContext drawingContext;
        private SpriteVisual textVisual;
        private SpriteVisual strokeVisual;
        private SpriteVisual colorFontVisual;

        private DrawingSurfaceBrushFactory textBrushFactory;
        private DrawingSurfaceBrushFactory strokeBrushFactory;
        private DrawingSurfaceBrushFactory colorFontFactory;

        private bool strokeVisible;
        private bool colorFontVisible;

        private TextView.FormattedTextProperties textProperties;
        private Vector2 layoutSize;

        internal TextViewContentRenderer(TextView textView, CompositionGraphicsDeviceHolder graphicsDeviceHolder)
        {
            this.textView = textView;
            this.graphicsDeviceHolder = graphicsDeviceHolder;

            drawingContext = new TextViewDrawingContext(textView, graphicsDeviceHolder);

            textBrushFactory = new DrawingSurfaceBrushFactory(graphicsDeviceHolder, true);
            strokeBrushFactory = new DrawingSurfaceBrushFactory(graphicsDeviceHolder, true);
            colorFontFactory = new DrawingSurfaceBrushFactory(graphicsDeviceHolder, false);

            textVisual = graphicsDeviceHolder.Compositor.CreateSpriteVisual();
            textVisual.Brush = textBrushFactory.Brush;

            strokeVisual = graphicsDeviceHolder.Compositor.CreateSpriteVisual();
            strokeVisual.RelativeSizeAdjustment = Vector2.One;
            strokeVisual.Brush = strokeBrushFactory.Brush;

            colorFontVisual = graphicsDeviceHolder.Compositor.CreateSpriteVisual();
            colorFontVisual.RelativeSizeAdjustment = Vector2.One;
            colorFontVisual.Brush = colorFontFactory.Brush;

            textBrushFactory.SetSourceBrush(graphicsDeviceHolder.Compositor.CreateColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)));
            strokeBrushFactory.SetSourceBrush(graphicsDeviceHolder.Compositor.CreateColorBrush(Windows.UI.Color.FromArgb(255, 0, 128, 255)));
        }

        internal SpriteVisual RootVisual => textVisual;

        internal void UpdateTextProperties(FormattedText formattedText, in TextView.FormattedTextProperties properties)
        {
            var oldProp = textProperties;
            var oldSize = layoutSize;

            textProperties = properties;
            var strokeThickness = (float)properties.ImmutableProperties.StrokeThickness;

            var layoutWidth = formattedText.Width + strokeThickness * 2;
            var layoutHeight = formattedText.Height + strokeThickness * 2;

            var newSize = new Vector2((float)layoutWidth, (float)layoutHeight);
            layoutSize = newSize;

            if (oldProp != properties || oldSize != newSize)
            {
                drawingContext.MakeDirty();

                // TODO: 判断是否需要渲染
                if (true)
                {
                    CompositionTarget.Rendering -= CompositionTarget_Rendering;
                    CompositionTarget.Rendering += CompositionTarget_Rendering;
                }
            }
        }

        internal void MakeDirty()
        {
            CompositionTarget.Rendering -= CompositionTarget_Rendering;

            textProperties = default;
            layoutSize = default;
        }

        private void CompositionTarget_Rendering(object? sender, object e)
        {
            CompositionTarget.Rendering -= CompositionTarget_Rendering;
            if (!UpdateText())
            {
                CompositionTarget.Rendering += CompositionTarget_Rendering;
            }
        }

        internal bool UpdateText()
        {
            var threadId = Thread.CurrentThread.ManagedThreadId;

            var formattedText = textView.GetFormattedTextInternal();
            var properties = this.textProperties;
            var strokeThickness = (float)properties.ImmutableProperties.StrokeThickness;

            if (formattedText != null)
            {
                var offset = new Vector2(strokeThickness, strokeThickness);

                var drawPixelWidth = (int)(layoutSize.X * properties.ImmutableProperties.RasterizationScale);
                var drawPixelHeight = (int)(layoutSize.Y * properties.ImmutableProperties.RasterizationScale);

                var surfaceScale = (float)(1 / properties.ImmutableProperties.RasterizationScale);
                textVisual.Size = layoutSize;
                textBrushFactory.SurfaceScale = surfaceScale;

                if (properties.ImmutableProperties.StrokeThickness > 0)
                {
                    if (!strokeVisible)
                    {
                        strokeVisible = true;
                        textVisual.Children.InsertAtBottom(strokeVisual);
                    }
                    strokeBrushFactory.SurfaceScale = surfaceScale;
                }
                else
                {
                    if (strokeVisible)
                    {
                        strokeVisible = false;
                        textVisual.Children.Remove(strokeVisual);
                    }
                }
                if (properties.ImmutableProperties.IsColorFontEnabled)
                {
                    if (!colorFontVisible)
                    {
                        colorFontVisible = true;
                        textVisual.Children.InsertAtTop(colorFontVisual);
                    }
                    colorFontFactory.SurfaceScale = surfaceScale;
                }
                else
                {
                    if (colorFontVisible)
                    {
                        colorFontVisible = false;
                        textVisual.Children.Remove(colorFontVisual);
                    }
                }

                if (drawPixelWidth > 0 && drawPixelHeight > 0)
                {
                    drawingContext.PrepareTexture(
                        new SizeInt32(drawPixelWidth, drawPixelHeight),
                        offset,
                        formattedText,
                        in properties);

                    using (graphicsDeviceHolder.Lock())
                    {
                        using (var drawingSession = textBrushFactory.CreateDrawingSession(new SizeInt32(drawPixelWidth, drawPixelHeight)))
                        {
                            drawingSession.Clear(Windows.UI.Color.FromArgb(0, 0, 0, 0));

                            drawingContext.RenderText(drawingSession);
                        }

                        if (properties.ImmutableProperties.StrokeThickness > 0)
                        {
                            using (var drawingSession = strokeBrushFactory.CreateDrawingSession(new SizeInt32(drawPixelWidth, drawPixelHeight)))
                            {
                                drawingSession.Clear(Windows.UI.Color.FromArgb(0, 0, 0, 0));

                                drawingContext.RenderStroke(drawingSession);
                            }
                        }

                        if (properties.ImmutableProperties.IsColorFontEnabled)
                        {
                            using (var drawingSession = colorFontFactory.CreateDrawingSession(new SizeInt32(drawPixelWidth, drawPixelHeight)))
                            {
                                drawingSession.Clear(Windows.UI.Color.FromArgb(0, 0, 0, 0));

                                drawingContext.RenderColorFont(drawingSession);
                            }
                        }
                    }
                }
            }
            return false;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        ~TextViewContentRenderer()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private class DrawingSurfaceBrushFactory : IDisposable
        {
            private readonly CompositionGraphicsDeviceHolder graphicsDeviceHolder;
            private readonly bool useAlphaMask;
            private bool disposedValue;
            private CompositionSurfaceBrush drawingSurfaceBrush;
            private CompositionEffectBrush? effectBrush;
            private CompositionBrush? realBrush;

            private CompositionDrawingSurface? drawingSurface;

            public DrawingSurfaceBrushFactory(
                CompositionGraphicsDeviceHolder graphicsDeviceHolder,
                bool useAlphaMask)
            {
                this.graphicsDeviceHolder = graphicsDeviceHolder;
                this.useAlphaMask = useAlphaMask;
                drawingSurfaceBrush = graphicsDeviceHolder.Compositor.CreateSurfaceBrush();
                drawingSurfaceBrush.HorizontalAlignmentRatio = 0;
                drawingSurfaceBrush.VerticalAlignmentRatio = 0;
                drawingSurfaceBrush.Stretch = CompositionStretch.None;

                if (useAlphaMask)
                {
                    effectBrush = CreateEffectBrush();
                    effectBrush.SetSourceParameter("drawingSurfaceBrush", drawingSurfaceBrush);
                }
            }

            internal float SurfaceScale
            {
                get => drawingSurfaceBrush.Scale.X;
                set => drawingSurfaceBrush.Scale = new Vector2(value, value);
            }

            internal CompositionBrush Brush => useAlphaMask ? effectBrush! : drawingSurfaceBrush;

            internal void SetSourceBrush(CompositionBrush? sourceBrush)
            {
                if (!useAlphaMask) throw new ArgumentException(nameof(sourceBrush));

                if (realBrush != sourceBrush)
                {
                    realBrush = sourceBrush;
                    effectBrush!.SetSourceParameter("realBrush", realBrush);
                }
            }

            internal CanvasDrawingSession CreateDrawingSession(SizeInt32 size)
            {
                if (size.Width <= 0 || size.Height <= 0) throw new ArgumentException("Width and height must be greater than zero", nameof(size));

                var drawingSurface = this.drawingSurface;

                if (drawingSurface == null)
                {
                    drawingSurface = graphicsDeviceHolder.CompositionGraphicsDevice.CreateVirtualDrawingSurface(
                        size,
                        Microsoft.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized,
                        Microsoft.Graphics.DirectX.DirectXAlphaMode.Premultiplied);

                    this.drawingSurface = drawingSurface;
                    drawingSurfaceBrush.Surface = drawingSurface;
                }
                else
                {
                    if (drawingSurface.SizeInt32 != size)
                    {
                        drawingSurface.Resize(size);
                    }
                }

                return CanvasComposition.CreateDrawingSession(drawingSurface);
            }

            private CompositionEffectBrush CreateEffectBrush()
            {
                using var effect = new AlphaMaskEffect()
                {
                    AlphaMask = new CompositionEffectSourceParameter("drawingSurfaceBrush"),
                    Source = new CompositionEffectSourceParameter("realBrush")
                };

                var factory = graphicsDeviceHolder.Compositor.CreateEffectFactory(effect);
                return factory.CreateBrush();
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        // TODO: 释放托管状态(托管对象)
                    }

                    // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                    // TODO: 将大型字段设置为 null
                    disposedValue = true;
                }
            }

            ~DrawingSurfaceBrushFactory()
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
