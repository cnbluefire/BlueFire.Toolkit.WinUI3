using BlueFire.Toolkit.WinUI3.Extensions;
using BlueFire.Toolkit.WinUI3.Graphics;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Text;
using Windows.Win32.Graphics.DirectWrite;
using WinRT;
using PInvoke = Windows.Win32.PInvoke;

namespace BlueFire.Toolkit.WinUI3.Text
{
    internal static class DWriteHelper
    {
        private static nint[] factories = new nint[2];
        private static Dictionary<(string? fontFamilyName, FontStretch fontStretch, FontStyle fontStyle, FontWeight fontWeight), bool> colorFontMap = new Dictionary<(string? fontFamilyName, FontStretch fontStretch, FontStyle fontStyle, FontWeight fontWeight), bool>();

        internal static unsafe bool IsColorFont(this CanvasFontFace canvasFontFace)
        {
            var props = GetFontFaceProperties(canvasFontFace);
            lock (colorFontMap)
            {
                if (colorFontMap.TryGetValue(props, out var value)) return value;
            }

            ComPtr<IDWriteFontFaceReference> dWriteFontFaceReference = default;
            ComPtr<IDWriteFontFace3> dWriteFontFace = default;

            try
            {
                bool value = false;
                dWriteFontFaceReference = Direct2DInterop.GetWrappedResourcePtr<IDWriteFontFaceReference>(canvasFontFace);
                if (dWriteFontFaceReference.Value.CreateFontFace(dWriteFontFace.TypedPointerRef).Succeeded)
                {
                    value = dWriteFontFace.Value.IsColorFont();
                }

                lock (colorFontMap)
                {
                    colorFontMap[props] = value;
                }
                return value;
            }
            finally
            {
                dWriteFontFace.Release();
                dWriteFontFaceReference.Release();
            }

            static (string? fontFamilyName, FontStretch fontStretch, FontStyle fontStyle, FontWeight fontWeight) GetFontFaceProperties(CanvasFontFace? canvasFontFace)
            {
                string? fontFamilyName = null;

                if (canvasFontFace != null)
                {
                    var familyNames = canvasFontFace.FamilyNames;

                    if (familyNames.Count > 0)
                    {
                        if (familyNames.TryGetValue("en-us", out var name))
                        {
                            fontFamilyName = name;
                        }
                        else
                        {
                            fontFamilyName = familyNames.Values.FirstOrDefault();
                        }
                    }
                    return (fontFamilyName, canvasFontFace.Stretch, canvasFontFace.Style, canvasFontFace.Weight);
                }

                return (null, FontStretch.Normal, FontStyle.Normal, FontWeights.Normal);
            }
        }

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
                    factory = GetIsolatedFactory<IDWriteFactory3>();
                }
                else
                {
                    factory = GetSharedFactory<IDWriteFactory3>();
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
            out ComPtr<IDWriteFontFace3> fontFace,
            out ComPtr<IDWriteFontCollection> fontCollection,
            out CanvasFontSet? canvasFontSet)
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
                    canvasFontSet = new CanvasFontSet(fontFamily.LocationUri);
                }
                catch { }

                if (canvasFontSet != null)
                {
                    var fallbackFont = new CanvasFallbackFont(fontFamily.FontFamilyName, canvasFontSet);
                    if (CreateFontFace(fallbackFont, false, out fontFace, out fontCollection))
                    {
                        fontProperties = GetFontProperties(fontFace);
                        if (fontProperties != null && string.IsNullOrEmpty(fontProperties.FontFamilyName))
                        {
                            fontProperties.FontFamilyName = fontFamily.FontFamilyName;
                        }
                    }
                }
            }

            return fontProperties;
        }

        internal static unsafe CanvasFontProperties GetFontProperties(ComPtr<IDWriteFontFace3> fontFace)
        {
            DWRITE_UNICODE_RANGE[]? unicodeRanges = null;

            uint actualRangeCount = 0;
            var hr = fontFace.Value.GetUnicodeRanges(0, (DWRITE_UNICODE_RANGE*)0, &actualRangeCount);

            if (hr.Value == unchecked((int)0x8007007A) && actualRangeCount > 0)
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

            string actualFamilyName = "";

            using ComPtr<IDWriteLocalizedStrings> familyNames = default;
            hr = fontFace.Value.GetFamilyNames(familyNames.TypedPointerRef);

            if (hr.Succeeded && GetLocalizedName(familyNames, "en-us", out var _name))
            {
                actualFamilyName = _name;
            }

            return new CanvasFontProperties(unicodeRanges ?? Array.Empty<DWRITE_UNICODE_RANGE>(), metrics)
            {
                FontFamilyName = actualFamilyName
            };

            static bool GetLocalizedName(ComPtr<IDWriteLocalizedStrings> _familyNames, string _localeName, out string _name)
            {
                _name = string.Empty;

                fixed (char* _pLocaleName = _localeName)
                {
                    uint index = 0;
                    Windows.Win32.Foundation.BOOL exist = default;
                    var hr = _familyNames.Value.FindLocaleName(_pLocaleName, &index, &exist);

                    if (hr.Failed || !exist) return false;

                    uint length = 0;
                    hr = _familyNames.Value.GetStringLength(index, &length);
                    if (hr.Failed) return false;

                    var buffer = stackalloc char[(int)length + 1];
                    hr = _familyNames.Value.GetString(index, buffer, length + 1);
                    if (hr.Failed) return false;

                    _name = new string(buffer, 0, (int)length);
                    return true;
                }
            }
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
        private DWRITE_FONT_METRICS fontMetrics;

        internal CanvasFontProperties(DWRITE_UNICODE_RANGE[] unicodeRanges, DWRITE_FONT_METRICS fontMetrics)
        {
            this.unicodeRanges = unicodeRanges;
            this.fontMetrics = fontMetrics;
        }

        public string FontFamilyName { get; internal set; } = "";

        public float Ascent => DesignSpaceToEmSpace(fontMetrics.ascent, fontMetrics.designUnitsPerEm);

        public float Descent => DesignSpaceToEmSpace(fontMetrics.descent, fontMetrics.designUnitsPerEm);

        public float LineGap => DesignSpaceToEmSpace(fontMetrics.lineGap, fontMetrics.designUnitsPerEm);

        public float CapHeight => DesignSpaceToEmSpace(fontMetrics.capHeight, fontMetrics.designUnitsPerEm);

        public float LowercaseLetterHeight => DesignSpaceToEmSpace(fontMetrics.xHeight, fontMetrics.designUnitsPerEm);

        public float UnderlinePosition => DesignSpaceToEmSpace(fontMetrics.underlinePosition, fontMetrics.designUnitsPerEm);

        public float UnderlineThickness => DesignSpaceToEmSpace(fontMetrics.underlineThickness, fontMetrics.designUnitsPerEm);

        public float StrikethroughPosition => DesignSpaceToEmSpace(fontMetrics.strikethroughPosition, fontMetrics.designUnitsPerEm);

        public float StrikethroughThickness => DesignSpaceToEmSpace(fontMetrics.strikethroughThickness, fontMetrics.designUnitsPerEm);

        public UnicodeRange[] UnicodeRanges => MemoryMarshal.Cast<DWRITE_UNICODE_RANGE, UnicodeRange>(unicodeRanges).ToArray();


        private static float DesignSpaceToEmSpace(int designSpaceUnits, ushort designUnitsPerEm)
        {
            return (float)(designSpaceUnits) / (float)(designUnitsPerEm);
        }

        internal CanvasFontProperties Clone(string fontFamilyName)
        {
            return new CanvasFontProperties(unicodeRanges, fontMetrics)
            {
                FontFamilyName = fontFamilyName
            };
        }
    }


    public struct UnicodeRange
    {
        /// <summary>The first code point in the Unicode range.</summary>
        public uint first;

        /// <summary>The last code point in the Unicode range.</summary>
        public uint last;
    }

}