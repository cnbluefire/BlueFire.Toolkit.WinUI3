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
            var compositeFont = new CompositeFontFamily()
            {
                FontFamilyName = "Custom Font",
                FamilyMaps = new List<CompositeFontFamilyMap>()
                {
                    new CompositeFontFamilyMap()
                    {
                        Target = "Segoe Print, Simsun",
                        LanguageTag = "zh",
                        UnicodeRanges = new[]
                        {
                            new UnicodeRange()
                            {
                                first = '测',
                                last = '测'
                            },
                            new UnicodeRange()
                            {
                                first = 'A',
                                last = 'A'
                            }
                        }
                    }
                }
            };

            CompositeFontManager.Register(compositeFont);

            using (var format = new CanvasTextFormat())
            {
                format.FontFamily = null;

                CanvasTextFormatHelper.SetFontFamilySource(format, "Custom Font, Wide Latin, 方正舒体");

                using (var layout = new CanvasTextLayout(CanvasDevice.GetSharedDevice(), "测试AB", format, float.MaxValue, float.MaxValue))
                {
                    var renderer = new TextRenderer();

                    layout.DrawToTextRenderer(renderer, default);

                    Assert.AreEqual(renderer.Fonts[0].FamilyNames["en-us"], "SimSun");
                    Assert.AreEqual(renderer.Fonts[1].FamilyNames["en-us"], "FZShuTi");
                    Assert.AreEqual(renderer.Fonts[2].FamilyNames["en-us"], "Segoe Print");
                    Assert.AreEqual(renderer.Fonts[3].FamilyNames["en-us"], "Wide Latin");
                }
            }
        }

        [TestMethod]
        public void CreateFormattedText()
        {
            var formattedText = new FormattedText(
                "测试AB",
                "en",
                Microsoft.UI.Xaml.FlowDirection.LeftToRight,
                new FormattedTextTypeface(),
                12,
                true,
                true);

            Assert.AreEqual(formattedText.LineGlyphRuns[0].GlyphRuns[0].TextString, "测");
            Assert.AreEqual(formattedText.LineGlyphRuns[0].GlyphRuns[2].TextString, "A");
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
