using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using WinRT;
using Windows.Win32.Graphics.DirectWrite;
using PInvoke = Windows.Win32.PInvoke;
using System.Diagnostics;
using System.Globalization;
using BlueFire.Toolkit.WinUI3.Graphics;
using BlueFire.Toolkit.WinUI3.Extensions;
using Microsoft.Graphics.Canvas.Text;
using System;
using System.Buffers;
using System.Runtime.CompilerServices;

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

        public static void SetFontFamilySource(
            CanvasTextFormat canvasTextFormat,
            string fontFamilySource)
        {
            var collection = new CanvasFontFamilyCollection(fontFamilySource);

            if (collection.Count > 0)
            {
                SetFallbackFontFamilies(canvasTextFormat, collection);
            }
        }

        private static unsafe void SetFallbackFontFamilies(CanvasTextFormat canvasTextFormat, IReadOnlyList<CanvasFontFamily> fontFamilies)
        {
            if (fontFamilies == null || fontFamilies.Count == 0) return;

            using var dWriteTextFormat1 = Direct2DInterop.GetWrappedResourcePtr<IDWriteTextFormat1>(canvasTextFormat);
            if (dWriteTextFormat1.HasValue)
            {
                using var factory = DWriteHelper.GetSharedFactory<IDWriteFactory2>();

                using (ComPtr<IDWriteFontFallbackBuilder> builder = default)
                {
                    factory.Value.CreateFontFallbackBuilder(builder.TypedPointerRef);

                    for (int i = 0; i < fontFamilies.Count; i++)
                    {
                        AppendToFontFallback(builder, fontFamilies[i]);
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
        }

        private static unsafe void AppendToFontFallback(ComPtr<IDWriteFontFallbackBuilder> builder, CanvasFontFamily fontFamily)
        {
            AppendToFontFallbackCore(builder, fontFamily, new HashSet<string>(), null);
        }

        private static unsafe void AppendToFontFallbackCore(ComPtr<IDWriteFontFallbackBuilder> builder, CanvasFontFamily fontFamily, HashSet<string> usedFontName, UnicodeRange[]? compositeFontUnicodeRanges)
        {
            if (compositeFontUnicodeRanges != null && compositeFontUnicodeRanges.Length == 0) return;

            CanvasFontProperties? fontProperties = null;
            CanvasFontSet? canvasFontSet = null;
            ComPtr<IDWriteFontCollection> fontCollection = default;
            ComPtr<IDWriteFontFace3> fontFace = default;

            try
            {
                if (!fontFamily.IsGenericFamilyName && fontFamily.LocationUri == null)
                {
                    var compositeFont = CompositeFontManager.Find(fontFamily.FontFamilyName);
                    if (compositeFont != null && usedFontName.Add(fontFamily.FontFamilyName))
                    {
                        if (compositeFont.FamilyMaps != null)
                        {
                            for (int i = 0; i < compositeFont.FamilyMaps.Count; i++)
                            {
                                var map = compositeFont.FamilyMaps[i];
                                var mapTargetFamilies = map.GetFamilies();
                                for (int j = 0; j < mapTargetFamilies.Count; j++)
                                {
                                    var mapTargetFamily = mapTargetFamilies[j];
                                    AppendToFontFallbackCore(builder, mapTargetFamily, usedFontName, GetMergedUnicodeRange(compositeFontUnicodeRanges, mapTargetFamily.UnicodeRanges));
                                }
                            }
                        }
                        return;
                    }
                }

                fontProperties = DWriteHelper.GetFontProperties(fontFamily, out fontFace, out fontCollection, out canvasFontSet);

                if (fontProperties != null && fontProperties.UnicodeRanges.Length > 0)
                {
                    //static ref DWRITE_UNICODE_RANGE GetRefUnicodeRange(UnicodeRange[]? range1, DWRITE_UNICODE_RANGE[]? range2)
                    //{
                    //    if (range1 != null && range1.Length > 0) return ref MemoryMarshal.GetReference(MemoryMarshal.Cast<UnicodeRange, DWRITE_UNICODE_RANGE>(range1));
                    //    else return ref MemoryMarshal.GetReference(range2.AsSpan());
                    //}

                    static UnicodeRange[]? GetFontUnicodeRange(UnicodeRange[]? range1, DWRITE_UNICODE_RANGE[]? range2)
                    {
                        if (range1 != null) return range1;
                        else if (range2 != null && range2.Length > 0) return Unsafe.As<UnicodeRange[]>(range2);
                        return null;
                    }

                    var length = 0;
                    if (fontFamily.UnicodeRanges != null && fontFamily.UnicodeRanges.Length > 0)
                    {
                        length = fontFamily.UnicodeRanges.Length;
                    }
                    else if (fontProperties.unicodeRanges != null && fontProperties.unicodeRanges.Length > 0)
                    {
                        length = fontProperties.unicodeRanges.Length;
                    }

                    if (length > 0)
                    {
                        var unicodeRange = GetFontUnicodeRange(fontFamily.UnicodeRanges, fontProperties.unicodeRanges);

                        var unicodeRange2 = GetMergedUnicodeRange(unicodeRange, compositeFontUnicodeRanges);

                        if (unicodeRange2 != null && unicodeRange2.Length > 0)
                        {
                            var scaleFactor = fontFamily.ScaleFactor;

                            var actualName = fontProperties.FontFamilyName;

                            fixed (UnicodeRange* ptr = unicodeRange2)
                            fixed (char* pActualName = actualName)
                            {
                                builder.Value.AddMapping(
                                    (DWRITE_UNICODE_RANGE*)ptr,
                                    (uint)length,
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
            }
            finally
            {
                fontFace.Release();
                fontCollection.Release();
                if (canvasFontSet is IDisposable disposable) disposable.Dispose();
            }

            //void MergeUnicodeRanges(Span<UnicodeRange> _input1, Span<UnicodeRange> _input2, out Span<UnicodeRange> _output, out IMemoryOwner<UnicodeRange>? _outputOwner)
            //{
            //    _output = Span<UnicodeRange>.Empty;
            //    _outputOwner = null;

            //    if (_input1.IsEmpty && _input2.IsEmpty)
            //    {
            //        _output = Span<UnicodeRange>.Empty;
            //    }
            //    else if (!_input1.IsEmpty && !_input2.IsEmpty)
            //    {

            //    }
            //    else if (!_input1.IsEmpty)
            //    {
            //        _output = _input1;
            //    }
            //    else
            //    {
            //        _output = _input2;
            //    }
            //}
        }


        //internal static void GetMergedUnicodeRange(Span<UnicodeRange> unicodeRange1, Span<UnicodeRange> unicodeRange2, out Span<UnicodeRange> outputRanges, out IMemoryOwner<UnicodeRange>? outputOwner)
        //{
        //    outputRanges = Span<UnicodeRange>.Empty;
        //    outputOwner = null;

        //    if (unicodeRange1.IsEmpty && unicodeRange2.IsEmpty) return;
        //    else if (!unicodeRange1.IsEmpty && !unicodeRange2.IsEmpty)
        //    {
        //        var maxCount = unicodeRange1.Length + unicodeRange2.Length;
        //        var list = new List<UnicodeRange>();
        //    }
        //    else if (!unicodeRange1.IsEmpty) outputRanges = unicodeRange1;
        //    else outputRanges = unicodeRange2;



        //    if (unicodeRange1 != null && unicodeRange2 == null) return unicodeRange1;
        //    else if (unicodeRange1 == null && unicodeRange2 != null) return unicodeRange2;
        //    else
        //    {
        //        var list = new List<UnicodeRange>();

        //        for (int i = 0; i < unicodeRange1!.Length; i++)
        //        {
        //            var r1 = unicodeRange1[i];

        //            for (int j = 0; j < unicodeRange2!.Length; j++)
        //            {
        //                var r2 = unicodeRange2[j];

        //                var first = Math.Max(r1.first, r2.first);
        //                var last = Math.Min(r1.last, r2.last);

        //                if (first <= last)
        //                {
        //                    list.Add(new UnicodeRange()
        //                    {
        //                        first = first,
        //                        last = last
        //                    });
        //                }
        //            }
        //        }

        //        list.Sort(new Comparison<UnicodeRange>((x, y) => x.last.CompareTo(y.last)));

        //        for (int i = list.Count - 1; i >= 1; i--)
        //        {
        //            var first = Math.Max(list[i].first, list[i - 1].first);
        //            var last = Math.Min(list[i].last, list[i - 1].last);

        //            if (first <= last)
        //            {
        //                list[i - 1] = new UnicodeRange()
        //                {
        //                    first = list[i - 1].first,
        //                    last = list[i].last
        //                };
        //                list.RemoveAt(i);
        //            }
        //        }

        //        return list.ToArray();
        //    }
        //}

        internal static UnicodeRange[]? GetMergedUnicodeRange(UnicodeRange[]? unicodeRange1, UnicodeRange[]? unicodeRange2)
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
