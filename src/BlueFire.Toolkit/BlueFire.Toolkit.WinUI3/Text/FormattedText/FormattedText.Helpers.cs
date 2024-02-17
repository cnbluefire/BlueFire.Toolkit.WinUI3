using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Text;

namespace BlueFire.Toolkit.WinUI3.Text
{
    partial class FormattedText
    {
        private CanvasLineMetrics[]? metrics;
        private Rect? drawBounds;
        private Rect layoutBounds;
        private Rect layoutBoundsIncludingTrailingWhitespace;
        private double? minWidth;
        private double? overhangAfter;
        private double? overhangLeading;
        private double? overhangTrailing;
        private IReadOnlyList<FormattedTextLineGlyphRuns>? lineGlyphRuns;

        private CanvasLineMetrics? TryGetFirstLineMetrics() => GetLineMetrics()?.FirstOrDefault();

        private Rect GetDrawBounds()
        {
            ThrowIfDisposed();

            if (!drawBounds.HasValue)
            {
                lock (locker)
                {
                    if (!drawBounds.HasValue)
                    {
                        drawBounds = EnsureTextLayout().DrawBounds;
                    }
                }
            }

            return drawBounds.Value;
        }

        private CanvasLineMetrics[] GetLineMetrics()
        {
            ThrowIfDisposed();

            if (metrics == null)
            {
                lock (locker)
                {
                    if (metrics == null)
                    {
                        metrics = EnsureTextLayout().LineMetrics;
                    }
                }
            }

            return metrics;
        }

        private Size GetRequestedSize()
        {
            var width = maxTextWidth == 0 ? float.MaxValue : maxTextWidth;
            var height = maxTextHeight == 0 ? float.MaxValue : maxTextHeight;

            return new Size(width, height);
        }

        private double GetMinWidth()
        {
            ThrowIfDisposed();

            var minWidth = this.minWidth;

            if (!minWidth.HasValue)
            {
                lock (locker)
                {
                    minWidth = this.minWidth;

                    if (!minWidth.HasValue)
                    {
                        this.minWidth = EnsureTextLayout().GetMinimumLineLength();
                        minWidth = this.minWidth;
                    }
                }
            }

            return minWidth.Value;
        }

        private double GetOverhangAfter()
        {
            ThrowIfDisposed();

            var overhangAfter = this.overhangAfter;

            if (!overhangAfter.HasValue)
            {
                lock (locker)
                {
                    overhangAfter = this.overhangAfter;

                    if (!overhangAfter.HasValue)
                    {
                        var lineMetrics = GetLineMetrics();

                        if (lineMetrics != null && lineMetrics.Length > 0)
                        {
                            var lastLineBottom = lineMetrics.Max(c => c.Height);
                            this.overhangAfter = GetDrawBounds().Bottom - lastLineBottom;
                        }
                        else
                        {
                            this.overhangAfter = 0;
                        }
                        overhangAfter = this.overhangAfter;
                    }
                }
            }

            return overhangAfter.Value;
        }

        private (double overhangLeading, double overhangTrailing) GetOverhangLeadingAndTrailing()
        {
            ThrowIfDisposed();

            var overhangLeading = this.overhangLeading;
            var overhangTrailing = this.overhangTrailing;

            if (!overhangLeading.HasValue)
            {
                lock (locker)
                {
                    overhangLeading = this.overhangLeading;
                    overhangTrailing = this.overhangTrailing;

                    if (!overhangLeading.HasValue)
                    {
                        var lineMetrics = GetLineMetrics();
                        if (lineMetrics != null && lineMetrics.Length > 0)
                        {
                            var drawBounds = GetDrawBounds();

                            var leading = double.MaxValue;
                            var trailing = double.MaxValue;

                            for (int i = 0; i < lineMetrics.Length; i++)
                            {
                                var m = lineMetrics[i];
                                leading = Math.Min(leading, m.LeadingWhitespaceBefore);
                                trailing = Math.Min(trailing, m.LeadingWhitespaceAfter);
                            }

                            this.overhangLeading = leading;
                            this.overhangTrailing = trailing;
                        }
                        else
                        {
                            this.overhangLeading = 0;
                            this.overhangTrailing = 0;
                        }

                        overhangLeading = this.overhangLeading;
                        overhangTrailing = this.overhangTrailing;
                    }
                }
            }

            return (overhangLeading.Value, overhangTrailing!.Value);
        }

        private IReadOnlyList<FormattedTextLineGlyphRuns> GetLineGlyphRuns()
        {
            ThrowIfDisposed();

            if (lineGlyphRuns == null)
            {
                lock (locker)
                {
                    lineGlyphRuns = CreateLineGlyphRuns();
                }
            }

            return lineGlyphRuns;
        }

        private CanvasTextLayout EnsureTextLayout()
        {
            ThrowIfDisposed();

            if (textLayout == null)
            {
                lock (locker)
                {
                    if (textLayout == null)
                    {
                        ICanvasResourceCreator creator;
                        if (canvasResourceCreator != null)
                            creator = canvasResourceCreator.Invoke();
                        else
                            creator = CanvasDevice.GetSharedDevice(true);

                        textLayout = CreateCanvasTextLayout(creator);

                        drawBounds = textLayout.DrawBounds;
                        layoutBounds = textLayout.LayoutBounds;
                        layoutBoundsIncludingTrailingWhitespace = textLayout.LayoutBoundsIncludingTrailingWhitespace;
                    }
                }
            }

            return textLayout!;
        }

        internal CanvasTextLayout GetInternalCanvasTextLayout()
        {
            ThrowIfDisposed();

            return EnsureTextLayout();
        }

        public CanvasTextLayout DetachInternalCanvasTextLayout()
        {
            ThrowIfDisposed();

            lock (locker)
            {
                var textLayout = EnsureTextLayout();
                this.textLayout = null;
                InvalidateMetrics();

                return textLayout;
            }
        }

        public CanvasTextLayout CreateCanvasTextLayout(ICanvasResourceCreator canvasResourceCreator)
        {
            var actualLineHeight = GetActualLineHeight();

            using (var textFormat = new CanvasTextFormat()
            {
                FontFamily = null,
                FontSize = (float)fontSize,
                FontStretch = typeface.FontStretch,
                FontWeight = typeface.FontWeight,
                FontStyle = typeface.FontStyle,

                LocaleName = localeName,

                Direction = ConvertToCanvasTextDirection(flowDirection),
                HorizontalAlignment = ConvertToCanvasHorizontalAlignment(textAlignment, flowDirection),
                VerticalAlignment = CanvasVerticalAlignment.Top,

                TrimmingGranularity = ConvertToTrimmingGranularity(textTrimming),
                TrimmingSign = CanvasTrimmingSign.Ellipsis,
                Options = GetOptions(textTrimming, IsPixelSnappingEnabled, IsColorFontEnabled),

                WordWrapping = ConvertToWordWrapping(textWrapping),
                LineSpacingBaseline = (float)(actualLineHeight * 0.8),
                LineSpacing = (float)actualLineHeight
            })
            {
                CanvasTextFormatHelper.SetFontFamilySource(textFormat, typeface.ActualFontFamilyName, localeName);

                var requestedSize = GetRequestedSize();

                var textLayout = new CanvasTextLayout(
                    canvasResourceCreator,
                    Text,
                    textFormat,
                    (float)requestedSize.Width,
                    (float)requestedSize.Height);

                return textLayout;
            }
        }

        private double GetActualLineHeight()
        {
            return lineHeight == 0 ? fontSize / 0.75 : lineHeight;
        }

        private void ThrowIfDisposed()
        {
            if (disposedValue) throw new ObjectDisposedException(nameof(FormattedText));
        }

        private void InvalidateMetrics()
        {
            lock (locker)
            {
                metrics = null;
                drawBounds = null;
                minWidth = null;
                overhangAfter = null;
                overhangLeading = null;
                overhangTrailing = null;

                if (textLayout != null)
                {
                    var oldLayoutBounds = layoutBounds;

                    drawBounds = textLayout.DrawBounds;
                    layoutBounds = textLayout.LayoutBounds;
                    layoutBoundsIncludingTrailingWhitespace = textLayout.LayoutBoundsIncludingTrailingWhitespace;

                    if (oldLayoutBounds != layoutBounds)
                    {
                        lineGlyphRuns = null;
                    }
                }
                else
                {
                    drawBounds = default;
                    layoutBounds = default;
                    layoutBoundsIncludingTrailingWhitespace = default;
                    lineGlyphRuns = null;
                }
            }
        }

        //private void InvalidateLayout()
        //{
        //    lock (locker)
        //    {
        //        textLayout?.Dispose();
        //        textLayout = null;

        //        textFormat?.Dispose();
        //        textFormat = null;

        //        InvalidateMetrics();
        //    }
        //}


        private static CanvasDrawTextOptions GetOptions(TextTrimming textTrimming, bool isPixelSnappingEnabled, bool isColorFontEnabled)
        {
            var options = CanvasDrawTextOptions.Default;

            if (!isPixelSnappingEnabled) options |= CanvasDrawTextOptions.NoPixelSnap;
            if (textTrimming == TextTrimming.Clip) options |= CanvasDrawTextOptions.Clip;
            if (isColorFontEnabled) options |= CanvasDrawTextOptions.EnableColorFont;

            return options;
        }

        private static CanvasTextDirection ConvertToCanvasTextDirection(FlowDirection flowDirection) => flowDirection switch
        {
            FlowDirection.LeftToRight => CanvasTextDirection.LeftToRightThenTopToBottom,
            _ => CanvasTextDirection.RightToLeftThenTopToBottom,
        };

        private static CanvasHorizontalAlignment ConvertToCanvasHorizontalAlignment(TextAlignment textAlignment, FlowDirection flowDirection) => textAlignment switch
        {
            TextAlignment.Center => CanvasHorizontalAlignment.Center,
            //TextAlignment.Start => FlowDirection switch
            //{
            //    FlowDirection.LeftToRight => CanvasHorizontalAlignment.Left,
            //    _ => CanvasHorizontalAlignment.Right
            //},
            TextAlignment.End => flowDirection switch
            {
                FlowDirection.LeftToRight => CanvasHorizontalAlignment.Right,
                _ => CanvasHorizontalAlignment.Left
            },
            TextAlignment.Justify => CanvasHorizontalAlignment.Justified,
            _ => flowDirection switch
            {
                FlowDirection.LeftToRight => CanvasHorizontalAlignment.Left,
                _ => CanvasHorizontalAlignment.Right
            },
        };

        private static CanvasTextTrimmingGranularity ConvertToTrimmingGranularity(TextTrimming textTrimming) => textTrimming switch
        {
            TextTrimming.None => CanvasTextTrimmingGranularity.None,
            TextTrimming.WordEllipsis => CanvasTextTrimmingGranularity.Word,
            TextTrimming.CharacterEllipsis => CanvasTextTrimmingGranularity.Character,
            _ => CanvasTextTrimmingGranularity.None
        };

        private static CanvasWordWrapping ConvertToWordWrapping(TextWrapping textWrapping) => textWrapping switch
        {
            TextWrapping.NoWrap => CanvasWordWrapping.NoWrap,
            TextWrapping.Wrap => CanvasWordWrapping.EmergencyBreak,
            TextWrapping.WrapWholeWords => CanvasWordWrapping.WholeWord,
            _ => CanvasWordWrapping.NoWrap
        };

    }
}
