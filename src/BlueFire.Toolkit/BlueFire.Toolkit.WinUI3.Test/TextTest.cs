using BlueFire.Toolkit.WinUI3.Text;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BlueFire.Toolkit.WinUI3.Test
{
    [TestClass]
    public class TextTest
    {
        [TestMethod]
        public void GetFontProperties()
        {
            var props = SystemFontHelper.GetFontProperties("Segoe UI");
            Assert.IsNotNull(props);
        }


        [TestMethod]
        public async Task GetNotExistFontProperties()
        {
            var props = SystemFontHelper.GetFontProperties("Segoe UI");
            Assert.IsNotNull(props);

            var props1 = SystemFontHelper.GetFontProperties("Fake Segoe UI 1");

            var props2 = await Task.Run(() => SystemFontHelper.GetFontProperties("Fake Segoe UI 2"));

            Assert.IsNotNull(props);
            Assert.IsNull(props1);
            Assert.IsNull(props2);
        }

        [TestMethod]
        public void SetFallbackFontFamilies()
        {
            using (var format = new CanvasTextFormat())
            {
                format.FontFamily = null;

                var list = new List<CanvasFontFamily>()
                {
                    new CanvasFontFamily("Simsun")
                    {
                        UnicodeRanges = new []
                        {
                            new UnicodeRange()
                            {
                                first = '测',
                                last = '测'
                            }
                        },
                    },
                    new CanvasFontFamily("Segoe UI")
                };

                lock (this)
                {
                    CanvasTextFormatHelper.SetFallbackFontFamilies(format, list, CultureInfo.CurrentUICulture.Name, (fontFileUri) => new CanvasFontSet(fontFileUri));
                }

                using (var layout = new CanvasTextLayout(CanvasDevice.GetSharedDevice(), "测A", format, float.MaxValue, float.MaxValue))
                {
                    var renderer = new TextRenderer();

                    layout.DrawToTextRenderer(renderer, default);

                    Assert.AreEqual(renderer.Fonts[0].FamilyNames["zh-cn"], "宋体");
                    Assert.AreEqual(renderer.Fonts[1].FamilyNames["en-us"], "Segoe UI");
                }
            }
        }

        [TestMethod]
        public void MergeUnicodeRanges()
        {
            var defaultRange = new[]
            {
                new UnicodeRange()
                {
                    first = 'A',
                    last = 'Z'
                }
            };

            var defaultRange2 = new[]
            {
                new UnicodeRange()
                {
                    first = '测',
                    last = '测'
                }
            };

            var props = SystemFontHelper.GetFontProperties("Segoe UI");

            var range1 = CanvasTextFormatHelper.GetMergedUnicodeRange(props.UnicodeRanges, defaultRange);

            Assert.AreEqual(range1[0].first, 'A');
            Assert.AreEqual(range1[0].last, 'Z');

            var range2 = CanvasTextFormatHelper.GetMergedUnicodeRange(props.UnicodeRanges, defaultRange2);

            Assert.AreEqual(range2.Length, 0);
        }

        private class TextRenderer : ICanvasTextRenderer
        {
            public float Dpi => 96;

            public bool PixelSnappingDisabled => false;

            public Matrix3x2 Transform => Matrix3x2.Identity;

            public List<CanvasFontFace> Fonts { get; } = new List<CanvasFontFace>();

            public void DrawGlyphRun(Vector2 point, CanvasFontFace fontFace, float fontSize, CanvasGlyph[] glyphs, bool isSideways, uint bidiLevel, object brush, CanvasTextMeasuringMode measuringMode, string localeName, string textString, int[] clusterMapIndices, uint characterIndex, CanvasGlyphOrientation glyphOrientation)
            {
                Fonts.Add(fontFace);
            }

            public void DrawInlineObject(Vector2 point, ICanvasTextInlineObject inlineObject, bool isSideways, bool isRightToLeft, object brush, CanvasGlyphOrientation glyphOrientation)
            {

            }

            public void DrawStrikethrough(Vector2 point, float strikethroughWidth, float strikethroughThickness, float strikethroughOffset, CanvasTextDirection textDirection, object brush, CanvasTextMeasuringMode textMeasuringMode, string localeName, CanvasGlyphOrientation glyphOrientation)
            {

            }

            public void DrawUnderline(Vector2 point, float underlineWidth, float underlineThickness, float underlineOffset, float runHeight, CanvasTextDirection textDirection, object brush, CanvasTextMeasuringMode textMeasuringMode, string localeName, CanvasGlyphOrientation glyphOrientation)
            {

            }
        }
    }
}
