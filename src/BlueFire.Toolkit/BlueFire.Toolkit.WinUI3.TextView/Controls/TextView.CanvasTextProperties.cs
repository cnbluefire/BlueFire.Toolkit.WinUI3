using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;
using Microsoft.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace BlueFire.Toolkit.WinUI3.Controls
{
    partial class TextView
    {
        private Color GetTextColor()
        {
            Brush? foreground = Foreground;

            if (foreground == null)
            {
                if (TryGetPropertyFromParent(p =>
                    (p as ContentPresenter)?.Foreground ??
                    (p as Control)?.Foreground ??
                    (p as TextElement)?.Foreground ??
                    (p as RichTextBlock)?.Foreground, out Brush? value))
                {
                    foreground = value;
                }
            }

            if (foreground is SolidColorBrush scb)
            {
                var color = scb.Color;
                return Color.FromArgb((byte)(color.A * scb.Opacity), color.R, color.G, color.B);
            }

            return Colors.Transparent;
        }

        private Color GetTextSecondaryColor(Color? foregroundColor)
        {
            if (SecondaryForeground is SolidColorBrush scb)
            {
                var color = scb.Color;
                return Color.FromArgb((byte)(color.A * scb.Opacity), color.R, color.G, color.B);
            }

            if (!foregroundColor.HasValue)
            {
                foregroundColor = GetTextColor();
            }

            if (foregroundColor.Value.A != 0)
            {
                var color = foregroundColor.Value;
                return Color.FromArgb((byte)(color.A * 0.8f), color.R, color.G, color.B);
            }
            return Colors.Transparent;
        }

        private Color GetStrokeColor()
        {
            if (StrokeThickness > 0 && Stroke is SolidColorBrush scb)
            {
                var color = scb.Color;
                return Color.FromArgb((byte)(color.A * scb.Opacity), color.R, color.G, color.B);
            }
            return Colors.Transparent;
        }

        private Color GetStrokeSecondaryColor(Color? strokeColor)
        {
            if (StrokeThickness > 0 && SecondaryStroke is SolidColorBrush scb)
            {
                var color = scb.Color;
                return Color.FromArgb((byte)(color.A * scb.Opacity), color.R, color.G, color.B);
            }
            return Colors.Transparent;
        }

        private FontFamily GetFontFamily()
        {
            var fontFamily = FontFamily;
            if (fontFamily == null && IsLoaded)
            {
                if (TryGetPropertyFromParent(p => (p as Control)?.FontFamily, out FontFamily? value))
                {
                    fontFamily = value;
                }
            }

            return string.IsNullOrEmpty(fontFamily?.Source) ? FontFamily.XamlAutoFontFamily : fontFamily;
        }

        private bool TryGetPropertyFromParent<T>(Func<DependencyObject?, object?> getValueFunc, out T? value)
        {
            value = default;

            var p = this.Parent;
            bool flag = false;

            do
            {
                var value2 = getValueFunc(p);
                flag = value2 != null;

                if (flag)
                {
                    value = (T?)value2;
                }
                else
                {
                    if (p is FrameworkElement element)
                    {
                        p = element.Parent;
                    }
                    else
                    {
                        p = VisualTreeHelper.GetParent(p);
                    }
                }
            }
            while (p != null && !flag);

            return flag;
        }

        private CanvasTextDirection GetCanvasTextDirection(FormattedTextProperties textProperties) => textProperties.ImmutableProperties.FlowDirection switch
        {
            FlowDirection.LeftToRight => CanvasTextDirection.LeftToRightThenTopToBottom,
            _ => CanvasTextDirection.RightToLeftThenTopToBottom,
        };

        private CanvasHorizontalAlignment GetCanvasHorizontalAlignment(FormattedTextProperties textProperties) => textProperties.ImmutableProperties.HorizontalTextAlignment switch
        {
            TextAlignment.Center => CanvasHorizontalAlignment.Center,
            //TextAlignment.Start => FlowDirection switch
            //{
            //    FlowDirection.LeftToRight => CanvasHorizontalAlignment.Left,
            //    _ => CanvasHorizontalAlignment.Right
            //},
            TextAlignment.End => FlowDirection switch
            {
                FlowDirection.LeftToRight => CanvasHorizontalAlignment.Right,
                _ => CanvasHorizontalAlignment.Left
            },
            TextAlignment.Justify => CanvasHorizontalAlignment.Justified,
            _ => FlowDirection switch
            {
                FlowDirection.LeftToRight => CanvasHorizontalAlignment.Left,
                _ => CanvasHorizontalAlignment.Right
            },
        };

        private CanvasTextTrimmingGranularity GetTrimmingGranularity(FormattedTextProperties textProperties) => textProperties.ImmutableProperties.TextTrimming switch
        {
            TextTrimming.None => CanvasTextTrimmingGranularity.None,
            TextTrimming.WordEllipsis => CanvasTextTrimmingGranularity.Word,
            TextTrimming.CharacterEllipsis => CanvasTextTrimmingGranularity.Character,
            _ => CanvasTextTrimmingGranularity.None
        };

        private CanvasWordWrapping GetWordWrapping(FormattedTextProperties textProperties) => textProperties.ImmutableProperties.TextWrapping switch
        {
            TextWrapping.NoWrap => CanvasWordWrapping.NoWrap,
            TextWrapping.Wrap => CanvasWordWrapping.Wrap,
            TextWrapping.WrapWholeWords => CanvasWordWrapping.WholeWord,
            _ => CanvasWordWrapping.NoWrap
        };

        private CanvasDrawTextOptions GetOptions(FormattedTextProperties textProperties)
        {
            var options = CanvasDrawTextOptions.Default;

            if (!textProperties.MutableProperties.UseLayoutRounding) options |= CanvasDrawTextOptions.NoPixelSnap;
            if (textProperties.ImmutableProperties.TextTrimming == TextTrimming.Clip) options |= CanvasDrawTextOptions.Clip;
            if (textProperties.ImmutableProperties.IsColorFontEnabled) options |= CanvasDrawTextOptions.EnableColorFont;

            return options;
        }

    }
}
