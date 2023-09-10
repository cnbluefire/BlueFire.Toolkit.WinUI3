using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using WinRT;
using Windows.Win32.Graphics.DirectWrite;
using PInvoke = Windows.Win32.PInvoke;
using System.Diagnostics;
using System.Globalization;
using BlueFire.Toolkit.WinUI3.Graphics;
using BlueFire.Toolkit.WinUI3.Extensions;

namespace BlueFire.Toolkit.WinUI3.Text
{
    public static class CanvasTextFormatHelper
    {
        private static HashSet<string> genericFamilyNames = new HashSet<string>()
        {
            "SERIF",
            "SANS-SERIF",
            "MONOSPACE",
            "UI-SERIF",
            "UI-SANS-SERIF",
            "UI-MONOSPACE",
            "SYSTEM-UI",
            "EMOJI"
        };

        public static void SetFontFamilySource<T>(
            T canvasTextFormat,
            string fontFamilySource,
            string? languageTag,
            Action<T, string> setFontFamilyAction,
            Func<Uri, IWinRTObject> createCanvasFontSetFunction) where T : IWinRTObject
        {
            var collection = new CanvasFontFamilyCollection(fontFamilySource);

            if (collection.Count > 0)
            {
                if (string.IsNullOrEmpty(languageTag))
                {
                    languageTag = CultureInfo.CurrentUICulture.Name;

                    if (string.IsNullOrEmpty(languageTag))
                    {
                        languageTag = "en";
                    }
                }

                var mainFontFamily = GetActualFamilyName(collection[0], languageTag, out _);

                setFontFamilyAction.Invoke(canvasTextFormat, mainFontFamily);

                if (collection.Count > 1)
                {
                    SetFallbackFontFamilies(canvasTextFormat, collection, true, createCanvasFontSetFunction);
                }
            }
        }

        public static unsafe void SetFallbackFontFamilies(object canvasTextFormat, IReadOnlyList<CanvasFontFamily> fontFamilies, string? languageTag, Func<Uri, IWinRTObject> createCanvasFontSetFunction)
        {
            if (fontFamilies.Count == 0) return;

            var list = new List<CanvasFontFamily>();

            if (string.IsNullOrEmpty(languageTag))
            {
                languageTag = CultureInfo.CurrentUICulture.Name;

                if (string.IsNullOrEmpty(languageTag))
                {
                    languageTag = "en";
                }
            }

            for (int i = 0; i < fontFamilies.Count; i++)
            {
                if (fontFamilies[i].LocationUri != null)
                {
                    list.Add(fontFamilies[i]);
                }
                else
                {
                    var actualName = GetActualFamilyName(fontFamilies[i], languageTag, out var scaleFactor);
                    list.Add(new CanvasFontFamily(actualName)
                    {
                        IsMainFont = fontFamilies[i].IsMainFont,
                        UnicodeRanges = fontFamilies[i].UnicodeRanges,
                        ScaleFactor = fontFamilies[i].ScaleFactor,
                    });
                }
            }

            SetFallbackFontFamilies(canvasTextFormat, list, false, createCanvasFontSetFunction);
        }

        private static unsafe void SetFallbackFontFamilies(object canvasTextFormat, IReadOnlyList<CanvasFontFamily> fontFamilies, bool ignoreMainFont, Func<Uri, IWinRTObject> createCanvasFontSetFunction)
        {
            if (fontFamilies == null || fontFamilies.Count == 0) return;

            CanvasFontFamily? primaryFontFamily = null;

            for (int i = 0; i < fontFamilies.Count; i++)
            {
                if (fontFamilies[i].IsMainFont)
                {
                    if (primaryFontFamily != null)
                    {
                        throw new ArgumentException(nameof(CanvasFontFamily.IsMainFont));
                    }

                    primaryFontFamily = fontFamilies[i];
                }
            }

            using var dWriteTextFormat1 = Direct2DInterop.GetWrappedResourcePtr<IDWriteTextFormat1>(canvasTextFormat);
            if (dWriteTextFormat1.HasValue)
            {
                using var factory = DWriteHelper.GetSharedFactory<IDWriteFactory2>();

                CanvasFontProperties? primaryFontProperties = null;
                IWinRTObject? primaryCanvasFontSet = null;
                var primaryFontLineHeight = 0f;
                float primaryFontScaleFactor = 1f;

                ComPtr<IDWriteFontFace3> primaryFontFace = default;
                ComPtr<IDWriteFontCollection> primaryFontCollection = default;

                try
                {
                    if (primaryFontFamily != null)
                    {
                        primaryFontProperties = DWriteHelper.GetFontProperties(primaryFontFamily, createCanvasFontSetFunction, out primaryFontFace, out primaryFontCollection, out primaryCanvasFontSet);
                        if (primaryFontProperties != null)
                        {
                            primaryFontLineHeight = primaryFontProperties.CapHeight;
                            primaryFontScaleFactor = primaryFontFamily.ScaleFactor ?? 1;
                        }
                    }

                    using (ComPtr<IDWriteFontFallbackBuilder> builder = default)
                    {
                        factory.Value.CreateFontFallbackBuilder(builder.TypedPointerRef);

                        for (int i = 0; i < fontFamilies.Count; i++)
                        {
                            if (ignoreMainFont && fontFamilies[i].IsMainFont)
                            {
                                continue;
                            }

                            CanvasFontProperties? fontProperties = null;
                            IWinRTObject? canvasFontSet = null;
                            ComPtr<IDWriteFontCollection> fontCollection = default;
                            ComPtr<IDWriteFontFace3> fontFace = default;

                            try
                            {
                                if (fontFamilies[i].IsMainFont)
                                {
                                    if (ignoreMainFont)
                                    {
                                        continue;
                                    }

                                    fontProperties = primaryFontProperties;
                                    fontFace = primaryFontFace;
                                    fontCollection = primaryFontCollection;
                                    canvasFontSet = primaryCanvasFontSet;
                                }
                                else
                                {
                                    fontProperties = DWriteHelper.GetFontProperties(fontFamilies[i], createCanvasFontSetFunction, out fontFace, out fontCollection, out canvasFontSet);
                                }

                                if (fontProperties != null && fontProperties.UnicodeRanges.Length > 0)
                                {
                                    DWRITE_UNICODE_RANGE[]? unicodeRanges = default;

                                    if (fontFamilies[i].UnicodeRanges != null)
                                    {
                                        unicodeRanges = MemoryMarshal.Cast<UnicodeRange, DWRITE_UNICODE_RANGE>(fontFamilies[i].UnicodeRanges)
                                            .ToArray();
                                    }
                                    else
                                    {
                                        unicodeRanges = fontProperties.unicodeRanges;
                                    }

                                    if (unicodeRanges != null && unicodeRanges.Length > 0)
                                    {
                                        var scaleFactor = 1f;

                                        if (fontFamilies[i].ScaleFactor.HasValue)
                                        {
                                            scaleFactor = fontFamilies[i].ScaleFactor!.Value;
                                        }
                                        else if (!fontFamilies[i].IsMainFont && primaryFontLineHeight > 0)
                                        {
                                            var lineHeight = fontProperties.CapHeight;

                                            if (lineHeight != 0)
                                            {
                                                scaleFactor = primaryFontLineHeight / lineHeight * primaryFontScaleFactor;
                                            }
                                        }

                                        var actualName = fontFamilies[i].FontFamilyName;

                                        fixed (DWRITE_UNICODE_RANGE* ptr = unicodeRanges)
                                        fixed (char* pActualName = actualName)
                                        {
                                            builder.Value.AddMapping(
                                                ptr,
                                                (uint)unicodeRanges.Length,
                                                (ushort**)(&pActualName),
                                                1,
                                                fontCollection.AsTypedPointer(),
                                                default,
                                                default,
                                                scaleFactor);
                                        }
                                    }
                                }
                            }
                            finally
                            {
                                if (!fontFamilies[i].IsMainFont)
                                {
                                    fontFace.Release();
                                    fontCollection.Release();
                                    if (canvasFontSet is IDisposable disposable) disposable.Dispose();
                                }
                            }
                        }

                        ComPtr<IDWriteFontFallback> systemFontFallback = default;
                        ComPtr<IDWriteFontFallback> fontFallback = default;
                        try
                        {
                            factory.Value.GetSystemFontFallback(systemFontFallback.TypedPointerRef);
                            builder.Value.AddMappings(systemFontFallback.AsTypedPointer());
                            builder.Value.CreateFontFallback(fontFallback.TypedPointerRef);

                            dWriteTextFormat1.Value.SetFontFallback(fontFallback.AsTypedPointer());
                        }
                        finally
                        {
                            systemFontFallback.Release();
                            fontFallback.Release();
                        }
                    }
                }
                finally
                {
                    primaryFontFace.Release();
                    primaryFontCollection.Release();
                    if (primaryCanvasFontSet is IDisposable disposable) disposable.Dispose();
                }
            }
        }

        public static UnicodeRange[]? GetMergedUnicodeRange(UnicodeRange[]? unicodeRange1, UnicodeRange[]? unicodeRange2)
        {
            if (unicodeRange1 == null && unicodeRange2 == null) return null;
            else if (unicodeRange1 != null && unicodeRange2 == null) return unicodeRange1;
            else if (unicodeRange1 == null && unicodeRange2 != null) return unicodeRange2;
            else
            {
                var list = new List<UnicodeRange>();

                for (int i = 0; i < unicodeRange1!.Length; i++)
                {
                    var r1 = unicodeRange1[i];

                    for (int j = 0; j < unicodeRange2!.Length; j++)
                    {
                        var r2 = unicodeRange2[j];

                        var first = Math.Max(r1.first, r2.first);
                        var last = Math.Min(r1.last, r2.last);

                        if (first <= last)
                        {
                            list.Add(new UnicodeRange()
                            {
                                first = first,
                                last = last
                            });
                        }
                    }
                }

                list.Sort(new Comparison<UnicodeRange>((x, y) => x.last.CompareTo(y.last)));

                for (int i = list.Count - 1; i >= 1; i--)
                {
                    var first = Math.Max(list[i].first, list[i - 1].first);
                    var last = Math.Min(list[i].last, list[i - 1].last);

                    if (first <= last)
                    {
                        list[i - 1] = new UnicodeRange()
                        {
                            first = list[i - 1].first,
                            last = list[i].last
                        };
                        list.RemoveAt(i);
                    }
                }

                return list.ToArray();
            }
        }

        internal static bool IsGenericFamilyName(string name)
        {
            return genericFamilyNames.Contains(name, StringComparer.OrdinalIgnoreCase);
        }

        internal static string GetActualFamilyName(CanvasFontFamily fontFallback, string languageTag, out float scaleFactor)
        {
            scaleFactor = 1;

            if (!fontFallback.IsGenericFamilyName) return fontFallback.ToString();

            var languageFontGroup = new Windows.Globalization.Fonts.LanguageFontGroup(languageTag);

            var name = fontFallback.FontFamilyName;

            if (name == "SERIF" || name == "UI-SERIF")
            {
                scaleFactor = (float)(languageFontGroup.TraditionalDocumentFont.ScaleFactor / 100);
                return languageFontGroup.TraditionalDocumentFont.FontFamily;
            }

            if (name == "SANS-SERIF" || name == "UI-SANS-SERIF")
            {
                scaleFactor = (float)(languageFontGroup.ModernDocumentFont.ScaleFactor / 100);
                return languageFontGroup.ModernDocumentFont.FontFamily;
            }

            if (name == "MONOSPACE" || name == "UI-MONOSPACE")
            {
                scaleFactor = (float)(languageFontGroup.FixedWidthTextFont.ScaleFactor / 100);
                return languageFontGroup.FixedWidthTextFont.FontFamily;
            }

            if (name == "SYSTEM-UI")
            {
                scaleFactor = (float)(languageFontGroup.UITextFont.ScaleFactor / 100);
                return languageFontGroup.UITextFont.FontFamily;
            }

            if (name == "EMOJI")
            {
                return "Segoe UI Emoji";
            }

            throw new ArgumentException(name, nameof(fontFallback.FontFamilyName));
        }

    }
}
