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
        private record struct FormattedTextProperties(
            FormattedTextImmutableProperties ImmutableProperties,
            FormattedTextMutableProperties MutableProperties);

        private record struct FormattedTextImmutableProperties(
            string Text,
            string? LocaleName,
            FormattedTextTypeface Typeface,
            double FontSize,
            bool IsColorFontEnabled,
            bool UseLayoutRounding);

        private record struct FormattedTextMutableProperties(
            double RasterizationScale,
            FlowDirection FlowDirection,
            TextAlignment HorizontalTextAlignment,
            TextTrimming TextTrimming,
            TextWrapping TextWrapping,
            Color TextColor,
            Color TextSecondaryColor,
            Color TextStrokeColor,
            Color TextStrokeSecondaryColor,
            double StrokeThickness);

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
                    UseLayoutRounding),
                new FormattedTextMutableProperties(
                    XamlRoot.RasterizationScale,
                    FlowDirection,
                    HorizontalTextAlignment,
                    TextTrimming,
                    TextWrapping,
                    textColor,
                    GetTextSecondaryColor(textColor),
                    strokeColor,
                    GetStrokeSecondaryColor(strokeColor),
                    StrokeThickness));
        }

        private static bool ShouldRecreateFormattedText(FormattedTextProperties? value1, FormattedTextProperties? value2)
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
