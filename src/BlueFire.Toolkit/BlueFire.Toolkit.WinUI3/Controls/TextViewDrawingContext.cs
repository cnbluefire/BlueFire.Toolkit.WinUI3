using BlueFire.Toolkit.WinUI3.Graphics;
using BlueFire.Toolkit.WinUI3.Text;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI.Composition;
using Microsoft.UI.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics;
using Windows.UI.Text;

namespace BlueFire.Toolkit.WinUI3.Controls
{
    internal partial class TextViewDrawingContext : IDisposable
    {
        private static CanvasStrokeStyle? defaultStrokeStyle;

        private readonly TextView textView;
        private readonly CompositionGraphicsDeviceHolder graphicsDeviceHolder;
        private ColorFontCache colorFontCache;

        private bool disposedValue;

        private D2DTexture? fillTexture;
        private D2DTexture? strokeTexture;
        private D2DTexture? colorFontTexture;

        private bool dirty;

        public TextViewDrawingContext(TextView textView, CompositionGraphicsDeviceHolder graphicsDeviceHolder)
        {
            this.textView = textView;
            this.graphicsDeviceHolder = graphicsDeviceHolder;
            this.colorFontCache = new ColorFontCache();

            graphicsDeviceHolder.CompositionGraphicsDevice.RenderingDeviceReplaced += CompositionGraphicsDevice_RenderingDeviceReplaced;
        }

        internal void MakeDirty()
        {
            dirty = true;
        }

        internal void RenderText(CanvasDrawingSession drawingSession)
        {
            if (!dirty)
            {
                fillTexture?.Present(drawingSession);
            }
        }

        internal void RenderStroke(CanvasDrawingSession drawingSession)
        {
            if (!dirty)
            {
                strokeTexture?.Present(drawingSession);
            }
        }

        internal void RenderColorFont(CanvasDrawingSession drawingSession)
        {
            if (!dirty)
            {
                colorFontTexture?.Present(drawingSession);
            }
        }

        internal void PrepareTexture(SizeInt32 size, Vector2 offset, FormattedText formattedText, in TextView.FormattedTextProperties properties)
        {
            if (dirty || fillTexture == null)
            {
                if (DrawTextToTexture(size, offset, formattedText, in properties))
                {
                    dirty = false;
                }
            }
        }

        private bool DrawTextToTexture(SizeInt32 size, Vector2 offset, FormattedText formattedText, in TextView.FormattedTextProperties properties)
        {
            var device = graphicsDeviceHolder.CanvasDevice;
            if (!graphicsDeviceHolder.DeviceRecreating
                && device != null
                && formattedText != null
                && size.Width > 0 && size.Height > 0)
            {
                var lineGlyphRuns = formattedText.LineGlyphRuns;

                if (properties.ImmutableProperties.StrokeThickness > 0)
                {
                    EnsureStrokeTexture();
                }

                if (properties.ImmutableProperties.IsColorFontEnabled)
                {
                    EnsureColorFontTexture();
                }

                var strokeThickness = (float)properties.ImmutableProperties.StrokeThickness;
                var scale = (float)properties.ImmutableProperties.RasterizationScale;

                EnsureFillTexture();


                var sw = Stopwatch.StartNew();

                using (var provider = fillTexture.CreateDrawingSession(size))
                {
                    if (provider.DrawingSession != null)
                    {
                        provider.DrawingSession.Clear(Windows.UI.Color.FromArgb(0, 0, 0, 0));

                        provider.DrawingSession.Transform = Matrix3x2.Identity;

                        FillGlyphRuns(
                            provider.DrawingSession,
                            formattedText.LineGlyphRuns,
                            offset,
                            Windows.UI.Color.FromArgb(255, 0, 0, 0),
                            scale);
                    }
                }
                if (strokeThickness > 0)
                {
                    using (var provider = strokeTexture!.CreateDrawingSession(size))
                    {
                        var bitmap = fillTexture.CanvasBitmap;
                        if (provider.DrawingSession != null && bitmap != null)
                        {
                            provider.DrawingSession.Clear(Windows.UI.Color.FromArgb(0, 0, 0, 0));

                            provider.DrawingSession.Transform = Matrix3x2.Identity;

                            StrokeGlyphRuns(
                                provider.DrawingSession,
                                formattedText,
                                offset,
                                new Windows.Foundation.Size(size.Width, size.Height),
                                bitmap,
                                Windows.UI.Color.FromArgb(255, 255, 255, 255),
                                strokeThickness,
                                scale,
                                properties.ImmutableProperties.IsColorFontEnabled);
                        }
                    }
                }

                if (properties.ImmutableProperties.IsColorFontEnabled)
                {
                    using (var provider = colorFontTexture!.CreateDrawingSession(size))
                    {
                        if (provider.DrawingSession != null)
                        {
                            provider.DrawingSession.Clear(Windows.UI.Color.FromArgb(0, 0, 0, 0));

                            DrawColorGlyphRuns(
                                provider.DrawingSession,
                                formattedText.LineGlyphRuns,
                                offset,
                                scale);
                        }
                    }
                }

                sw.Stop();
                //Debug.WriteLine($"Duration: {sw.ElapsedMilliseconds}");

                return true;
            }

            return false;
        }

        private void FillGlyphRuns(
            CanvasDrawingSession drawingSession,
            IReadOnlyList<Text.FormattedText.FormattedTextLineGlyphRuns> lines,
            Vector2 offset,
            Windows.UI.Color fillColor,
            float scale)
        {
            var oldTransform = drawingSession.Transform;
            drawingSession.Transform = Matrix3x2.CreateScale(scale, scale);

            ICanvasBrush? brush = null;
            try
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    for (int j = 0; j < lines[i].GlyphRuns.Length; j++)
                    {
                        var glyphRun = lines[i].GlyphRuns[j];
                        if (!glyphRun.IsColorFont)
                        {
                            if (brush == null)
                            {
                                brush = new CanvasSolidColorBrush(drawingSession, fillColor);
                            }

                            drawingSession.DrawGlyphRun(
                                glyphRun.Point + offset,
                                glyphRun.FontFace,
                                glyphRun.FontSize,
                                glyphRun.Glyphs,
                                glyphRun.IsSideways,
                                glyphRun.BidiLevel,
                                brush);
                        }
                    }
                }
            }
            finally
            {
                drawingSession.Transform = oldTransform;
                brush?.Dispose();
            }
        }

        private void DrawColorGlyphRuns(
            CanvasDrawingSession drawingSession,
            IReadOnlyList<Text.FormattedText.FormattedTextLineGlyphRuns> lines,
            Vector2 offset,
            float scale)
        {
            var oldTransform = drawingSession.Transform;
            drawingSession.Transform = Matrix3x2.CreateScale(scale, scale);

            try
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    for (int j = 0; j < lines[i].GlyphRuns.Length; j++)
                    {
                        var glyphRun = lines[i].GlyphRuns[j];

                        if (glyphRun.IsColorFont)
                        {
                            var bitmap = colorFontCache.TryGetOrCreate(glyphRun, scale);

                            if (bitmap != null)
                            {
                                using (var canvasBitmap = CanvasBitmap.CreateFromSoftwareBitmap(drawingSession, bitmap))
                                {
                                    var destinationRect = glyphRun.LayoutBounds;
                                    destinationRect.X += offset.X;
                                    destinationRect.Y += offset.Y;

                                    drawingSession.DrawImage(canvasBitmap, destinationRect);
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                drawingSession.Transform = oldTransform;
            }
        }

        private void DrawColorGlyphRunsFromTextLayout(
            CanvasDrawingSession drawingSession,
            FormattedText formattedText,
            Vector2 offset,
            float scale)
        {
            var oldTransform = drawingSession.Transform;
            drawingSession.Transform = Matrix3x2.CreateScale(scale, scale);

            try
            {
                using (var textLayout = formattedText.CreateCanvasTextLayout(drawingSession))
                using (var brush = new CanvasSolidColorBrush(drawingSession, Windows.UI.Color.FromArgb(0, 255, 255, 255)))
                {
                    textLayout.Options |= CanvasDrawTextOptions.EnableColorFont;
                    drawingSession.DrawTextLayout(textLayout, offset, brush);
                }
            }
            finally
            {
                drawingSession.Transform = oldTransform;
            }
        }

        private void DrawColorGlyphRunsWithoutCache(
            CanvasDrawingSession drawingSession,
            IReadOnlyList<Text.FormattedText.FormattedTextLineGlyphRuns> lines,
            Vector2 offset,
            float scale)
        {
            var oldTransform = drawingSession.Transform;
            drawingSession.Transform = Matrix3x2.CreateScale(scale, scale);

            try
            {
                using (var format = new CanvasTextFormat()
                {
                    Options = CanvasDrawTextOptions.EnableColorFont
                })
                {
                    for (int i = 0; i < lines.Count; i++)
                    {
                        for (int j = 0; j < lines[i].GlyphRuns.Length; j++)
                        {
                            var glyphRun = lines[i].GlyphRuns[j];
                            if (glyphRun.IsColorFont)
                            {
                                var x = offset.X + glyphRun.Point.X;
                                var y = offset.Y + (float)glyphRun.LayoutBounds.Top;

                                if (glyphRun.FontFace != null)
                                {
                                    y = offset.Y + glyphRun.Point.Y - (glyphRun.FontFace.Ascent - glyphRun.FontFace.LineGap) * glyphRun.FontSize;
                                }

                                var (fontFamilyName, fontStretch, fontStyle, fontWeight) = GetFontFaceProperties(glyphRun.FontFace);

                                format.FontSize = glyphRun.FontSize;
                                format.FontFamily = fontFamilyName;
                                format.FontStretch = fontStretch;
                                format.FontStyle = fontStyle;
                                format.FontWeight = fontWeight;

                                drawingSession.DrawText(
                                    glyphRun.TextString,
                                    new Vector2(x, y),
                                    Windows.UI.Color.FromArgb(255, 0, 0, 0),
                                    format);
                            }
                        }
                    }
                }
            }
            finally
            {
                drawingSession.Transform = oldTransform;
            }

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

        private void StrokeGlyphRuns(
            CanvasDrawingSession drawingSession,
            FormattedText formattedText,
            Vector2 offset,
            Windows.Foundation.Size size,
            CanvasBitmap fillTextBitmap,
            Windows.UI.Color strokeColor,
            float strokeThickness,
            float scale,
            bool isColorFontEnabled)
        {
            if (strokeThickness > 0)
            {
                using (var geometry = BuildGeometry(
                    drawingSession,
                    formattedText,
                    offset,
                    new Windows.Foundation.Size(size.Width, size.Height),
                    strokeThickness,
                    scale,
                    isColorFontEnabled))
                {
                    if (geometry != null)
                    {
                        if (defaultStrokeStyle == null)
                        {
                            defaultStrokeStyle = new CanvasStrokeStyle();
                        }

                        CanvasActiveLayer? layer = null;
                        try
                        {
                            if (isColorFontEnabled)
                            {
                                CanvasGeometry? maskGeometry = null;
                                var glyphRuns = formattedText.LineGlyphRuns;
                                for (int i = 0; i < glyphRuns.Count; i++)
                                {
                                    var lineGlyphRuns = glyphRuns[i];
                                    for (int j = 0; j < lineGlyphRuns.GlyphRuns.Length; j++)
                                    {
                                        var glyphRun = lineGlyphRuns.GlyphRuns[j];
                                        if (glyphRun.IsColorFont)
                                        {
                                            var bounds = glyphRun.LayoutBounds;
                                            bounds.X += offset.X;
                                            bounds.Y += offset.Y;

                                            bounds.X *= scale;
                                            bounds.Y *= scale;
                                            bounds.Width *= scale;
                                            bounds.Height *= scale;

                                            bounds.X -= 0.5;
                                            bounds.Y -= 0.5;
                                            bounds.Width += 1;
                                            bounds.Height += 1;

                                            using var boundsGeometry = CanvasGeometry.CreateRectangle(drawingSession, bounds);

                                            if (maskGeometry == null)
                                            {
                                                using var tmp = CanvasGeometry.CreateRectangle(drawingSession, new Windows.Foundation.Rect(default, size));
                                                maskGeometry = tmp.CombineWith(boundsGeometry, Matrix3x2.Identity, CanvasGeometryCombine.Exclude);
                                            }
                                            else
                                            {
                                                using var tmp = maskGeometry;
                                                maskGeometry = tmp.CombineWith(boundsGeometry, Matrix3x2.Identity, CanvasGeometryCombine.Exclude);
                                            }
                                        }
                                    }
                                }

                                if (maskGeometry != null)
                                {
                                    layer = drawingSession.CreateLayer(1, maskGeometry);
                                }
                            }

                            using var commandList = new CanvasCommandList(drawingSession);

                            using (var tmpDs = commandList.CreateDrawingSession())
                            {
                                tmpDs.DrawGeometry(geometry, strokeColor, (float)(strokeThickness * 2 * scale), defaultStrokeStyle);
                                tmpDs.DrawImage(fillTextBitmap);
                            }

                            using var colorMatrixEffect = new ColorMatrixEffect()
                            {
                                Source = commandList,
                                ClampOutput = true,
                                ColorMatrix = new Matrix5x4()
                                {
                                #pragma warning disable format

                                    M11 = 1, M12 = 0, M13 = 0, M14 = 1,
                                    M21 = 0, M22 = 1, M23 = 0, M24 = 0,
                                    M31 = 0, M32 = 0, M33 = 1, M34 = 0,
                                    M41 = 0, M42 = 0, M43 = 0, M44 = 0,
                                    M51 = 0, M52 = 0, M53 = 0, M54 = 0

                                #pragma warning restore format
                                }
                            };

                            using var maskEffect = new AlphaMaskEffect()
                            {
                                Source = commandList,
                                AlphaMask = colorMatrixEffect,
                            };

                            drawingSession.DrawImage(maskEffect);
                        }
                        finally
                        {
                            layer?.Dispose();
                        }
                    }
                }
            }
        }

        private CanvasGeometry? BuildGeometry(
            ICanvasResourceCreator resourceCreator,
            FormattedText formattedText,
            Vector2 offset,
            Windows.Foundation.Size size,
            float strokeThickness,
            float scale,
            bool isColorFontEnabled)
        {
            const bool AlwaysDrawColorFontStroke = true;

            CanvasGeometry? geometry = null;

            if (!AlwaysDrawColorFontStroke && isColorFontEnabled)
            {
                var transform = Matrix3x2.CreateScale(scale, scale);

                var glyphRuns = formattedText.LineGlyphRuns;

                for (int i = 0; i < glyphRuns.Count; i++)
                {
                    var lineGlyphRuns = glyphRuns[i];
                    for (int j = 0; j < lineGlyphRuns.GlyphRuns.Length; j++)
                    {
                        var glyphRun = lineGlyphRuns.GlyphRuns[j];

                        using var tmp = CanvasGeometry.CreateGlyphRun(
                            resourceCreator,
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
                                geometry = tmp.Simplify(CanvasGeometrySimplification.Lines, transform, CanvasGeometry.DefaultFlatteningTolerance);
                            }
                            else
                            {
                                using var tmp2 = geometry;
                                using var tmp3 = tmp.Simplify(
                                    CanvasGeometrySimplification.Lines,
                                    Matrix3x2.Identity,
                                    0.5f);
                                geometry = tmp2.CombineWith(tmp3, transform, CanvasGeometryCombine.Union);
                            }
                        }
                    }
                }
            }
            else
            {
                using (var textLayout = formattedText.CreateCanvasTextLayout(resourceCreator))
                {
                    var transform = Matrix3x2.CreateTranslation(offset)
                        * Matrix3x2.CreateScale(scale, scale);

                    using var tmp1 = CanvasGeometry.CreateText(textLayout);
                    geometry = tmp1.Transform(transform);
                }
            }

            if (geometry != null && strokeThickness > 0)
            {
                using var tmp = geometry;
                using var rectGeometry = CanvasGeometry.CreateRectangle(resourceCreator, 0, 0, (float)size.Width, (float)size.Height);
                geometry = rectGeometry.CombineWith(geometry, Matrix3x2.Identity, CanvasGeometryCombine.Intersect);
            }

            return geometry;
        }


        [MemberNotNull(nameof(fillTexture))]
        private void EnsureFillTexture()
        {
            if (fillTexture == null)
            {
                fillTexture = new D2DTexture(graphicsDeviceHolder);
            }
        }

        [MemberNotNull(nameof(strokeTexture))]
        private void EnsureStrokeTexture()
        {
            if (strokeTexture == null)
            {
                strokeTexture = new D2DTexture(graphicsDeviceHolder);
            }
        }

        [MemberNotNull(nameof(colorFontTexture))]
        private void EnsureColorFontTexture()
        {
            if (colorFontTexture == null)
            {
                colorFontTexture = new D2DTexture(graphicsDeviceHolder);
            }
        }

        private void CompositionGraphicsDevice_RenderingDeviceReplaced(CompositionGraphicsDevice sender, RenderingDeviceReplacedEventArgs args)
        {
            dirty = true;

            fillTexture?.Dispose();
            fillTexture = null!;

            strokeTexture?.Dispose();
            strokeTexture = null;

            colorFontTexture?.Dispose();
            colorFontTexture = null;
        }

        public void Dispose()
        {
            if (!disposedValue)
            {
                graphicsDeviceHolder.CompositionGraphicsDevice.RenderingDeviceReplaced -= CompositionGraphicsDevice_RenderingDeviceReplaced;

                fillTexture?.Dispose();
                fillTexture = null!;

                strokeTexture?.Dispose();
                strokeTexture = null;

                colorFontTexture?.Dispose();
                colorFontTexture = null;

                disposedValue = true;
            }
        }


        private class D2DTexture : IDisposable
        {
            private bool disposedValue;
            private readonly CompositionGraphicsDeviceHolder deviceHolder;
            private CanvasRenderTarget? renderTarget;
            private object locker = new object();

            internal D2DTexture(CompositionGraphicsDeviceHolder deviceHolder)
            {
                this.deviceHolder = deviceHolder;
                deviceHolder.CompositionGraphicsDevice.RenderingDeviceReplaced += CompositionGraphicsDevice_RenderingDeviceReplaced;
            }

            private void CompositionGraphicsDevice_RenderingDeviceReplaced(CompositionGraphicsDevice sender, RenderingDeviceReplacedEventArgs args)
            {
                lock (locker)
                {
                    renderTarget?.Dispose();
                    renderTarget = null;
                }
            }

            internal CanvasBitmap? CanvasBitmap => renderTarget;

            internal DrawingSessionProvider CreateDrawingSession(SizeInt32 size)
            {
                if (size.Width == 0 || size.Height == 0) return new DrawingSessionProvider(null, null);

                var canvasDevice = deviceHolder.CanvasDevice;
                if (deviceHolder.DeviceRecreating || canvasDevice == null) return new DrawingSessionProvider(null, null);

                CanvasDrawingSession? drawingSession = null;
                bool lockWasTaken = false;

                try
                {
                    Monitor.Enter(locker, ref lockWasTaken);

                    if (renderTarget != null)
                    {
                        var renderTargetSize = renderTarget.SizeInPixels;
                        if (renderTargetSize.Width != size.Width
                            || renderTargetSize.Height != size.Height)
                        {
                            renderTarget.Dispose();
                            renderTarget = null;
                        }
                    }

                    if (renderTarget == null)
                    {
                        renderTarget = new CanvasRenderTarget(
                            canvasDevice,
                            size.Width,
                            size.Height,
                            96,
                            Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized,
                            CanvasAlphaMode.Premultiplied);
                    }

                    drawingSession = renderTarget.CreateDrawingSession();
                }
                catch (Exception ex) when (canvasDevice.IsDeviceLost(ex.HResult)) { }
#if !DEBUG
                catch (Exception ex) { }
#endif

                return new DrawingSessionProvider(drawingSession, lockWasTaken ? locker : null);
            }

            internal bool Present(CanvasDrawingSession drawingSession)
            {
                lock (locker)
                {
                    if (renderTarget == null) return false;
                    var canvasDevice = deviceHolder.CanvasDevice;
                    if (deviceHolder.DeviceRecreating || canvasDevice == null) return false;

                    try
                    {
                        drawingSession.DrawImage(renderTarget);

                        return true;
                    }
                    catch (Exception ex) when (canvasDevice.IsDeviceLost(ex.HResult)) { }
#if !DEBUG
                    catch (Exception ex) { }
#endif

                    return false;
                }
            }


            public void Dispose()
            {
                lock (locker)
                {
                    if (!disposedValue)
                    {
                        deviceHolder.CompositionGraphicsDevice.RenderingDeviceReplaced -= CompositionGraphicsDevice_RenderingDeviceReplaced;
                        renderTarget?.Dispose();
                        renderTarget = null;

                        disposedValue = true;
                    }
                }
            }
        }

        private struct DrawingSessionProvider : IDisposable
        {
            private object? locker;
            private readonly Matrix3x2 originalTransform;

            internal DrawingSessionProvider(CanvasDrawingSession? drawingSession, object? locker)
            {
                this.locker = locker;
                DrawingSession = drawingSession;

                if (drawingSession != null)
                {
                    this.originalTransform = drawingSession.Transform;
                    drawingSession.Transform = Matrix3x2.Identity;
                }
                else
                {
                    this.originalTransform = Matrix3x2.Identity;
                }
            }

            internal CanvasDrawingSession? DrawingSession { get; private set; }

            public void Dispose()
            {
                if (DrawingSession != null)
                {
                    DrawingSession.Transform = originalTransform;
                    DrawingSession.Dispose();
                    DrawingSession = null;
                }

                if (locker != null) Monitor.Exit(locker);
            }
        }
    }
}
