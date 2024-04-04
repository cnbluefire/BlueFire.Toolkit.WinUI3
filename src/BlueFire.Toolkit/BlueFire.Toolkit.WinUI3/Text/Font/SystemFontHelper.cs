using BlueFire.Toolkit.WinUI3.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32.Graphics.DirectWrite;
using WinRT;
using PInvoke = Windows.Win32.PInvoke;

namespace BlueFire.Toolkit.WinUI3.Text
{
    public static class SystemFontHelper
    {
        private static ComPtr<IDWriteFontSet>? systemFontSet;
        private static Dictionary<string, CanvasFontProperties?> fontPropertiesCache = new Dictionary<string, CanvasFontProperties?>(StringComparer.OrdinalIgnoreCase);

        public static CanvasFontProperties? GetFontProperties(string fontFamily)
        {
            return GetFontProperties(fontFamily, CultureInfo.CurrentUICulture.Name);
        }

        public static CanvasFontProperties? GetFontProperties(string fontFamily, string languageTag)
        {
            var actualName = CanvasTextFormatHelper.GetActualFamilyName(new CanvasFontFamily(fontFamily, null), languageTag, out _);
            if (string.IsNullOrEmpty(actualName)) return null;

            var cacheKey = actualName.ToUpperInvariant();

            lock (fontPropertiesCache)
            {
                if (!fontPropertiesCache.TryGetValue(cacheKey, out var value))
                {
                    value = GetFontPropertiesFromFontSet(actualName);
                    fontPropertiesCache[cacheKey] = value;
                }
                return value?.Clone(actualName);
            }
        }

        private static unsafe CanvasFontProperties? GetFontPropertiesFromFontSet(string fontFamily)
        {
            ComPtr<IDWriteFontFaceReference> fontFaceReference = default;
            ComPtr<IDWriteFontSetBuilder> fontSetBuilder = default;
            ComPtr<IDWriteFontSet> systemFontSet = default;
            ComPtr<IDWriteFontSet> filteredSet = default;
            ComPtr<IDWriteFontSet> fontSet = default;
            ComPtr<IDWriteFontFace3> fontFace = default;

            try
            {
                fixed (char* ptr = fontFamily)
                {
                    var property = new DWRITE_FONT_PROPERTY()
                    {
                        propertyId = DWRITE_FONT_PROPERTY_ID.DWRITE_FONT_PROPERTY_ID_FAMILY_NAME,
                        propertyValue = ptr
                    };

                    GetSharedSystemFontSet().Value.GetMatchingFonts(&property, 1, filteredSet.TypedPointerRef);

                    if (filteredSet.HasValue && filteredSet.Value.GetFontCount() > 0)
                    {
                        filteredSet.Value.GetFontFaceReference(0, fontFaceReference.TypedPointerRef);
                        if (fontFaceReference.HasValue)
                        {
                            fontFaceReference.Value.CreateFontFace(fontFace.TypedPointerRef);

                            var properties = DWriteHelper.GetFontProperties(fontFace);
                            if (properties != null && string.IsNullOrEmpty(properties.FontFamilyName))
                            {
                                properties.FontFamilyName = fontFamily;
                            }
                            return properties;
                        }
                    }
                }

                return null;
            }
            finally
            {
                fontFaceReference.Release();
                systemFontSet.Release();
                fontSetBuilder.Release();
                fontSet.Release();
                filteredSet.Release();
                fontFace.Release();
            }
        }

        internal static ComPtr<IDWriteFontSet> GetSharedSystemFontSet()
        {
            if (!systemFontSet.HasValue)
            {
                systemFontSet = CreateSystemFontSet(DWriteHelper.GetSharedFactory<IDWriteFactory3>());
            }
            return systemFontSet.Value;
        }

        internal static unsafe ComPtr<IDWriteFontSet> CreateSystemFontSet(ComPtr<IDWriteFactory3> factory)
        {
            ComPtr<IDWriteFontSet> tempFontSet = default;
            ComPtr<IDWriteFontSetBuilder> fontSetBuilder = default;

            try
            {
                factory.Value.GetSystemFontSet(tempFontSet.TypedPointerRef);

                factory.Value.CreateFontSetBuilder(fontSetBuilder.TypedPointerRef);

                var fontCount = tempFontSet.Value.GetFontCount();
                for (uint i = 0; i < fontCount; i++)
                {
                    ComPtr<IDWriteFontFaceReference> tmpFontRef = default;
                    tempFontSet.Value.GetFontFaceReference(i, tmpFontRef.TypedPointerRef);

                    try
                    {
                        if (tmpFontRef.Value.GetLocality() == DWRITE_LOCALITY.DWRITE_LOCALITY_LOCAL)
                        {
                            fontSetBuilder.Value.AddFontFaceReference(tmpFontRef.AsTypedPointer());
                        }
                    }
                    finally
                    {
                        tmpFontRef.Release();
                    }
                }

                ComPtr<IDWriteFontSet> fontSet = default;

                fontSetBuilder.Value.CreateFontSet(fontSet.TypedPointerRef);

                return fontSet;
            }
            finally
            {
                tempFontSet.Release();
                fontSetBuilder.Release();
            }
        }
    }
}
