using BlueFire.Toolkit.WinUI3.Graphics;
using BlueFire.Toolkit.WinUI3.Text;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.UI.Text;
using WinRT;
using WinRT.Interop;

namespace BlueFire.Toolkit.WinUI3.Controls
{
    partial class TextViewDrawingContext
    {
        internal class ColorFontCache : IDisposable
        {
            private bool disposedValue;
            private Dictionary<ColorFontGlyphRun, SoftwareBitmap> colorFontCache;

            internal ColorFontCache()
            {
                colorFontCache = new Dictionary<ColorFontGlyphRun, SoftwareBitmap>();
            }

            internal SoftwareBitmap? TryGetOrCreate(FormattedText.FormattedTextGlyphRun glyphRun, float scale)
            {
                if (!glyphRun.IsColorFont) return null;

                var width = (int)Math.Ceiling(glyphRun.LayoutBounds.Width * scale);
                var height = (int)Math.Ceiling(glyphRun.LayoutBounds.Height * scale);

                var cacheKey = CreateColorFontGlyphRuns(glyphRun);
                lock (colorFontCache)
                {
                    if (colorFontCache.TryGetValue(cacheKey, out var bitmap))
                    {
                        if (bitmap.PixelWidth >= width && bitmap.PixelHeight >= height) return bitmap;
                    }

                    bitmap = CreateBitmap(glyphRun, cacheKey.FontFace, width, height, scale);
                    if (bitmap != null)
                    {
                        colorFontCache[cacheKey] = bitmap;
                    }
                    return bitmap;
                }
            }

            internal void Clear()
            {
                lock (colorFontCache)
                {
                    var tmp = colorFontCache;
                    colorFontCache = new Dictionary<ColorFontGlyphRun, SoftwareBitmap>();

                    foreach (var item in tmp.Values)
                    {
                        item.Dispose();
                    }
                }
            }

            private static SoftwareBitmap? CreateBitmap(FormattedText.FormattedTextGlyphRun glyphRun, FontFace? fontFace, int width, int height, float scale)
            {
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        var device = CanvasDevice.GetSharedDevice(i != 2);

                        using (var renderTarget = new CanvasRenderTarget(device, width, height, 96))
                        {
                            using (var drawingSession = renderTarget.CreateDrawingSession())
                            {
                                using (var format = new CanvasTextFormat()
                                {
                                    Options = CanvasDrawTextOptions.EnableColorFont,
                                    FontSize = glyphRun.FontSize,
                                    FontFamily = fontFace?.FontFamilyName ?? "",
                                    FontStretch = fontFace?.FontStretch ?? FontStretch.Normal,
                                    FontStyle = fontFace?.FontStyle ?? FontStyle.Normal,
                                    FontWeight = fontFace?.FontWeight ?? Microsoft.UI.Text.FontWeights.Normal,
                                })
                                {
                                    var x = glyphRun.Point.X;
                                    var y = (float)glyphRun.LayoutBounds.Top;

                                    if (fontFace.HasValue)
                                    {
                                        y = glyphRun.Point.Y - (fontFace.Value.Ascent - fontFace.Value.LineGap) * glyphRun.FontSize;
                                    }

                                    var oldTransform = drawingSession.Transform;
                                    drawingSession.Transform = Matrix3x2.CreateScale(scale, scale);

                                    try
                                    {
                                        drawingSession.DrawText(
                                            glyphRun.TextString,
                                            new Vector2(x - (float)glyphRun.LayoutBounds.Left, y - (float)glyphRun.LayoutBounds.Top),
                                            Windows.UI.Color.FromArgb(255, 0, 0, 0),
                                            format);
                                    }
                                    finally
                                    {
                                        drawingSession.Transform = oldTransform;
                                    }
                                }
                            }

                            var bufferSize = width * 4 * height;
                            var buffer = new Windows.Storage.Streams.Buffer((uint)bufferSize);

                            renderTarget.GetPixelBytes(buffer);

                            return SoftwareBitmap.CreateCopyFromBuffer(buffer, BitmapPixelFormat.Bgra8, width, height, BitmapAlphaMode.Premultiplied);
                        }
                    }
                    catch { }
                }

                return null;
            }

            public void Dispose()
            {
                if (!disposedValue)
                {
                    Clear();

                    disposedValue = true;
                }
            }

            private static ColorFontGlyphRun CreateColorFontGlyphRuns(FormattedText.FormattedTextGlyphRun glyphRun)
            {
                string? fontFamilyName = null;

                FontFace? fontFace = null;
                if (glyphRun.FontFace != null)
                {
                    var familyNames = glyphRun.FontFace.FamilyNames;

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
                    fontFace = new FontFace(
                        fontFamilyName,
                        glyphRun.FontFace.Stretch,
                        glyphRun.FontFace.Style,
                        glyphRun.FontFace.Weight,
                        glyphRun.FontFace.Ascent,
                        glyphRun.FontFace.Descent,
                        glyphRun.FontFace.LineGap);
                }

                return new ColorFontGlyphRun(
                    fontFace,
                    new GlyphArray(glyphRun.Glyphs),
                    glyphRun.IsSideways,
                    glyphRun.BidiLevel,
                    glyphRun.LocaleName,
                    glyphRun.TextString,
                    glyphRun.GlyphOrientation);
            }

            private record struct ColorFontGlyphRun(
                FontFace? FontFace,
                GlyphArray Glyphs,
                bool IsSideways,
                uint BidiLevel,
                string? LocaleName,
                string? TextString,
                CanvasGlyphOrientation GlyphOrientation);

            private record struct FontFace(
                string? FontFamilyName,
                FontStretch FontStretch,
                FontStyle FontStyle,
                FontWeight FontWeight,
                float Ascent,
                float Descent,
                float LineGap);

            private struct GlyphArray : IEquatable<GlyphArray>
            {
                public GlyphArray(CanvasGlyph[]? glyphs)
                {
                    Glyphs = glyphs;
                }

                public CanvasGlyph[]? Glyphs { get; }

                public bool Equals(GlyphArray other)
                {
                    if (Glyphs == null && other.Glyphs == null) return true;
                    else if (Glyphs == null || other.Glyphs == null) return false;

                    return Enumerable.SequenceEqual(Glyphs, other.Glyphs);
                }

                public override bool Equals([NotNullWhen(true)] object? obj)
                {
                    return obj is GlyphArray obj1
                        && Equals(obj1);
                }

                public override int GetHashCode()
                {
                    return HashCode.Combine(Glyphs);
                }
            }
        }
    }
}
