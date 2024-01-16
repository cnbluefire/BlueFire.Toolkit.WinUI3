using BlueFire.Toolkit.WinUI3.Text;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Composition;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics;
using Windows.UI;
using Windows.UI.Text;

namespace BlueFire.Toolkit.WinUI3.Controls.FormattedTextRenderers
{
    internal class DrawingSurfaceRendererLineVisual : FormattedTextRendererLineVisual
    {
        private const string FadeOutColorWidth = "10f";
        private static CanvasStrokeStyle? defaultStrokeStyle;

        private bool hasColorFont;

        private bool isColorFontEnabled;
        private double textStrokeThickness;

        private ContainerVisual rootVisual = null!;
        private LayerVisual layerVisual = null!;
        private SpriteVisual textVisual = null!;
        private SpriteVisual strokeVisual = null!;
        private SpriteVisual colorFontVisual = null!;

        private CompositionSurfaceBrush textBrush = null!;
        private CompositionSurfaceBrush strokeBrush = null!;
        private CompositionSurfaceBrush colorFontBrush = null!;
        private CompositionDrawingSurface textSurface = null!;
        private CompositionDrawingSurface strokeSurface = null!;
        private CompositionDrawingSurface colorFontSurface = null!;

        private CompositionMaskBrush textMaskBrush = null!;
        private CompositionMaskBrush strokeMaskBrush = null!;

        protected override void Initialize()
        {
            var compositor = DeviceHolder.Compositor;
            var graphicsDevice = DeviceHolder.CompositionGraphicsDevice;

            rootVisual = compositor.CreateContainerVisual();

            textSurface = graphicsDevice.CreateDrawingSurface(
                new Windows.Foundation.Size(0, 0),
                Microsoft.Graphics.DirectX.DirectXPixelFormat.R8G8B8A8UIntNormalized,
                Microsoft.Graphics.DirectX.DirectXAlphaMode.Premultiplied);
            strokeSurface = graphicsDevice.CreateDrawingSurface(
                new Windows.Foundation.Size(0, 0),
                Microsoft.Graphics.DirectX.DirectXPixelFormat.R8G8B8A8UIntNormalized,
                Microsoft.Graphics.DirectX.DirectXAlphaMode.Premultiplied);
            colorFontSurface = graphicsDevice.CreateDrawingSurface(
                new Windows.Foundation.Size(0, 0),
                Microsoft.Graphics.DirectX.DirectXPixelFormat.R8G8B8A8UIntNormalized,
                Microsoft.Graphics.DirectX.DirectXAlphaMode.Premultiplied);

            textBrush = compositor.CreateSurfaceBrush(textSurface);
            textBrush.Stretch = CompositionStretch.None;
            textBrush.HorizontalAlignmentRatio = 0;
            textBrush.VerticalAlignmentRatio = 0;

            strokeBrush = compositor.CreateSurfaceBrush(strokeSurface);
            strokeBrush.Stretch = CompositionStretch.None;
            strokeBrush.HorizontalAlignmentRatio = 0;
            strokeBrush.VerticalAlignmentRatio = 0;

            colorFontBrush = compositor.CreateSurfaceBrush(colorFontSurface);
            colorFontBrush.Stretch = CompositionStretch.None;
            colorFontBrush.HorizontalAlignmentRatio = 0;
            colorFontBrush.VerticalAlignmentRatio = 0;

            textMaskBrush = compositor.CreateMaskBrush();
            textMaskBrush.Mask = textBrush;
            textMaskBrush.Source = GetTextBrush();

            strokeMaskBrush = compositor.CreateMaskBrush();
            strokeMaskBrush.Mask = strokeBrush;
            strokeMaskBrush.Source = GetStrokeBrush();

            textVisual = compositor.CreateSpriteVisual();
            textVisual.RelativeSizeAdjustment = Vector2.One;
            textVisual.Brush = textMaskBrush;
            textVisual.IsPixelSnappingEnabled = true;

            strokeVisual = compositor.CreateSpriteVisual();
            strokeVisual.RelativeSizeAdjustment = Vector2.One;
            strokeVisual.Brush = strokeMaskBrush;
            strokeVisual.IsPixelSnappingEnabled = true;

            colorFontVisual = compositor.CreateSpriteVisual();
            colorFontVisual.RelativeSizeAdjustment = Vector2.One;
            colorFontVisual.Brush = colorFontBrush;
            colorFontVisual.IsPixelSnappingEnabled = true;

            layerVisual = compositor.CreateLayerVisual();
            layerVisual.RelativeSizeAdjustment = Vector2.One;
            layerVisual.IsPixelSnappingEnabled = true;
        }

        public override Visual Visual => rootVisual!;

        protected override void OnTextStrokeColorChanged(Color value)
        {
            base.OnTextStrokeColorChanged(value);
            UpdateVisualChildren();
        }

        protected override void OnTextStrokeSecondaryColorChanged(Color value)
        {
            base.OnTextStrokeSecondaryColorChanged(value);
            UpdateVisualChildren();
        }

        protected override void OnDropShadowChanged(DropShadow? value)
        {
            base.OnDropShadowChanged(value);
            layerVisual.Shadow = value;
            UpdateVisualChildren();
        }

        private void UpdateVisualChildren()
        {
            layerVisual.Children.RemoveAll();
            rootVisual.Children.RemoveAll();

            ContainerVisual root = rootVisual;

            if (DropShadow != null)
            {
                rootVisual.Children.InsertAtTop(layerVisual);
                root = layerVisual;
            }

            root.Children.InsertAtTop(textVisual);

            var textStrokeColor = TextStrokeColor;
            var textStrokeSecondaryColor = TextStrokeSecondaryColor;

            if (textStrokeThickness > 0 && (textStrokeColor.A > 0 || textStrokeSecondaryColor.A > 0))
            {
                root.Children.InsertAtBottom(strokeVisual);
            }

            if (isColorFontEnabled && hasColorFont)
            {
                root.Children.InsertAtTop(colorFontVisual);
            }
        }

        public override void UpdateLineVisual(
            FormattedText.FormattedTextLineGlyphRuns lineGlyphRuns,
            double textStrokeThickness,
            Point startPointOffset,
            double rasterizationScale,
            bool isColorFontEnabled)
        {
            this.isColorFontEnabled = isColorFontEnabled;
            this.textStrokeThickness = textStrokeThickness;
            this.hasColorFont = lineGlyphRuns.HasColorFont;

            rootVisual.Size = new Vector2((float)(lineGlyphRuns.Bounds.Width + textStrokeThickness * 2), (float)(lineGlyphRuns.Bounds.Height + textStrokeThickness * 2));

            var linePixelWidth = GetPixelSize(lineGlyphRuns.Bounds.Width + textStrokeThickness * 2, rasterizationScale);
            var linePixelHeight = GetPixelSize(lineGlyphRuns.Bounds.Height + textStrokeThickness * 2, rasterizationScale);

            var dpi = GetPixelSize(96, rasterizationScale);
            var surfaceBrushScale = (float)(rasterizationScale > 0 ? (float)(1 / rasterizationScale) : 1);

            textSurface.Resize(new SizeInt32(linePixelWidth, linePixelHeight));

            if (textStrokeThickness > 0)
            {
                strokeSurface.Resize(new SizeInt32(linePixelWidth, linePixelHeight));
            }
            else
            {
                strokeSurface.Resize(new SizeInt32(0, 0));
            }

            if (isColorFontEnabled)
            {
                colorFontSurface.Resize(new SizeInt32(linePixelWidth, linePixelHeight));
            }
            else
            {
                colorFontSurface.Resize(new SizeInt32(0, 0));
            }

            textBrush.Scale = new Vector2(surfaceBrushScale);
            strokeBrush.Scale = new Vector2(surfaceBrushScale);
            colorFontBrush.Scale = new Vector2(surfaceBrushScale);

            CanvasGeometry? geometry = null;

            var offset = new Vector2((float)(startPointOffset.X + textStrokeThickness), (float)(startPointOffset.Y + textStrokeThickness));

            try
            {
                using var brush = new CanvasSolidColorBrush(
                    DeviceHolder.CanvasDevice,
                    Color.FromArgb(255, 255, 255, 255));

                for (int j = 0; j < lineGlyphRuns.GlyphRuns.Length; j++)
                {
                    var glyphRun = lineGlyphRuns.GlyphRuns[j];

                    using var tmp = CanvasGeometry.CreateGlyphRun(
                        DeviceHolder.CanvasDevice,
                        glyphRun.Point + offset,
                        glyphRun.FontFace,
                        glyphRun.FontSize,
                        glyphRun.Glyphs,
                        glyphRun.IsSideways,
                        glyphRun.BidiLevel,
                        CanvasTextMeasuringMode.Natural,
                        glyphRun.GlyphOrientation);

                    var b = tmp.ComputeBounds();

                    if (!glyphRun.IsColorFont)
                    {
                        if (geometry == null)
                        {
                            geometry = tmp.Simplify(CanvasGeometrySimplification.Lines, Matrix3x2.Identity, CanvasGeometry.DefaultFlatteningTolerance);
                        }
                        else
                        {
                            using var tmp2 = geometry;
                            using var tmp3 = tmp.Simplify(
                                CanvasGeometrySimplification.Lines,
                                Matrix3x2.Identity,
                                0.5f);
                            geometry = tmp2.CombineWith(tmp3, Matrix3x2.Identity, CanvasGeometryCombine.Union);
                        }
                    }
                }

                using var renderTarget = new CanvasRenderTarget(
                    DeviceHolder.CanvasDevice,
                    linePixelWidth,
                    linePixelHeight,
                    dpi);

                using (var ds = renderTarget.CreateDrawingSession())
                {
                    if (geometry != null)
                    {
                        ds.FillGeometry(geometry, Color.FromArgb(255, 255, 255, 255));
                    }
                }

                using (var ds = CanvasComposition.CreateDrawingSession(
                    textSurface,
                    new Rect(0, 0, linePixelWidth, linePixelHeight),
                    dpi))
                {
                    ds.Clear(Colors.Transparent);
                    ds.DrawImage(renderTarget);
                }

                if (textStrokeThickness > 0)
                {
                    using (var ds = CanvasComposition.CreateDrawingSession(
                        strokeSurface,
                        new Rect(0, 0, linePixelWidth, linePixelHeight),
                        dpi))
                    {
                        ds.Clear(Colors.Transparent);

                        if (geometry != null)
                        {
                            if (defaultStrokeStyle == null)
                            {
                                defaultStrokeStyle = new CanvasStrokeStyle();
                            }

                            //{
                            //    Geometry运算，特别卡，算球
                            //    using var geometry2 = geometry.Outline(Matrix3x2.Identity, 0.5f);
                            //    using var geometry3 = geometry2.Stroke(
                            //        (float)(textStrokeThickness * 2),
                            //        defaultStrokeStyle,
                            //        Matrix3x2.Identity,
                            //        1f);
                            //    using var geometry4 = geometry3.CombineWith(geometry2, Matrix3x2.Identity, CanvasGeometryCombine.Exclude, 1);

                            //    ds.FillGeometry(geometry4, Color.FromArgb(255, 255, 255, 255));
                            //}

                            using var geometry2 = geometry.Stroke(
                                (float)(textStrokeThickness * 2),
                                defaultStrokeStyle,
                                Matrix3x2.Identity,
                                1f);

                            using var commandList = new CanvasCommandList(renderTarget);

                            using (var tmpDs = commandList.CreateDrawingSession())
                            {
                                tmpDs.FillGeometry(geometry2, Color.FromArgb(255, 255, 255, 255));
                                tmpDs.FillGeometry(geometry, Color.FromArgb(255, 0, 0, 0));
                            }

                            using var colorMatrixEffect = new ColorMatrixEffect()
                            {
                                Source = commandList,
                                ClampOutput = true,
                                ColorMatrix = new Matrix5x4()
                                {
                                #pragma warning disable IDE0055
                                    M11 = 1, M12 = 0, M13 = 0, M14 = 1,
                                    M21 = 0, M22 = 1, M23 = 0, M24 = 0,
                                    M31 = 0, M32 = 0, M33 = 1, M34 = 0,
                                    M41 = 0, M42 = 0, M43 = 0, M44 = 0,
                                    M51 = 0, M52 = 0, M53 = 0, M54 = 0
                                #pragma warning restore IDE0055
                                }
                            };

                            using var maskEffect = new AlphaMaskEffect()
                            {
                                Source = commandList,
                                AlphaMask = colorMatrixEffect,
                            };

                            ds.DrawImage(maskEffect);
                        }
                    }
                }
            }
            finally
            {
                geometry?.Dispose();
            }

            if (isColorFontEnabled)
            {
                using (var ds = CanvasComposition.CreateDrawingSession(
                    colorFontSurface,
                    new Rect(0, 0, linePixelWidth, linePixelHeight),
                    dpi))
                {
                    ds.Clear(Colors.Transparent);

                    if (lineGlyphRuns.HasColorFont)
                    {
                        for (int j = 0; j < lineGlyphRuns.GlyphRuns.Length; j++)
                        {
                            var glyphRun = lineGlyphRuns.GlyphRuns[j];

                            if (glyphRun.IsColorFont)
                            {
                                var x = offset.X + glyphRun.Point.X;
                                var y = offset.Y + (float)glyphRun.LayoutBounds.Top;

                                if (glyphRun.FontFace != null)
                                {
                                    y = offset.Y + glyphRun.Point.Y - (glyphRun.FontFace.Ascent - glyphRun.FontFace.LineGap) * glyphRun.FontSize;
                                }

                                var (fontFamilyName, fontStretch, fontStyle, fontWeight) = GetFontFaceProperties(glyphRun.FontFace);

                                using (var format = new CanvasTextFormat()
                                {
                                    Options = CanvasDrawTextOptions.EnableColorFont,
                                    FontSize = glyphRun.FontSize,
                                    FontFamily = fontFamilyName,
                                    FontStretch = fontStretch,
                                    FontStyle = fontStyle,
                                    FontWeight = fontWeight
                                })
                                {
                                    ds.DrawText(
                                        glyphRun.TextString,
                                        new Vector2(x, offset.Y),
                                        Color.FromArgb(255, 255, 255, 255),
                                        format);
                                }
                            }
                        }
                    }
                }
            }

            UpdateVisualChildren();

            static (string? fontFamilyName, FontStretch fontStretch, FontStyle fontStyle, FontWeight fontWeight) GetFontFaceProperties(CanvasFontFace? canvasFontFace)
            {
                string? fontFamilyName = null;

                if (canvasFontFace != null)
                {
                    var familyNames = canvasFontFace.FamilyNames;

                    if (familyNames.Count > 0)
                    {
                        if (familyNames.TryGetValue("en-us", out var name))
                        {
                            fontFamilyName = name;
                        }
                        else
                        {
                            fontFamilyName = familyNames.Values.FirstOrDefault();
                        }
                    }
                    return (fontFamilyName, canvasFontFace.Stretch, canvasFontFace.Style, canvasFontFace.Weight);
                }

                return (null, FontStretch.Normal, FontStyle.Normal, FontWeights.Normal);
            }
        }

        protected override void DisposeCore(bool disposing)
        {
            rootVisual?.Dispose();
            rootVisual = null!;

            textVisual?.Dispose();
            textVisual = null!;

            strokeVisual?.Dispose();
            strokeVisual = null!;

            colorFontVisual?.Dispose();
            colorFontVisual = null!;

            textMaskBrush?.Dispose();
            textMaskBrush = null!;

            strokeMaskBrush?.Dispose();
            strokeMaskBrush = null!;

            textBrush?.Dispose();
            textBrush = null!;

            strokeBrush?.Dispose();
            strokeBrush = null!;

            textSurface?.Dispose();
            textSurface = null!;

            strokeSurface?.Dispose();
            strokeSurface = null!;

            colorFontSurface?.Dispose();
            colorFontSurface = null!;
        }
    }
}
