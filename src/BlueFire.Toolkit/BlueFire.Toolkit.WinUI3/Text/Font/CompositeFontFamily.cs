using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }

    public class CompositeFontFamilyMap
    {
        private List<CanvasFontFamily>? families;

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
                        var collection = new CanvasFontFamilyCollection(Target ?? "");

                        for (int i = 0; i < collection.Count; i++)
                        {
                            var family = collection[i];

                            var props = SystemFontHelper.GetFontProperties(family.FontFamilyName, LanguageTag ?? CultureInfo.CurrentUICulture.Name);
                            UnicodeRange[]? ranges = null;

                            if (props != null)
                            {
                                ranges = CanvasTextFormatHelper.GetMergedUnicodeRange(props.UnicodeRanges, UnicodeRanges);
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
    }
}
