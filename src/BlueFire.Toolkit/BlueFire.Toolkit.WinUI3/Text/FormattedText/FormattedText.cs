using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace BlueFire.Toolkit.WinUI3.Text
{
    /// <summary>
    /// Provides low-level control for drawing text.
    /// </summary>
    public partial class FormattedText : IDisposable
    {
        private bool disposedValue;

        private readonly Func<ICanvasResourceCreator>? canvasResourceCreator;
        private readonly string? localeName;
        private readonly FormattedTextTypeface typeface;
        private readonly double fontSize;
        private CanvasTextLayout? textLayout;
        private FlowDirection flowDirection;
        private double lineHeight;
        private double maxTextWidth;
        private double maxTextHeight;
        private TextAlignment textAlignment;
        private TextTrimming textTrimming;
        private TextWrapping textWrapping;
        private object locker = new object();

        /// <summary>
        /// Initializes a new instance of the FormattedText class.
        /// </summary>
        /// <param name="text">The text to be displayed.</param>
        /// <param name="localeName">The specific locale name of the text.</param>
        /// <param name="flowDirection">The direction the text is read.</param>
        /// <param name="typeface">The font family, weight, style and stretch the text should be formatted with.</param>
        /// <param name="fontSize">The font size the text should be formatted at.</param>
        /// <param name="isPixelSnappingEnabled"></param>
        /// <param name="isColorFontEnabled"></param>
        /// <param name="canvasResourceCreator">The win2d canvas resource creator factory.</param>
        public FormattedText(
            string text,
            string? localeName,
            FlowDirection flowDirection,
            FormattedTextTypeface typeface,
            double fontSize,
            bool isPixelSnappingEnabled,
            bool isColorFontEnabled,
            Func<ICanvasResourceCreator>? canvasResourceCreator = null)
        {
            this.canvasResourceCreator = canvasResourceCreator;

            Text = text;
            this.localeName = localeName;
            this.flowDirection = flowDirection;
            this.typeface = typeface;
            this.fontSize = fontSize;

            IsPixelSnappingEnabled = isPixelSnappingEnabled;
            IsColorFontEnabled = isColorFontEnabled;
            textAlignment = TextAlignment.Start;
            textTrimming = TextTrimming.WordEllipsis;
            textWrapping = TextWrapping.NoWrap;
        }

        /// <summary>
        /// Gets the distance from the top of the first line to the baseline of the first line of a FormattedText object.
        /// </summary>
        public double Baseline => TryGetFirstLineMetrics()?.Baseline ?? 0;

        /// <summary>
        /// Gets the distance from the topmost drawn pixel of the first line to the bottommost drawn pixel of the last line.
        /// </summary>
        public double Extent => GetDrawBounds().Height;

        /// <summary>
        /// Gets or sets the FlowDirection of a FormattedText object.
        /// </summary>
        public FlowDirection FlowDirection
        {
            get => flowDirection;
            set
            {
                if (flowDirection != value)
                {
                    lock (locker)
                    {
                        if (flowDirection != value)
                        {
                            flowDirection = value;

                            if (textLayout != null)
                            {
                                textLayout.Direction = ConvertToCanvasTextDirection(flowDirection);
                                textLayout.HorizontalAlignment = ConvertToCanvasHorizontalAlignment(textAlignment, flowDirection);
                                InvalidateMetrics();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the width between the leading and trailing alignment points of a line, excluding any trailing white-space characters.
        /// </summary>
        public double Width => EnsureTextLayout().LayoutBounds.Right;

        /// <summary>
        /// Gets the distance from the top of the first line to the bottom of the last line of the FormattedText object.
        /// </summary>
        public double Height => EnsureTextLayout().LayoutBounds.Bottom;

        /// <summary>
        /// Gets the width between the leading and trailing alignment points of a line, including any trailing white-space characters.
        /// </summary>
        public double WidthIncludingTrailingWhitespace => EnsureTextLayout().LayoutBoundsIncludingTrailingWhitespace.Right;

        /// <summary>
        /// Gets or sets the line height, or line spacing, between lines of text.
        /// </summary>
        public double LineHeight
        {
            get => lineHeight;
            set
            {
                if (lineHeight != value)
                {
                    lock (locker)
                    {
                        if (lineHeight != value)
                        {
                            lineHeight = value;

                            if (textLayout != null)
                            {
                                var actualLineHeight = GetActualLineHeight();

                                textLayout.LineSpacing = (float)actualLineHeight;
                                textLayout.LineSpacingBaseline = (float)(actualLineHeight * 0.8);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the maximum text width (length) for a line of text.
        /// </summary>
        public double MaxTextWidth
        {
            get => maxTextWidth;
            set
            {
                if (maxTextWidth != value)
                {
                    lock (locker)
                    {
                        if (maxTextWidth != value)
                        {
                            if (value < 0) throw new ArgumentException(nameof(MaxTextWidth));

                            maxTextWidth = value;

                            if (textLayout != null)
                            {
                                textLayout.RequestedSize = GetRequestedSize();
                                InvalidateMetrics();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the maximum height of a text column.
        /// </summary>
        public double MaxTextHeight
        {
            get => maxTextHeight;
            set
            {
                if (maxTextHeight != value)
                {
                    lock (locker)
                    {
                        if (maxTextHeight != value)
                        {
                            if (value < 0) throw new ArgumentException(nameof(MaxTextHeight));

                            maxTextHeight = value;

                            if (textLayout != null)
                            {
                                textLayout.RequestedSize = GetRequestedSize();
                                InvalidateMetrics();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the smallest possible text width that can fully contain the specified text content.
        /// </summary>
        public double MinWidth => GetMinWidth();

        /// <summary>
        /// Gets the distance from the bottom of the last line of text to the bottommost drawn pixel.
        /// </summary>
        public double OverhangAfter => GetOverhangAfter();

        /// <summary>
        /// Gets the maximum distance from the leading alignment point to the leading drawn pixel of a line.
        /// </summary>
        public double OverhangLeading => GetOverhangLeadingAndTrailing().overhangLeading;

        /// <summary>
        /// Gets the maximum distance from the trailing inked pixel to the trailing alignment point of a line.
        /// </summary>
        public double OverhangTrailing => GetOverhangLeadingAndTrailing().overhangTrailing;

        /// <summary>
        /// Gets the string of text to be displayed.
        /// </summary>
        public string Text { get; }

        public bool IsPixelSnappingEnabled { get; }

        public bool IsColorFontEnabled { get; }

        /// <summary>
        /// Gets or sets the alignment of text within a FormattedText object.
        /// </summary>
        public TextAlignment TextAlignment
        {
            get => textAlignment;
            set
            {
                if (textAlignment != value)
                {
                    lock (locker)
                    {
                        if (textAlignment != value)
                        {
                            textAlignment = value;
                            if (textLayout != null)
                            {
                                textLayout.HorizontalAlignment = ConvertToCanvasHorizontalAlignment(textAlignment, flowDirection);
                                InvalidateMetrics();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the means by which the omission of text is indicated.
        /// </summary>
        public TextTrimming TextTrimming
        {
            get => textTrimming;
            set
            {
                if (textTrimming != value)
                {
                    lock (locker)
                    {
                        if (textTrimming != value)
                        {
                            textTrimming = value;
                            if (textLayout != null)
                            {
                                textLayout.TrimmingGranularity = ConvertToTrimmingGranularity(textTrimming);
                                InvalidateMetrics();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets how the FormattedText should wrap text.
        /// </summary>
        public TextWrapping TextWrapping
        {
            get => textWrapping;
            set
            {
                if (textWrapping != value)
                {
                    lock (locker)
                    {
                        if (textWrapping != value)
                        {
                            textWrapping = value;
                            if (textLayout != null)
                            {
                                textLayout.WordWrapping = ConvertToWordWrapping(textWrapping);
                                InvalidateMetrics();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get the glyph runs for each line.
        /// </summary>
        public IReadOnlyList<FormattedTextLineGlyphRuns> LineGlyphRuns => GetLineGlyphRuns();

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                lock (locker)
                {
                    textLayout?.Dispose();
                    textLayout = null;

                    InvalidateMetrics();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
