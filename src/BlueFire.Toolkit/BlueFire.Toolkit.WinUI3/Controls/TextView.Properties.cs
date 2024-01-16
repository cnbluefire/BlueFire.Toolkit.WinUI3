using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Text;

namespace BlueFire.Toolkit.WinUI3.Controls
{
    partial class TextView
    {
        #region Font Properties

        public FontFamily FontFamily
        {
            get { return (FontFamily)GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }
        public double FontSize
        {
            get { return (double)GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }
        public FontWeight FontWeight
        {
            get { return (FontWeight)GetValue(FontWeightProperty); }
            set { SetValue(FontWeightProperty, value); }
        }
        public FontStyle FontStyle
        {
            get { return (FontStyle)GetValue(FontStyleProperty); }
            set { SetValue(FontStyleProperty, value); }
        }

        public FontStretch FontStretch
        {
            get { return (FontStretch)GetValue(FontStretchProperty); }
            set { SetValue(FontStretchProperty, value); }
        }

        public bool IsColorFontEnabled
        {
            get { return (bool)GetValue(IsColorFontEnabledProperty); }
            set { SetValue(IsColorFontEnabledProperty, value); }
        }

        public static readonly DependencyProperty FontFamilyProperty =
            DependencyProperty.Register("FontFamily", typeof(FontFamily), typeof(TextView), new PropertyMetadata(FontFamily.XamlAutoFontFamily, OnFontPropertyChanged));

        public static readonly DependencyProperty FontSizeProperty =
            DependencyProperty.Register("FontSize", typeof(double), typeof(TextView), new PropertyMetadata(14d, OnFontPropertyChanged));

        public static readonly DependencyProperty FontWeightProperty =
            DependencyProperty.Register("FontWeight", typeof(FontWeight), typeof(TextView), new PropertyMetadata(FontWeights.Normal, OnFontPropertyChanged));

        public static readonly DependencyProperty FontStyleProperty =
            DependencyProperty.Register("FontStyle", typeof(FontStyle), typeof(TextView), new PropertyMetadata(FontStyle.Normal, OnFontPropertyChanged));

        public static readonly DependencyProperty FontStretchProperty =
            DependencyProperty.Register("FontStretch", typeof(FontStretch), typeof(TextView), new PropertyMetadata(FontStretch.Normal, OnFontPropertyChanged));

        public static readonly DependencyProperty IsColorFontEnabledProperty =
            DependencyProperty.Register("IsColorFontEnabled", typeof(bool), typeof(TextView), new PropertyMetadata(true, OnFontPropertyChanged));

        private static void OnFontPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            OnFontPropertyChanged(d, e.Property);
        }

        private static void OnFontPropertyChanged(DependencyObject d, DependencyProperty property)
        {
            var sender = (TextView)d;
            sender.MakeDirty();
        }

        #endregion Font Properties


        #region Text Properties



        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public TextAlignment HorizontalTextAlignment
        {
            get { return (TextAlignment)GetValue(HorizontalTextAlignmentProperty); }
            set { SetValue(HorizontalTextAlignmentProperty, value); }
        }

        public bool IsTextTrimmed
        {
            get { return (bool)GetValue(IsTextTrimmedProperty); }
            private set
            {
                isTextTrimmedUpdatingFlag = true;
                try
                {
                    SetValue(IsTextTrimmedProperty, value);
                }
                finally
                {
                    isTextTrimmedUpdatingFlag = false;
                }

            }
        }

        public TextTrimming TextTrimming
        {
            get { return (TextTrimming)GetValue(TextTrimmingProperty); }
            set { SetValue(TextTrimmingProperty, value); }
        }

        public TextWrapping TextWrapping
        {
            get { return (TextWrapping)GetValue(TextWrappingProperty); }
            set { SetValue(TextWrappingProperty, value); }
        }


        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(TextView), new PropertyMetadata(string.Empty, OnTextPropertyChanged));

        public static readonly DependencyProperty HorizontalTextAlignmentProperty =
            DependencyProperty.Register("HorizontalTextAlignment", typeof(TextAlignment), typeof(TextView), new PropertyMetadata(TextAlignment.Start, OnTextPropertyChanged));

        private bool isTextTrimmedUpdatingFlag;
        public static readonly DependencyProperty IsTextTrimmedProperty =
            DependencyProperty.Register("IsTextTrimmed", typeof(bool), typeof(TextView), new PropertyMetadata(false, OnIsTextTrimmedPropertyChanged));

        public static readonly DependencyProperty TextTrimmingProperty =
            DependencyProperty.Register("TextTrimming", typeof(TextTrimming), typeof(TextView), new PropertyMetadata(TextTrimming.None, OnTextPropertyChanged));

        public static readonly DependencyProperty TextWrappingProperty =
            DependencyProperty.Register("TextWrapping", typeof(TextWrapping), typeof(TextView), new PropertyMetadata(TextWrapping.NoWrap, OnTextPropertyChanged));


        private static void OnIsTextTrimmedPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = (TextView)d;

            if (!sender.isTextTrimmedUpdatingFlag)
                throw new InvalidOperationException(nameof(IsTextTrimmed));

            OnTextPropertyChanged(d, e);

            sender.IsTextTrimmedChanged?.Invoke(sender, EventArgs.Empty);
        }

        private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            OnTextPropertyChanged(d, e.Property);
        }

        private static void OnTextPropertyChanged(DependencyObject d, DependencyProperty dp)
        {
            var sender = (TextView)d;
            sender.MakeDirty();
        }

        #endregion Text Properties


        #region Draw Properties

        // Progress == 1 时的前景色
        public SolidColorBrush Foreground
        {
            get { return (SolidColorBrush)GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }

        // Progress != 1 时右侧的前景色，默认为Foreground的颜色，透明度为80%
        public SolidColorBrush SecondaryForeground
        {
            get { return (SolidColorBrush)GetValue(SecondaryForegroundProperty); }
            set { SetValue(SecondaryForegroundProperty, value); }
        }

        public SolidColorBrush Stroke
        {
            get { return (SolidColorBrush)GetValue(StrokeProperty); }
            set { SetValue(StrokeProperty, value); }
        }

        public SolidColorBrush SecondaryStroke
        {
            get { return (SolidColorBrush)GetValue(SecondaryStrokeProperty); }
            set { SetValue(SecondaryStrokeProperty, value); }
        }

        public double StrokeThickness
        {
            get { return (double)GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }


        public static readonly DependencyProperty ForegroundProperty =
            DependencyProperty.Register("Foreground", typeof(SolidColorBrush), typeof(TextView), new PropertyMetadata(null, OnDrawPropertyChanged));

        public static readonly DependencyProperty SecondaryForegroundProperty =
            DependencyProperty.Register("SecondaryForeground", typeof(SolidColorBrush), typeof(TextView), new PropertyMetadata(null, OnDrawPropertyChanged));

        public static readonly DependencyProperty StrokeProperty =
            DependencyProperty.Register("Stroke", typeof(SolidColorBrush), typeof(TextView), new PropertyMetadata(null, OnDrawPropertyChanged));

        public static readonly DependencyProperty SecondaryStrokeProperty =
            DependencyProperty.Register("SecondaryStroke", typeof(SolidColorBrush), typeof(TextView), new PropertyMetadata(null, OnDrawPropertyChanged));

        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register("StrokeThickness", typeof(double), typeof(TextView), new PropertyMetadata(0d, OnDrawPropertyChanged));

        private static void OnDrawPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            OnDrawPropertyChanged(d, e.Property);
        }

        private static void OnDrawPropertyChanged(DependencyObject d, DependencyProperty property)
        {
            var sender = (TextView)d;
            sender.Redraw();
        }



        #endregion Draw Properties


        public event EventHandler? IsTextTrimmedChanged;
    }
}
