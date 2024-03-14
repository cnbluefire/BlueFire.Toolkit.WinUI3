using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueFire.Toolkit.WinUI3.Text
{
    public static class CompositeFontManager
    {
        private static Dictionary<string, CompositeFontFamily> compositeFonts = new Dictionary<string, CompositeFontFamily>();

        public static bool Register(CompositeFontFamily compositeFont)
        {
            var key = compositeFont.FontFamilyName?.Trim()?.ToUpperInvariant();
            if (string.IsNullOrEmpty(key)) return false;

            lock (compositeFonts)
            {
                if (compositeFonts.ContainsKey(key)) return false;

                compositeFonts[key] = compositeFont.Clone();

                try
                {
                    CompositeFontsChanged?.Invoke(null, EventArgs.Empty);
                }
                catch { }

                return true;
            }
        }

        public static bool Unregister(string fontFamilyName)
        {
            var key = fontFamilyName?.Trim()?.ToUpperInvariant();
            if (string.IsNullOrEmpty(key)) return false;

            lock (compositeFonts)
            {
                if (compositeFonts.Remove(key, out var value))
                {
                    value?.Dispose();

                    try
                    {
                        CompositeFontsChanged?.Invoke(null, EventArgs.Empty);
                    }
                    catch { }

                    return true;
                }
                return false;
            }
        }

        internal static CompositeFontFamily? Find(string fontFamilyName)
        {
            var key = fontFamilyName?.Trim()?.ToUpperInvariant();
            if (string.IsNullOrEmpty(key)) return null;

            lock (compositeFonts)
            {
                if (compositeFonts.TryGetValue(key, out var value)) return value;
                return null;
            }
        }

        internal static event EventHandler? CompositeFontsChanged;
    }
}
