using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Text;

namespace BlueFire.Toolkit.WinUI3.Text
{
    partial class FormattedText
    {
        private IReadOnlyList<FormattedTextLineGlyphRuns> CreateLineGlyphRuns()
        {
            var textLayout = this.EnsureTextLayout();

            var renderer = new FormattedTextRendererImpl(textLayout);

            var lineMetrics = textLayout.LineMetrics;

            var lineIndex = 0;
            var processedLineCharTotalCount = 0;
            var processedLineBottom = 0d;

            var lines = new List<FormattedTextLineGlyphRuns>(lineMetrics.Length);
            var lineGlyphRuns = new List<FormattedTextGlyphRun>();

            var currentLineBounds = Rect.Empty;

            for (int i = 0; i <= renderer.GlyphRuns.Count; i++)
            {
                FormattedTextGlyphRun? glyphRun = null;

                if (i < renderer.GlyphRuns.Count)
                {
                    glyphRun = renderer.GlyphRuns[i];
                }

                var curLineMetrics = lineMetrics[lineIndex];

                if (glyphRun != null
                    && glyphRun.CharacterIndex < processedLineCharTotalCount + curLineMetrics.CharacterCount)
                {
                    currentLineBounds.Union(glyphRun.LayoutBounds);
                    lineGlyphRuns.Add(glyphRun);
                }
                else
                {
                    lines.Add(new FormattedTextLineGlyphRuns(
                        currentLineBounds,
                        curLineMetrics,
                        lineGlyphRuns.ToArray()));

                    lineGlyphRuns.Clear();
                    processedLineCharTotalCount += curLineMetrics.CharacterCount;
                    processedLineBottom += curLineMetrics.Height;
                    currentLineBounds = Rect.Empty;

                    lineIndex++;

                    if (glyphRun != null)
                    {
                        currentLineBounds.Union(glyphRun.LayoutBounds);
                        lineGlyphRuns.Add(glyphRun);
                    }
                }
            }

            return lines;
        }

        public record class FormattedTextLineGlyphRuns(
            Rect Bounds,
            CanvasLineMetrics CanvasLineMetrics,
            FormattedTextGlyphRun[] GlyphRuns)
        {
            public bool HasColorFont => GlyphRuns.Any(c => c.IsColorFont);
        }

        public record class FormattedTextGlyphRun(
            Vector2 Point,
            CanvasFontFace? FontFace,
            float FontSize,
            CanvasGlyph[]? Glyphs,
            bool IsSideways,
            uint BidiLevel,
            string? LocaleName,
            string? TextString,
            uint CharacterIndex,
            uint CharacterCount,
            Rect LayoutBounds,
            CanvasGlyphOrientation GlyphOrientation)
        {
            private bool? isColorFont;

            public bool IsColorFont => isColorFont ?? (isColorFont = FontFace?.IsColorFont() ?? false).Value;

            public override string? ToString()
            {
                return TextString;
            }
        }


        private class FormattedTextRendererImpl : ICanvasTextRenderer
        {
            private CanvasTextLayout? textLayout;
            private List<FormattedTextGlyphRun> glyphRuns;

            public FormattedTextRendererImpl(CanvasTextLayout textLayout)
            {
                this.textLayout = textLayout;
                glyphRuns = new List<FormattedTextGlyphRun>();

                try
                {
                    textLayout.DrawToTextRenderer(this, 0, 0);
                }
                finally
                {
                    this.textLayout = null;
                }
            }

            public IReadOnlyList<FormattedTextGlyphRun> GlyphRuns => glyphRuns;

            public void DrawGlyphRun(Vector2 point, CanvasFontFace fontFace, float fontSize, CanvasGlyph[] glyphs, bool isSideways, uint bidiLevel, object brush, CanvasTextMeasuringMode measuringMode, string localeName, string textString, int[] clusterMapIndices, uint characterIndex, CanvasGlyphOrientation glyphOrientation)
            {
                const string DefaultTrimmingDelimiter = "…";

                if (glyphs == null || glyphs.Length == 0 || clusterMapIndices == null || clusterMapIndices.Length == 0) return;

                var glyphIndex = clusterMapIndices[0];
                var charIndex = 0;
                var x = point.X;
                var y = point.Y;

                for (int i = 0; i < clusterMapIndices.Length; i++)
                {
                    var glyphCount = (i < clusterMapIndices.Length - 1) ? clusterMapIndices[i + 1] - glyphIndex : glyphs.Length - glyphIndex;

                    if (glyphCount > 0)
                    {
                        bool isTrimmingDelimiter = glyphCount == 1 && textString == DefaultTrimmingDelimiter && characterIndex == 0 && glyphRuns.Count > 0;

                        var curGlyphs = glyphs.Skip(glyphIndex).Take(glyphCount).ToArray();
                        var advance = curGlyphs.Sum(c => c.Advance);

                        Rect layoutBounds;
                        int characterCount;

                        if (isTrimmingDelimiter)
                        {
                            using var geometry = CanvasGeometry.CreateGlyphRun(
                                null,
                                new Vector2(x, y),
                                fontFace,
                                fontSize,
                                glyphs,
                                isSideways,
                                bidiLevel,
                                CanvasTextMeasuringMode.Natural,
                                glyphOrientation);

                            layoutBounds = geometry.ComputeBounds();
                            characterCount = DefaultTrimmingDelimiter.Length;
                        }
                        else
                        {
                            var caretPosition = textLayout!.GetCaretPosition((int)(charIndex + characterIndex), false, out var region);
                            layoutBounds = region.LayoutBounds;
                            characterCount = region.CharacterCount;
                        }

                        var glyphRun = new FormattedTextGlyphRun(
                            Point: new Vector2(x, y),
                            FontFace: fontFace,
                            FontSize: fontSize,
                            Glyphs: curGlyphs,
                            IsSideways: isSideways,
                            BidiLevel: bidiLevel,
                            LocaleName: localeName,
                            TextString: textString.Substring(charIndex, i - charIndex + 1),
                            CharacterIndex: (uint)charIndex + characterIndex,
                            CharacterCount: (uint)characterCount,
                            LayoutBounds: layoutBounds,
                            GlyphOrientation: glyphOrientation);

                        glyphRuns.Add(glyphRun);

                        if (i < clusterMapIndices.Length - 1)
                        {
                            glyphIndex = clusterMapIndices[i + 1];
                            charIndex = i + 1;
                        }

                        if (isSideways
                            || glyphOrientation == CanvasGlyphOrientation.Clockwise90Degrees
                            || glyphOrientation == CanvasGlyphOrientation.Clockwise270Degrees)
                        {
                            if (bidiLevel % 2 == 0)
                            {
                                y += advance;
                            }
                            else
                            {
                                y -= advance;
                            }
                        }
                        else
                        {
                            if (bidiLevel % 2 == 0)
                            {
                                x += advance;
                            }
                            else
                            {
                                x -= advance;
                            }
                        }
                    }
                }
            }

            public void DrawStrikethrough(Vector2 point, float strikethroughWidth, float strikethroughThickness, float strikethroughOffset, CanvasTextDirection textDirection, object brush, CanvasTextMeasuringMode textMeasuringMode, string localeName, CanvasGlyphOrientation glyphOrientation)
            {
            }

            public void DrawUnderline(Vector2 point, float underlineWidth, float underlineThickness, float underlineOffset, float runHeight, CanvasTextDirection textDirection, object brush, CanvasTextMeasuringMode textMeasuringMode, string localeName, CanvasGlyphOrientation glyphOrientation)
            {
            }

            public void DrawInlineObject(Vector2 point, ICanvasTextInlineObject inlineObject, bool isSideways, bool isRightToLeft, object brush, CanvasGlyphOrientation glyphOrientation)
            {
            }

            public float Dpi => 96;

            public bool PixelSnappingDisabled => true;

            public Matrix3x2 Transform => Matrix3x2.Identity;
        }
    }
}
