using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using WinRT;
using Windows.Win32.Graphics.DirectWrite;
using PInvoke = Windows.Win32.PInvoke;
using System.Diagnostics;

namespace BlueFire.Toolkit.WinUI3.Graphics
{
    public static class CanvasTextFormatHelper
    {
        private static IDWriteFactory? sharedFactory;
        private static object sharedFactoryLocker = new object();

        public static unsafe void SetFallbackFonts(object canvasTextFormat, IReadOnlyList<FontIdentifier> fallbackFonts)
        {
            if (fallbackFonts == null || fallbackFonts.Count == 0) return;

            var dWriteTextFormat1 = Direct2DInterop.GetWrappedResource<IDWriteTextFormat1>(canvasTextFormat);
            if (dWriteTextFormat1 != null)
            {
                var factory = GetSharedFactory<IDWriteFactory2>();

                factory.CreateFontFallbackBuilder(out var builder);

                for (int i = 0; i < fallbackFonts.Count; i++)
                {
                    if (TryGetFontFace(fallbackFonts[i], out var fontFace, out var fontCollection))
                    {
                        try
                        {
                            fontFace.GetFamilyNames(out var names);

                            if (names != null && names.GetCount() > 0)
                            {
                                var rangeArray = GetUnicodeRanges(fontFace);

                                if (rangeArray != null && rangeArray.Length > 0)
                                {
                                    var len = fallbackFonts[i].FontFamilyName.Length + 1;
                                    var familyNameArray = new char[1][] { new char[len] };

                                    fallbackFonts[i].FontFamilyName.CopyTo(familyNameArray[0]);
                                    familyNameArray[0][len - 1] = '\0';

                                    fixed (DWRITE_UNICODE_RANGE* ptr = rangeArray)
                                    fixed (char* familyNameArrayPtr = familyNameArray[0])
                                    {
                                        builder.AddMapping(
                                            ptr,
                                            (uint)rangeArray.Length,
                                            (ushort**)(&familyNameArrayPtr),
                                            (uint)familyNameArray.Length,
                                            fontCollection,
                                            default,
                                            default,
                                            fallbackFonts[i].ScaleFactor);
                                    }
                                }
                            }
                        }
                        finally
                        {
                            Marshal.FinalReleaseComObject(fontFace);
                            if (fontCollection != null) Marshal.ReleaseComObject(fontCollection);
                        }
                    }
                }

                factory.GetSystemFontFallback(out var systemFontFallback);
                builder.AddMappings(systemFontFallback);

                builder.CreateFontFallback(out var fontFallback);
                dWriteTextFormat1.SetFontFallback(fontFallback);
            }
        }

        [DebuggerNonUserCode]
        private static unsafe DWRITE_UNICODE_RANGE[]? GetUnicodeRanges(IDWriteFontFace3 fontFace)
        {
            uint actualRangeCount = 0;
            try
            {
                fontFace.GetUnicodeRanges(0, (DWRITE_UNICODE_RANGE*)0, out actualRangeCount);
            }
            catch (Exception ex) when (ex.HResult == unchecked((int)0x8007007A)) { }
            if (actualRangeCount > 0)
            {
                var rangeArray = new DWRITE_UNICODE_RANGE[actualRangeCount];

                fixed (DWRITE_UNICODE_RANGE* ptr = rangeArray)
                {
                    fontFace.GetUnicodeRanges(actualRangeCount, ptr, out actualRangeCount);
                }

                return rangeArray;
            }

            return null;
        }

        private static unsafe bool TryGetFontFace(
            FontIdentifier fontIdentifier,
            [NotNullWhen(true)]
            out IDWriteFontFace3? fontFace,
            out IDWriteFontCollection? fontCollection)
        {
            IDWriteFontFaceReference? fontFaceReference = null;
            IDWriteFontSetBuilder? fontSetBuilder = null;
            IDWriteFontSet? systemFontSet = null;
            IDWriteFontSet? filteredSet = null;
            IDWriteFontSet? fontSet = null;

            fontCollection = null;
            fontFace = null;

            if (string.IsNullOrEmpty(fontIdentifier?.FontFamilyName)) return false;

            try
            {
                var factory = GetSharedFactory<IDWriteFactory3>();

                if (fontIdentifier.CanvasFontSet != null)
                {
                    fontSet = Direct2DInterop.GetWrappedResource<IDWriteFontSet>(fontIdentifier.CanvasFontSet);
                }
                else
                {
                    factory.GetSystemFontSet(out systemFontSet);

                    if (systemFontSet != null)
                    {
                        factory.CreateFontSetBuilder(out fontSetBuilder);

                        var fontCount = systemFontSet.GetFontCount();
                        for (uint i = 0; i < fontCount; i++)
                        {
                            systemFontSet.GetFontFaceReference(i, out var tmpFontRef);

                            try
                            {
                                if (tmpFontRef.GetLocality() == DWRITE_LOCALITY.DWRITE_LOCALITY_LOCAL)
                                {
                                    fontSetBuilder.AddFontFaceReference(tmpFontRef);
                                }
                            }
                            finally
                            {
                                Marshal.ReleaseComObject(tmpFontRef);
                            }
                        }

                        fontSetBuilder.CreateFontSet(out fontSet);
                    }
                }

                if (fontSet != null)
                {
                    fixed (char* ptr = fontIdentifier.FontFamilyName)
                    {
                        var property = new DWRITE_FONT_PROPERTY()
                        {
                            propertyId = DWRITE_FONT_PROPERTY_ID.DWRITE_FONT_PROPERTY_ID_FAMILY_NAME,
                            propertyValue = ptr
                        };

                        fontSet.GetMatchingFonts(&property, 1, out filteredSet);

                        if (filteredSet != null && filteredSet.GetFontCount() > 0)
                        {
                            filteredSet.GetFontFaceReference(0, out fontFaceReference);
                            if (fontFaceReference != null)
                            {
                                fontFaceReference.CreateFontFace(out fontFace);

                                if (fontFace != null)
                                {
                                    if (fontIdentifier.CanvasFontSet != null)
                                    {
                                        factory.CreateFontCollectionFromFontSet(filteredSet, out var fontCollection1);
                                        fontCollection = (IDWriteFontCollection)fontCollection1;
                                    }

                                    return true;
                                }
                            }
                        }
                    }
                }

                return false;
            }
            finally
            {
                if (fontFaceReference != null) Marshal.FinalReleaseComObject(fontFaceReference);
                if (systemFontSet != null) Marshal.ReleaseComObject(systemFontSet);
                if (fontSetBuilder != null) Marshal.ReleaseComObject(fontSetBuilder);
                if (fontSet != null) Marshal.ReleaseComObject(fontSet);
                if (filteredSet != null) Marshal.ReleaseComObject(filteredSet);

            }

        }

        private static IDWriteFactory GetSharedFactory()
        {
            if (sharedFactory == null)
            {
                lock (sharedFactoryLocker)
                {
                    if (sharedFactory == null)
                    {
                        var guid = typeof(IDWriteFactory).GetGuidType().GUID;
                        PInvoke.DWriteCreateFactory(DWRITE_FACTORY_TYPE.DWRITE_FACTORY_TYPE_SHARED, in guid, out var factory)
                            .ThrowOnFailure();

                        sharedFactory = (IDWriteFactory)factory;
                    }
                }
            }
            if (sharedFactory == null) throw new ArgumentException(null, nameof(sharedFactory));

            return sharedFactory;
        }

        private static T GetSharedFactory<T>() where T : IDWriteFactory
        {
            return (T)GetSharedFactory();
        }

        public record class FontIdentifier(string FontFamilyName, IWinRTObject? CanvasFontSet, float ScaleFactor = 1);
    }
}
