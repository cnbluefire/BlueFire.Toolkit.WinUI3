using BlueFire.Toolkit.WinUI3.Extensions;
using BlueFire.Toolkit.WinUI3.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32.Graphics.DirectWrite;
using WinRT;
using PInvoke = Windows.Win32.PInvoke;

namespace BlueFire.Toolkit.WinUI3.Text
{
    internal static class DWriteHelper
    {
        private static nint[] factories = new nint[2];

        internal static unsafe bool CreateFontFace(
            CanvasFallbackFont fontIdentifier,
            bool createIsolatedFont,
            [NotNullWhen(true)]
            out ComPtr<IDWriteFontFace3> fontFace,
            out ComPtr<IDWriteFontCollection> fontCollection)
        {
            ComPtr<IDWriteFontFaceReference> fontFaceReference = default;
            ComPtr<IDWriteFontSetBuilder> fontSetBuilder = default;
            ComPtr<IDWriteFontSet> filteredSet = default;
            ComPtr<IDWriteFontSet> fontSet = default;

            fontCollection = default;
            fontFace = default;

            if (string.IsNullOrEmpty(fontIdentifier?.FontFamilyName)) return false;

            try
            {
                ComPtr<IDWriteFactory3> factory;
                if (createIsolatedFont)
                {
                    factory = GetSharedFactory<IDWriteFactory3>();
                }
                else
                {
                    factory = GetIsolatedFactory<IDWriteFactory3>();
                }

                if (fontIdentifier.CanvasFontSet != null)
                {
                    fontSet = Direct2DInterop.GetWrappedResourcePtr<IDWriteFontSet>(fontIdentifier.CanvasFontSet);
                }
                else
                {
                    if (createIsolatedFont)
                    {
                        fontSet = SystemFontHelper.CreateSystemFontSet(factory);
                    }
                    else
                    {
                        fontSet = SystemFontHelper.GetSharedSystemFontSet();
                    }
                }

                if (fontSet.HasValue)
                {
                    fixed (char* ptr = fontIdentifier.FontFamilyName)
                    {
                        var property = new DWRITE_FONT_PROPERTY()
                        {
                            propertyId = DWRITE_FONT_PROPERTY_ID.DWRITE_FONT_PROPERTY_ID_FAMILY_NAME,
                            propertyValue = ptr
                        };

                        fontSet.Value.GetMatchingFonts(&property, 1, filteredSet.TypedPointerRef);

                        if (filteredSet.HasValue && filteredSet.Value.GetFontCount() > 0)
                        {
                            filteredSet.Value.GetFontFaceReference(0, fontFaceReference.TypedPointerRef);
                            if (fontFaceReference.HasValue)
                            {
                                fontFaceReference.Value.CreateFontFace(fontFace.TypedPointerRef);

                                if (fontFace.HasValue)
                                {
                                    if (fontIdentifier.CanvasFontSet != null)
                                    {
                                        nint result = 0;
                                        factory.Value.CreateFontCollectionFromFontSet(filteredSet.AsTypedPointer(), (IDWriteFontCollection1**)(&result));
                                        fontCollection = ComPtr<IDWriteFontCollection>.Attach(result);
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
                fontFaceReference.Release();
                fontSetBuilder.Release();
                fontSet.Release();
                filteredSet.Release();
            }
        }

        internal static CanvasFontProperties? GetFontProperties(
            CanvasFontFamily fontFamily, 
            Func<Uri, IWinRTObject> createCanvasFontSetFunction, 
            out ComPtr<IDWriteFontFace3> fontFace, 
            out ComPtr<IDWriteFontCollection> fontCollection, 
            out IWinRTObject? canvasFontSet)
        {
            CanvasFontProperties? fontProperties = null;
            fontCollection = default;
            fontFace = default;
            canvasFontSet = null;

            if (fontFamily.LocationUri == null)
            {
                fontProperties = SystemFontHelper.GetFontProperties(fontFamily.FontFamilyName);
            }
            else
            {
                try
                {
                    canvasFontSet = createCanvasFontSetFunction(fontFamily.LocationUri);
                }
                catch { }

                if (canvasFontSet != null)
                {
                    var fallbackFont = new CanvasFallbackFont(fontFamily.FontFamilyName, canvasFontSet);
                    if (DWriteHelper.CreateFontFace(fallbackFont, false, out fontFace, out fontCollection))
                    {
                        fontProperties = DWriteHelper.GetFontProperties(fontFace);
                    }
                }
            }

            return fontProperties;
        }

        internal static unsafe CanvasFontProperties GetFontProperties(ComPtr<IDWriteFontFace3> fontFace)
        {
            DWRITE_UNICODE_RANGE[]? unicodeRanges = null;
            float ascent = 0f;
            float descent = 0f;
            float lineGap = 0f;

            uint actualRangeCount = 0;
            try
            {
                fontFace.Value.GetUnicodeRanges(0, (DWRITE_UNICODE_RANGE*)0, &actualRangeCount);
            }
            catch (Exception ex) when (ex.HResult == unchecked((int)0x8007007A)) { }
            if (actualRangeCount > 0)
            {
                var rangeArray = new DWRITE_UNICODE_RANGE[actualRangeCount];

                fixed (DWRITE_UNICODE_RANGE* ptr = rangeArray)
                {
                    fontFace.Value.GetUnicodeRanges(actualRangeCount, ptr, &actualRangeCount);
                }

                unicodeRanges = rangeArray;
            }


            DWRITE_FONT_METRICS metrics = default;
            fontFace.Value.GetMetrics(&metrics);

            ascent = DesignSpaceToEmSpace(metrics.ascent, metrics.designUnitsPerEm);
            descent = DesignSpaceToEmSpace(metrics.descent, metrics.designUnitsPerEm);
            lineGap = DesignSpaceToEmSpace(metrics.lineGap, metrics.designUnitsPerEm);

            return new CanvasFontProperties(unicodeRanges ?? Array.Empty<DWRITE_UNICODE_RANGE>(), ascent, descent, lineGap);
        }

        private static float DesignSpaceToEmSpace(int designSpaceUnits, ushort designUnitsPerEm)
        {
            return (float)(designSpaceUnits) / (float)(designUnitsPerEm);
        }

        private static unsafe nint GetFactoryCore(DWRITE_FACTORY_TYPE type)
        {
            var index = type == DWRITE_FACTORY_TYPE.DWRITE_FACTORY_TYPE_SHARED ? 0 : 1;
            if (factories[index] == 0)
            {
                lock (factories)
                {
                    if (factories[index] == 0)
                    {
                        PInvoke.DWriteCreateFactory(type, IDWriteFactory.IID_Guid, out var factory)
                            .ThrowOnFailure();

                        factories[index] = (nint)factory;
                    }
                }
            }
            return factories[index];
        }

        internal static ComPtr<T> GetSharedFactory<T>() where T : unmanaged
        {
            var factory = GetFactoryCore(DWRITE_FACTORY_TYPE.DWRITE_FACTORY_TYPE_SHARED);
            return ComPtr<T>.FromAbi(factory);
        }

        internal static ComPtr<T> GetIsolatedFactory<T>() where T : unmanaged
        {
            var factory = GetFactoryCore(DWRITE_FACTORY_TYPE.DWRITE_FACTORY_TYPE_ISOLATED);
            return ComPtr<T>.FromAbi(factory);
        }
    }
    public class CanvasFontProperties
    {
        internal readonly DWRITE_UNICODE_RANGE[] unicodeRanges;

        internal CanvasFontProperties(DWRITE_UNICODE_RANGE[] unicodeRanges, float ascent, float descent, float lineGap)
        {
            this.unicodeRanges = unicodeRanges;
            Ascent = ascent;
            Descent = descent;
            LineGap = lineGap;
        }

        public float Ascent { get; }

        public float Descent { get; }

        public float LineGap { get; }

        public UnicodeRange[] UnicodeRanges => MemoryMarshal.Cast<DWRITE_UNICODE_RANGE, UnicodeRange>(unicodeRanges).ToArray();
    }

    public struct UnicodeRange
    {
        /// <summary>The first code point in the Unicode range.</summary>
        public uint first;

        /// <summary>The last code point in the Unicode range.</summary>
        public uint last;
    }

}