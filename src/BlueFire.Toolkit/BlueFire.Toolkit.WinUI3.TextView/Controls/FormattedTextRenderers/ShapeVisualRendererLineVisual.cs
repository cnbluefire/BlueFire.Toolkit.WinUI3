using BlueFire.Toolkit.WinUI3.Text;
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
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics;
using Windows.UI;
using Windows.UI.Text;

namespace BlueFire.Toolkit.WinUI3.Controls.FormattedTextRenderers
{
    internal class ShapeVisualRendererLineVisual : FormattedTextRendererLineVisual
    {
        private bool hasColorFont;
        private bool hasGeometryContent;

        private bool isColorFontEnabled;
        private double textStrokeThickness;

        private CompositionPathGeometry textGeometry = null!;
        private CompositionPathGeometry strokeClipGeometry = null!;

        private CompositionSpriteShape textShape = null!;
        private CompositionSpriteShape strokeShape = null!;

        private CompositionGeometricClip strokeClip = null!;

        private CompositionDrawingSurface colorFontSurface = null!;
        private CompositionSurfaceBrush colorFontBrush = null!;

        private ShapeVisual textVisual = null!;
        private ShapeVisual strokeVisual = null!;
        private SpriteVisual colorFontVisual = null!;
        private LayerVisual layerVisual = null!;
        private ContainerVisual rootVisual = null!;

        protected override void Initialize()
        {
            base.Initialize();

            var compositor = DeviceHolder.Compositor;
            var graphicsDevice = DeviceHolder.CompositionGraphicsDevice;

            textGeometry = compositor.CreatePathGeometry();
            strokeClipGeometry = compositor.CreatePathGeometry();

            textShape = compositor.CreateSpriteShape(textGeometry);
            textShape.FillBrush = GetTextBrush();

            strokeShape = compositor.CreateSpriteShape(textGeometry);
            strokeShape.StrokeBrush = GetStrokeBrush();

            strokeClip = compositor.CreateGeometricClip(strokeClipGeometry);

            textVisual = compositor.CreateShapeVisual();
            textVisual.RelativeSizeAdjustment = Vector2.One;
            textVisual.Shapes.Add(textShape);
            textVisual.IsPixelSnappingEnabled = true;

            strokeVisual = compositor.CreateShapeVisual();
            strokeVisual.RelativeSizeAdjustment = Vector2.One;
            strokeVisual.Shapes.Add(strokeShape);
            strokeVisual.Clip = strokeClip;
            strokeVisual.IsPixelSnappingEnabled = true;

            colorFontSurface = graphicsDevice.CreateDrawingSurface(
                new Windows.Foundation.Size(0, 0),
                Microsoft.Graphics.DirectX.DirectXPixelFormat.R8G8B8A8UIntNormalized,
                Microsoft.Graphics.DirectX.DirectXAlphaMode.Premultiplied);
            colorFontBrush = compositor.CreateSurfaceBrush(colorFontSurface);
            colorFontBrush.Stretch = CompositionStretch.None;
            colorFontBrush.HorizontalAlignmentRatio = 0;
            colorFontBrush.VerticalAlignmentRatio = 0;

            colorFontVisual = compositor.CreateSpriteVisual();
            colorFontVisual.RelativeSizeAdjustment = Vector2.One;
            colorFontVisual.Brush = colorFontBrush;
            colorFontVisual.IsPixelSnappingEnabled = true;

            layerVisual = compositor.CreateLayerVisual();
            layerVisual.RelativeSizeAdjustment = Vector2.One;
            layerVisual.IsPixelSnappingEnabled = true;

            rootVisual = compositor.CreateContainerVisual();
            rootVisual.IsPixelSnappingEnabled = true;
        }

        public override Visual Visual => rootVisual;

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

            if (hasGeometryContent)
            {
                root.Children.InsertAtTop(textVisual);

                var textStrokeColor = TextStrokeColor;
                var textStrokeSecondaryColor = TextStrokeSecondaryColor;

                if (textStrokeThickness > 0 && (textStrokeColor.A > 0 || textStrokeSecondaryColor.A > 0))
                {
                    root.Children.InsertAtBottom(strokeVisual);
                }
            }

            if (isColorFontEnabled && hasColorFont)
            {
                root.Children.InsertAtTop(colorFontVisual);
            }
        }

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

        public override void UpdateLineVisual(FormattedText.FormattedTextLineGlyphRuns lineGlyphRuns, double textStrokeThickness, Point startPointOffset, double rasterizationScale, bool isColorFontEnabled)
        {
            this.isColorFontEnabled = isColorFontEnabled;
            this.textStrokeThickness = textStrokeThickness;
            this.hasColorFont = lineGlyphRuns.HasColorFont;

            var size = new Vector2((float)(lineGlyphRuns.Bounds.Width + textStrokeThickness * 2), (float)(lineGlyphRuns.Bounds.Height + textStrokeThickness * 2));
            var offset = new Vector2((float)(startPointOffset.X + textStrokeThickness), (float)(startPointOffset.Y + textStrokeThickness));

            rootVisual.Size = size;

            var linePixelWidth = GetPixelSize(lineGlyphRuns.Bounds.Width + textStrokeThickness * 2, rasterizationScale);
            var linePixelHeight = GetPixelSize(lineGlyphRuns.Bounds.Height + textStrokeThickness * 2, rasterizationScale);

            var dpi = GetPixelSize(96, rasterizationScale);
            var surfaceBrushScale = (float)(rasterizationScale > 0 ? (float)(1 / rasterizationScale) : 1);

            if (isColorFontEnabled)
            {
                colorFontSurface.Resize(new SizeInt32(linePixelWidth, linePixelHeight));
            }
            else
            {
                colorFontSurface.Resize(new SizeInt32(0, 0));
            }

            strokeShape.StrokeThickness = (float)(textStrokeThickness * 2);
            colorFontBrush.Scale = new Vector2(surfaceBrushScale);

            CanvasGeometry? geometry = null;

            try
            {
                for (int j = 0; j < lineGlyphRuns.GlyphRuns.Length; j++)
                {
                    var glyphRun = lineGlyphRuns.GlyphRuns[j];

                    using var tmp = CanvasGeometry.CreateGlyphRun(
                        null,
                        glyphRun.Point + offset,
                        glyphRun.FontFace,
                        glyphRun.FontSize,
                        glyphRun.Glyphs,
                        glyphRun.IsSideways,
                        glyphRun.BidiLevel,
                        CanvasTextMeasuringMode.Natural,
                        glyphRun.GlyphOrientation);

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

                hasGeometryContent = false;

                if (geometry != null)
                {
                    hasGeometryContent = true;

                    textGeometry.Path = new CompositionPath(geometry);

                    if (textStrokeThickness > 0)
                    {
                        using var rectGeometry = CanvasGeometry.CreateRectangle(null, 0, 0, size.X, size.Y);
                        using var clipGeometry = rectGeometry.CombineWith(geometry, Matrix3x2.Identity, CanvasGeometryCombine.Xor);

                        strokeClipGeometry.Path = new CompositionPath(clipGeometry);
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
            base.DisposeCore(disposing);

            rootVisual?.Dispose();
            rootVisual = null!;

            layerVisual?.Dispose();
            layerVisual = null!;

            textVisual?.Dispose();
            textVisual = null!;

            strokeVisual?.Dispose();
            strokeVisual = null!;

            colorFontVisual?.Dispose();
            colorFontVisual = null!;

            colorFontBrush?.Dispose();
            colorFontBrush = null!;

            colorFontSurface?.Dispose();
            colorFontSurface = null!;

            strokeClip?.Dispose();
            strokeClip = null!;

            strokeClipGeometry?.Dispose();
            strokeClipGeometry = null!;

            textShape?.Dispose();
            textShape = null!;

            strokeShape?.Dispose();
            strokeShape = null!;

            textGeometry?.Dispose();
            textGeometry = null!;
        }
    }

}
