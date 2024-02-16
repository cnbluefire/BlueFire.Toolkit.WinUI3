using BlueFire.Toolkit.WinUI3.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Text;

namespace BlueFire.Toolkit.WinUI3.Controls
{
    partial class TextView
    {
        internal record struct FormattedTextProperties(
            FormattedTextImmutableProperties ImmutableProperties,
            FormattedTextMutableProperties MutableProperties);

        internal record struct FormattedTextImmutableProperties(
            string Text,
            string? LocaleName,
            FormattedTextTypeface Typeface,
            double FontSize,
            bool IsColorFontEnabled,
            FlowDirection FlowDirection,
            TextAlignment HorizontalTextAlignment,
            TextTrimming TextTrimming,
            TextWrapping TextWrapping,
            double StrokeThickness,
            double RasterizationScale);

        internal record struct FormattedTextMutableProperties(
            Color TextColor,
            Color TextSecondaryColor,
            Color TextStrokeColor,
            Color TextStrokeSecondaryColor,
            bool UseLayoutRounding);

        private FormattedTextProperties GetCurrentTextProperties()
        {
            var textColor = GetTextColor();
            var strokeColor = GetStrokeColor();
            var fontFamily = GetFontFamily();

            return new FormattedTextProperties(
                new FormattedTextImmutableProperties(
                    Text,
                    null,
                    new FormattedTextTypeface(fontFamily,
                        FontWeight,
                        FontStyle,
                        FontStretch),
                    FontSize,
                    IsColorFontEnabled,
                    FlowDirection,
                    HorizontalTextAlignment,
                    TextTrimming,
                    TextWrapping,
                    StrokeThickness,
                    XamlRoot.RasterizationScale),
                new FormattedTextMutableProperties(
                    textColor,
                    GetTextSecondaryColor(textColor),
                    strokeColor,
                    GetStrokeSecondaryColor(strokeColor),
                    UseLayoutRounding));
        }

        private static bool ShouldRecreateFormattedText(in FormattedTextProperties? value1, in FormattedTextProperties? value2)
        {
            if (!value1.HasValue && !value2.HasValue) return false;
            else if (value1.HasValue && value2.HasValue)
            {
                if (value1.Value == value2.Value) return false;
                return value1.Value.ImmutableProperties != value2.Value.ImmutableProperties;
            }
            else return true;
        }
    }
}
