using BlueFire.Toolkit.WinUI3.Media;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Composition;
using System.Numerics;
using Microsoft.UI.Xaml;

namespace BlueFire.Toolkit.WinUI3.Compositions
{
    internal sealed class MicaBrushProvider : CompositionBrushProvider
    {
        // https://github.com/microsoft/microsoft-ui-xaml/tree/e30572d2b3516ced7c2c574d5fb9d059a00e8570/dev/Materials/Backdrop
        // https://github.com/wherewhere/Mica-For-UWP

        private static readonly bool IsBlurredWallpaperBackdropBrushSupported = Environment.OSVersion.Version >= new Version(10, 0, 22000, 0);

        private double tintOpacity = 0.8d;
        private double luminosityOpacity = 1;
        private Color tintColor = Color.FromArgb(255, 32, 32, 32);

        internal MicaBrushProvider()
        {
            ForceUseFallback = !IsBlurredWallpaperBackdropBrushSupported;
        }

        public double TintOpacity
        {
            get => tintOpacity;
            set => SetProperty(ref tintOpacity, value);
        }

        public double LuminosityOpacity
        {
            get => luminosityOpacity;
            set => SetProperty(ref luminosityOpacity, value);
        }

        public Color TintColor
        {
            get => tintColor;
            set => SetProperty(ref tintColor, value);
        }

        protected override IReadOnlyList<string> AnimatableProperties => new[]
        {
            "TintColor.Color",
            "TintOpacity.Opacity",
            "LuminosityColor.Color",
            "LuminosityOpacity.Opacity",
        };

        protected override void OnPropertyChanged([CallerMemberName] string propName = "")
        {
            switch (propName)
            {
                case nameof(TintOpacity):
                case nameof(LuminosityOpacity):
                case nameof(TintColor):
                    {
                        UpdateTintState();
                    }
                    break;
            }

            base.OnPropertyChanged(propName);
        }

        protected override CompositionBrush CreateBrush(IEnumerable<string> animatableProperties)
        {
            var effect = TraceDisposable(CreateEffect());

            var factory = TraceDisposable(Compositor.CreateEffectFactory(effect, animatableProperties));

            var brush = TraceDisposable(factory.CreateBrush());
            if (IsBlurredWallpaperBackdropBrushSupported)
            {
                var backdropBrush = TraceDisposable(Compositor.TryCreateBlurredWallpaperBackdropBrush());
                brush.SetSourceParameter("source", backdropBrush);
            }
            else
            {
                var colorBrush = TraceDisposable(Compositor.CreateColorBrush(Color.FromArgb(255, 0, 0, 0)));
                brush.SetSourceParameter("source", colorBrush);
            }
            return brush;
        }

        protected override ICanvasEffect CreateEffectCore()
        {
            ColorSourceEffect tintColorEffect = TraceDisposable(new ColorSourceEffect()
            {
                Color = Color.FromArgb(255, 32, 32, 32),
                Name = "TintColor"
            });

            OpacityEffect tintOpacityEffect = TraceDisposable(new OpacityEffect()
            {
                Name = "TintOpacity",
                Source = tintColorEffect,
                Opacity = 0.8f
            });

            ColorSourceEffect luminosityColorEffect = TraceDisposable(new ColorSourceEffect()
            {
                Color = Color.FromArgb(255, 32, 32, 32),
                Name = "LuminosityColor"
            });

            OpacityEffect luminosityOpacityEffect = TraceDisposable(new OpacityEffect()
            {
                Name = "LuminosityOpacity",
                Source = luminosityColorEffect,
                Opacity = 1f
            });

            BlendEffect luminosityBlendEffect = TraceDisposable(new BlendEffect()
            {
                Mode = BlendEffectMode.Color,
                Background = new CompositionEffectSourceParameter("source"),
                Foreground = luminosityOpacityEffect,
            });

            BlendEffect colorBlendEffect = TraceDisposable(new BlendEffect()
            {
                Mode = BlendEffectMode.Luminosity,
                Background = luminosityBlendEffect,
                Foreground = tintOpacityEffect,
            });


            return colorBlendEffect;
        }

        private void UpdateTintState()
        {
            if (Brush is not null and not CompositionColorBrush)
            {
                Brush.Properties.InsertColor("LuminosityColor.Color", TintColor);
                Brush.Properties.InsertScalar("LuminosityOpacity.Opacity", (float)LuminosityOpacity);

                Brush.Properties.InsertColor("TintColor.Color", TintColor);
                Brush.Properties.InsertScalar("TintOpacity.Opacity", (float)TintOpacity);
            }
        }
    }
}