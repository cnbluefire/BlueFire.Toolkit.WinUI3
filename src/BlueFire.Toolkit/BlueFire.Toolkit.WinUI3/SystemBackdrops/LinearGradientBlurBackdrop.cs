using BlueFire.Toolkit.WinUI3.Compositions;
using BlueFire.Toolkit.WinUI3.Compositions.LinearGradientBlur;
using BlueFire.Toolkit.WinUI3.Controls;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Color = Windows.UI.Color;

namespace BlueFire.Toolkit.WinUI3.SystemBackdrops
{
    public class LinearGradientBlurBackdrop : TransparentBackdrop
    {
        private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LinearGradientBlurBackdrop sender && !Equals(e.NewValue, e.OldValue))
            {
                foreach (var entry in sender.ControllerEntries.OfType<LinearGradientBlurBackdropBackdropControllerEntry>())
                {
                    if (e.Property == MaxBlurAmountProperty) entry.MaxBlurAmount = (double)e.NewValue;
                    if (e.Property == MinBlurAmountProperty) entry.MinBlurAmount = (double)e.NewValue;
                    if (e.Property == TintColorProperty) entry.TintColor = (Color)e.NewValue;
                    if (e.Property == StartPointProperty) entry.StartPoint = (Point)e.NewValue;
                    if (e.Property == EndPointProperty) entry.EndPoint = (Point)e.NewValue;
                }
            }
        }

        public double MaxBlurAmount
        {
            get { return (double)GetValue(MaxBlurAmountProperty); }
            set { SetValue(MaxBlurAmountProperty, value); }
        }

        public static readonly DependencyProperty MaxBlurAmountProperty =
            DependencyProperty.Register("MaxBlurAmount", typeof(double), typeof(LinearGradientBlurBackdrop), new PropertyMetadata(64d, OnDependencyPropertyChanged));

        public double MinBlurAmount
        {
            get { return (double)GetValue(MinBlurAmountProperty); }
            set { SetValue(MinBlurAmountProperty, value); }
        }

        public static readonly DependencyProperty MinBlurAmountProperty =
            DependencyProperty.Register("MinBlurAmount", typeof(double), typeof(LinearGradientBlurBackdrop), new PropertyMetadata(0.5d, OnDependencyPropertyChanged));


        public Color TintColor
        {
            get { return (Color)GetValue(TintColorProperty); }
            set { SetValue(TintColorProperty, value); }
        }

        public static readonly DependencyProperty TintColorProperty =
            DependencyProperty.Register("TintColor", typeof(Color), typeof(LinearGradientBlurBackdrop), new PropertyMetadata(Color.FromArgb(0, 0, 0, 0), OnDependencyPropertyChanged));

        public Point StartPoint
        {
            get { return (Point)GetValue(StartPointProperty); }
            set { SetValue(StartPointProperty, value); }
        }

        public static readonly DependencyProperty StartPointProperty =
            DependencyProperty.Register("StartPoint", typeof(Point), typeof(LinearGradientBlurBackdrop), new PropertyMetadata(new Point(0, 0), OnDependencyPropertyChanged));

        public Point EndPoint
        {
            get { return (Point)GetValue(EndPointProperty); }
            set { SetValue(EndPointProperty, value); }
        }

        public static readonly DependencyProperty EndPointProperty =
            DependencyProperty.Register("EndPoint", typeof(Point), typeof(LinearGradientBlurBackdrop), new PropertyMetadata(new Point(0, 1), OnDependencyPropertyChanged));



        internal override TransparentBackdropControllerEntry CreateControllerEntry(ICompositionSupportsSystemBackdrop connectedTarget, XamlRoot xamlRoot)
        {
            return new LinearGradientBlurBackdropBackdropControllerEntry(connectedTarget, xamlRoot.ContentIslandEnvironment.AppWindowId)
            {
                MaxBlurAmount = MaxBlurAmount,
                MinBlurAmount = MinBlurAmount,
                TintColor = TintColor,
                StartPoint = StartPoint,
                EndPoint = EndPoint
            };
        }

        private class LinearGradientBlurBackdropBackdropControllerEntry : TransparentBackdropControllerEntry
        {
            private LinearGradientBlurHelperWUC? visualHelper;
            private WindowManager? windowManager;

            private double maxBlurAmount;
            private double minBlurAmount;
            private Color tintColor;
            private Point startPoint;
            private Point endPoint;

            internal LinearGradientBlurBackdropBackdropControllerEntry(ICompositionSupportsSystemBackdrop connectedTarget, WindowId windowId) : base(connectedTarget, windowId)
            {
            }

            public double MaxBlurAmount
            {
                get => maxBlurAmount;
                set
                {
                    if (maxBlurAmount != value)
                    {
                        maxBlurAmount = value;
                        if (visualHelper != null)
                        {
                            visualHelper.MaxBlurAmount = (float)value;
                        }
                    }
                }
            }
            public double MinBlurAmount
            {
                get => minBlurAmount;
                set
                {
                    if (minBlurAmount != value)
                    {
                        minBlurAmount = value;
                        if (visualHelper != null)
                        {
                            visualHelper.MinBlurAmount = (float)value;
                        }
                    }
                }
            }
            public Color TintColor
            {
                get => tintColor;
                set
                {
                    if (tintColor != value)
                    {
                        tintColor = value;
                        if (visualHelper != null)
                        {
                            visualHelper.TintColor = value;
                        }
                    }
                }
            }
            public Point StartPoint
            {
                get => startPoint;
                set
                {
                    if (startPoint != value)
                    {
                        startPoint = value;
                        if (visualHelper != null)
                        {
                            visualHelper.StartPoint = value.ToVector2();
                        }
                    }
                }
            }
            public Point EndPoint
            {
                get => endPoint;
                set
                {
                    if (endPoint != value)
                    {
                        endPoint = value;
                        if (visualHelper != null)
                        {
                            visualHelper.EndPoint = value.ToVector2();
                        }
                    }
                }
            }

            protected override void OnAttached(ICompositionSupportsSystemBackdrop connectedTarget, WindowId windowId)
            {
                base.OnAttached(connectedTarget, windowId);

                visualHelper = new LinearGradientBlurHelperWUC(WindowsCompositionHelper.Compositor)
                {
                    MaxBlurAmount = (float)maxBlurAmount,
                    MinBlurAmount = (float)minBlurAmount,
                    TintColor = tintColor,
                    StartPoint = startPoint.ToVector2(),
                    EndPoint = endPoint.ToVector2(),
                };
                windowManager = WindowManager.Get(windowId);
                if (windowManager != null)
                {
                    windowManager.BackdropVisual.Children.InsertAtTop(visualHelper.RootVisual);
                }
            }

            protected override void OnDetached(ICompositionSupportsSystemBackdrop connectedTarget, WindowId windowId)
            {
                base.OnDetached(connectedTarget, windowId);

                if (visualHelper != null)
                {
                    if (windowManager?.BackdropVisual != null)
                    {
                        windowManager.BackdropVisual.Children.Remove(visualHelper.RootVisual);
                    }

                    visualHelper.Dispose();
                    visualHelper = null;
                }
            }
        }
    }
}
