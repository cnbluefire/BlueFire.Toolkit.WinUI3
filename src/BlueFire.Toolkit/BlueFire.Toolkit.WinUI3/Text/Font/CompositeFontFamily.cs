using Microsoft.Graphics.Canvas.Text;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinRT;

namespace BlueFire.Toolkit.WinUI3.Text
{
    public class CompositeFontFamily
    {
        public string? FontFamilyName { get; set; }

        public IReadOnlyList<CompositeFontFamilyMap>? FamilyMaps { get; set; }

        internal CompositeFontFamily Clone()
        {
            return new CompositeFontFamily()
            {
                FontFamilyName = FontFamilyName,
                FamilyMaps = FamilyMaps?.ToList()
            };
        }

        internal void Dispose()
        {
            if (FamilyMaps != null)
            {
                foreach (var map in FamilyMaps)
                {
                    map.Dispose();
                }
            }
        }
    }

    public class CompositeFontFamilyMap
    {
        private List<CanvasFontFamily>? families;
        private Dictionary<int, CanvasFontFace>? canvasFontFaces;

        public UnicodeRange[]? UnicodeRanges { get; set; }

        public string? Target { get; set; }

        public double Scale { get; set; } = 1;

        public string? LanguageTag { get; set; }

        internal CompositeFontFamilyMap Clone()
        {
            return new CompositeFontFamilyMap()
            {
                UnicodeRanges = UnicodeRanges?.ToArray(),
                Target = Target,
                Scale = Scale,
                LanguageTag = LanguageTag,
            };
        }

        internal IReadOnlyList<CanvasFontFamily> GetFamilies()
        {
            if (families == null)
            {
                lock (this)
                {
                    if (families == null)
                    {
                        families = new List<CanvasFontFamily>();
                        canvasFontFaces = new Dictionary<int, CanvasFontFace>();

                        var collection = new CanvasFontFamilyCollection(Target ?? "");

                        for (int i = 0; i < collection.Count; i++)
                        {
                            var family = collection[i];

                            var props = SystemFontHelper.GetFontProperties(family.FontFamilyName, LanguageTag ?? CultureInfo.CurrentUICulture.Name);

                            UnicodeRange[]? ranges = null;

                            if (family.LocationUri == null)
                            {
                                if (props != null)
                                {
                                    ranges = CanvasTextFormatHelper.GetMergedUnicodeRange(props.UnicodeRanges, UnicodeRanges);
                                }
                            }
                            else
                            {
                                try
                                {
                                    using (var fontSet = new CanvasFontSet(family.LocationUri))
                                    {
                                        //var fontFace = fontSet.GetMatchingFonts(
                                        //    family.FontFamilyName,
                                        //    Microsoft.UI.Text.FontWeights.Normal,
                                        //    Windows.UI.Text.FontStretch.Normal,
                                        //    Windows.UI.Text.FontStyle.Normal);

                                        using (var subSet = fontSet.GetMatchingFonts(
                                            new[] { new CanvasFontProperty(
                                                CanvasFontPropertyIdentifier.FamilyName,
                                                family.FontFamilyName,
                                                this.LanguageTag) }))
                                        {
                                            if (subSet.Fonts.Count > 0)
                                            {
                                                var fontFace = subSet.Fonts[0];

                                                var unicodeRange = fontFace.UnicodeRanges;
                                                if (unicodeRange != null)
                                                {
                                                    ranges = CanvasTextFormatHelper.GetMergedUnicodeRange(
                                                        System.Runtime.CompilerServices.Unsafe.As<UnicodeRange[]>(unicodeRange), 
                                                        UnicodeRanges);
                                                }

                                                canvasFontFaces[families.Count] = fontFace;
                                            }

                                        }
                                    }
                                }
                                catch { }
                            }

                            if (ranges != null)
                            {
                                family.UnicodeRanges = ranges;
                                families.Add(family);
                            }
                        }
                    }
                }
            }

            return families;
        }

        internal void Dispose()
        {
            var tmp = Interlocked.Exchange(ref canvasFontFaces, null);
            if (tmp != null)
            {
                foreach (var item in tmp.Values)
                {
                    item.Dispose();
                }
            }
        }
    }
}
