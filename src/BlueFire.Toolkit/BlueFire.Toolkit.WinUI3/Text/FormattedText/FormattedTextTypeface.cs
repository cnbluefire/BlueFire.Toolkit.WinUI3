using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Text;

namespace BlueFire.Toolkit.WinUI3.Text
{
    public record struct FormattedTextTypeface(string? FontFamily, FontWeight FontWeight, FontStyle FontStyle, FontStretch FontStretch)
    {
        public FormattedTextTypeface(string? fontFamily) : this(fontFamily, new FontWeight(400), FontStyle.Normal, FontStretch.Normal) { }

        public FormattedTextTypeface() : this(null, new FontWeight(400), FontStyle.Normal, FontStretch.Normal) { }

        public static FormattedTextTypeface XamlAutoTypeFace =>
            new FormattedTextTypeface();

        public readonly string ActualFontFamilyName
        {
            get
            {
                try
                {
                    return !string.IsNullOrEmpty(FontFamily) ?
                        FontFamily : Microsoft.UI.Xaml.Media.FontFamily.XamlAutoFontFamily.Source;
                }
                catch { }
                return "SYSTEM-UI";
            }

        }
    }
}
