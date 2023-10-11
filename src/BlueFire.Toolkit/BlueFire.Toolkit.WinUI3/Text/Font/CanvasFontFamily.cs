using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32.Graphics.DirectWrite;
using PInvoke = Windows.Win32.PInvoke;
using System.Diagnostics;
using System.Globalization;
using BlueFire.Toolkit.WinUI3.Graphics;
using System.Runtime.InteropServices;
using WinRT;

namespace BlueFire.Toolkit.WinUI3.Text
{
    public class CanvasFontFamily
    {
        public CanvasFontFamily(string fontFamilyName, Uri? locationUri)
        {
            FontFamilyName = fontFamilyName;
            LocationUri = locationUri;
        }

        public CanvasFontFamily(string fontFamilyName)
        {
            FontFamilyName = fontFamilyName;
        }

        public string FontFamilyName { get; }

        public Uri? LocationUri { get; }

        public bool IsGenericFamilyName => LocationUri == null && CanvasTextFormatHelper.IsGenericFamilyName(FontFamilyName);

        public UnicodeRange[]? UnicodeRanges { get; set; }

        public float ScaleFactor { get; set; } = 1f;

        public override string ToString()
        {
            if (LocationUri != null)
            {
                return $"{LocationUri.OriginalString}#{FontFamilyName}";
            }
            return FontFamilyName;
        }
    }

    public record class CanvasFallbackFont(string FontFamilyName, IWinRTObject? CanvasFontSet, float ScaleFactor = 1);

}
