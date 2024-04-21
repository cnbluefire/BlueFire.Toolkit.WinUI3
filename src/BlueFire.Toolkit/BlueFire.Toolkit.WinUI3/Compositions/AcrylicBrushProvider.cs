using BlueFire.Toolkit.WinUI3.Media;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Composition;
using Windows.UI;
using System.Numerics;
using Microsoft.UI.Xaml.Media;

namespace BlueFire.Toolkit.WinUI3.Compositions
{
    internal sealed class AcrylicBrushProvider : CompositionBrushProvider
    {
        // https://github.com/microsoft/microsoft-ui-xaml/tree/e30572d2b3516ced7c2c574d5fb9d059a00e8570/dev/Materials/Acrylic

        internal static readonly bool IsDwmHostBackdropBrushSupported = Environment.OSVersion.Version >= new Version(10, 0, 22000, 0);

        private static CompositionSurfaceLoader? noiseImageHolder;

        private double tintOpacity = 0;
        private double? tintLuminosityOpacity = 0.85;
        private Color tintColor = Color.FromArgb(255, 249, 249, 249);
        private double noiseScaleRatio = 1d;
        private double blurAmount = 30f;
        private bool useHostBackdropBrush = IsDwmHostBackdropBrushSupported;

        private CompositionEasingFunction? fallbackTransitionEasing;
        private CompositionScopedBatch? switchTransitionBatch;

        private CompositionSurfaceBrush? noiseBrush;
        private bool disposedValue;

        public double TintOpacity
        {
            get => tintOpacity;
            set => SetProperty(ref tintOpacity, value);
        }

        public double? TintLuminosityOpacity
        {
            get => tintLuminosityOpacity;
            set => SetProperty(ref tintLuminosityOpacity, value);
        }

        public Color TintColor
        {
            get => tintColor;
            set => SetProperty(ref tintColor, value);
        }

        public double NoiseScaleRatio
        {
            get => noiseScaleRatio;
            set
            {
                if (SetProperty(ref noiseScaleRatio, value))
                {
                    noiseScaleRatio = value;
                    noiseBrush!.Scale = new Vector2((float)noiseScaleRatio);
                }
            }
        }

        public double BlurAmount
        {
            get => blurAmount;
            set
            {
                if (SetProperty(ref blurAmount, value))
                {
                    var _blurAmount = UseHostBackdropBrush ? Math.Max(value - 30, 0) : value;

                    Brush.Properties.InsertScalar("GaussianBlurEffect.BlurAmount", (float)_blurAmount);
                }
            }
        }

        public bool UseHostBackdropBrush
        {
            get => useHostBackdropBrush;
            set
            {
                ThrowUseHostBackdropBrushNotSupportedException(value);

                if (SetProperty(ref useHostBackdropBrush, value))
                {
                    UpdateBackdropBrush((CompositionEffectBrush)Brush);
                }
            }
        }

        protected override IReadOnlyList<string> AnimatableProperties => new[]
        {
            "FixHostBackdropLayer.Opacity",
            "LuminosityColorEffect.Color",
            "TintColorEffect.Color",
            "TintColorEffectWithoutAlpha.Color",
            "TintColorOpacityEffect.Opacity",
            "TintColorWithoutAlphaOpacityEffect.Opacity",
            "GaussianBlurEffect.BlurAmount",
        };

        protected override void OnPropertyChanged([CallerMemberName] string propName = "")
        {
            switch (propName)
            {
                case nameof(TintOpacity):
                case nameof(TintLuminosityOpacity):
                case nameof(TintColor):
                    {
                        UpdateTintState();
                    }
                    break;
            }

            base.OnPropertyChanged(propName);
        }

        private void UpdateTintState()
        {
            var luminosityColor = ColorConversion.GetEffectiveLuminosityColor(TintColor, TintOpacity, TintLuminosityOpacity);
            var tintColor = ColorConversion.GetEffectiveTintColor(TintColor, TintOpacity, TintLuminosityOpacity);

            Brush.Properties.InsertColor("LuminosityColorEffect.Color", luminosityColor);
            Brush.Properties.InsertColor("TintColorEffectWithoutAlpha.Color", tintColor);
            Brush.Properties.InsertColor("TintColorEffect.Color", tintColor);

            Brush.Properties.InsertScalar("TintColorWithoutAlphaOpacityEffect.Opacity", tintColor.A == 255 ? 1f : 0f);
            Brush.Properties.InsertScalar("TintColorOpacityEffect.Opacity", tintColor.A == 255 ? 0f : 1f);
        }

        protected override CompositionBrush CreateBrush(IEnumerable<string> animatableProperties)
        {
            var effect = TraceDisposable(CreateEffect());

            noiseBrush = TraceDisposable(Compositor.CreateSurfaceBrush());
            noiseBrush.Stretch = CompositionStretch.None;

            if (Compositor == WindowsCompositionHelper.Compositor)
            {
                noiseBrush.Surface = EnsureNoiseImageSurface();
            }

            var factory = TraceDisposable(Compositor.CreateEffectFactory(effect, animatableProperties));

            var brush = TraceDisposable(factory.CreateBrush());

            UpdateBackdropBrush(brush);

            brush.SetSourceParameter("noise", noiseBrush);

            return brush;
        }

        private void UpdateBackdropBrush(CompositionEffectBrush effectBrush)
        {
            CompositionBrush backdropBrush;
            var blurAmount = this.blurAmount;

            if (UseHostBackdropBrush)
            {
                blurAmount = Math.Max(this.blurAmount - 30, 0);
                backdropBrush = TraceDisposable(Compositor.CreateHostBackdropBrush());
            }
            else
            {
                backdropBrush = TraceDisposable(Compositor.CreateBackdropBrush());
            }

            effectBrush.SetSourceParameter("source", backdropBrush);
            effectBrush.Properties.InsertScalar("GaussianBlurEffect.BlurAmount", (float)blurAmount);
            effectBrush.Properties.InsertScalar("FixHostBackdropLayer.Opacity", UseHostBackdropBrush ? 1 : 0);
        }

        protected override ICanvasEffect CreateEffectCore()
        {
            var luminosityColor = ColorConversion.GetEffectiveLuminosityColor(this.tintColor, this.tintOpacity, this.tintLuminosityOpacity);
            var tintColor = ColorConversion.GetEffectiveTintColor(this.tintColor, this.tintOpacity, this.tintLuminosityOpacity);

            var effect = TraceDisposable(new CompositeEffect()
            {
                Mode = CanvasComposite.SourceOver,
                Sources =
                {
                    TraceDisposable(new OpacityEffect()
                    {
                        Name = "FixHostBackdropLayer",
                        Opacity = 0,
                        Source = TraceDisposable(new ColorSourceEffect()
                        {
                            Color = Color.FromArgb(255, 0, 0, 0)
                        }),
                    }),
                    TraceDisposable(new OpacityEffect()
                    {
                        Name = "TintColorOpacityEffect",
                        Opacity = tintColor.A == 255 ? 0f : 1f,
                        Source = TraceDisposable(new BlendEffect()
                        {
                            Mode = BlendEffectMode.Luminosity,
                            Foreground = TraceDisposable(new ColorSourceEffect()
                            {
                                Name = "TintColorEffect",
                                Color = tintColor
                            }),
                            Background = TraceDisposable(new BlendEffect
                            {
                                Mode = BlendEffectMode.Color,
                                Foreground = TraceDisposable(new ColorSourceEffect
                                {
                                    Name = "LuminosityColorEffect",
                                    Color = luminosityColor,
                                }),
                                Background = TraceDisposable(new GaussianBlurEffect()
                                {
                                    Name = "GaussianBlurEffect",
                                    BlurAmount = 0,
                                    Source = new CompositionEffectSourceParameter("source"),
                                    BorderMode = EffectBorderMode.Hard
                                }),
                            }),
                        }),
                    }),
                    TraceDisposable(new OpacityEffect()
                    {
                        Name = "TintColorWithoutAlphaOpacityEffect",
                        Opacity = tintColor.A == 255 ? 1f : 0f,
                        Source = TraceDisposable(new ColorSourceEffect()
                        {
                            Name = "TintColorEffectWithoutAlpha",
                            Color = tintColor
                        }),
                    }),
                    TraceDisposable(new OpacityEffect()
                    {
                        Opacity = 0.02f,
                        Source = TraceDisposable(new BorderEffect()
                        {
                            Source = new CompositionEffectSourceParameter("noise"),
                            ExtendX = CanvasEdgeBehavior.Wrap,
                            ExtendY = CanvasEdgeBehavior.Wrap,
                        }),
                    }),
                }
            });

            return effect;
        }

        #region Create Brush

        private static ICompositionSurface EnsureNoiseImageSurface()
        {
            if (noiseImageHolder == null)
            {
                noiseImageHolder = CompositionSurfaceLoader.StartLoadFromUri(new Uri("ms-resource://BlueFire.Toolkit.WinUI3/Files/BlueFire.Toolkit.WinUI3/Assets/NoiseAsset_256x256_PNG.png"));
            }
            return noiseImageHolder.Surface;
        }


        #endregion Create Brush


        internal static void ThrowUseHostBackdropBrushNotSupportedException(bool useHostBackdropBrush)
        {
            if (useHostBackdropBrush && !IsDwmHostBackdropBrushSupported)
            {
                throw new NotSupportedException("UseHostBackdropBrush is not supported on the current system version. It requires at least version 10.0.22000.0 or higher of the operating system.");
            }
        }

    }
}
