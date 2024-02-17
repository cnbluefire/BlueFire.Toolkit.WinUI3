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

        public double Baseline => TryGetFirstLineMetrics()?.Baseline ?? 0;

        public double Extent => GetDrawBounds().Height;

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

        public double Width => EnsureTextLayout().LayoutBounds.Right;

        public double Height => EnsureTextLayout().LayoutBounds.Bottom;

        public double WidthIncludingTrailingWhitespace => EnsureTextLayout().LayoutBoundsIncludingTrailingWhitespace.Right;

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

        public double MinWidth => GetMinWidth();

        public double OverhangAfter => GetOverhangAfter();

        public double OverhangLeading => GetOverhangLeadingAndTrailing().overhangLeading;

        public double OverhangTrailing => GetOverhangLeadingAndTrailing().overhangTrailing;

        public string Text { get; }

        public bool IsPixelSnappingEnabled { get; }

        public bool IsColorFontEnabled { get; }

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
