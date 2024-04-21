using BlueFire.Toolkit.WinUI3.Compositions;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Color = Windows.UI.Color;

namespace BlueFire.Toolkit.WinUI3.SystemBackdrops
{
    [CreateFromString(MethodName = "BlueFire.Toolkit.WinUI3.SystemBackdrops.MaterialCardBackdropConfiguration.Parse")]
    public abstract class MaterialCardBackdropConfiguration
    {
        private SystemBackdropTheme theme = SystemBackdropTheme.Default;
        private bool alwaysUseFallback = false;
        private Color fallbackColor = Color.FromArgb(255, 0, 0, 0);

        public MaterialCardBackdropConfiguration()
        {
            SetTheme(theme);
        }

        public bool AlwaysUseFallback
        {
            get => alwaysUseFallback;
            set => SetProperty(ref alwaysUseFallback, value);
        }

        public Color FallbackColor
        {
            get => fallbackColor;
            set => SetProperty(ref fallbackColor, value);
        }

        internal SystemBackdropTheme Theme => theme;

        public void SetTheme(SystemBackdropTheme theme)
        {
            this.theme = theme;

            var _theme = theme;
            if (_theme == SystemBackdropTheme.Default)
            {
                _theme = Application.Current.RequestedTheme switch
                {
                    ApplicationTheme.Light => SystemBackdropTheme.Light,
                    _ => SystemBackdropTheme.Dark,
                };
            }

            SetThemeCore(_theme);
        }

        protected virtual void SetThemeCore(SystemBackdropTheme theme)
        {
            if (theme == SystemBackdropTheme.Light)
            {
                FallbackColor = Color.FromArgb(255, 255, 255, 255);
            }
            else
            {
                FallbackColor = Color.FromArgb(255, 0, 0, 0);
            }
        }

        internal bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propName = "")
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                OnPropertyChanged(propName);
                return true;
            }
            return false;
        }

        internal void OnPropertyChanged([CallerMemberName] string propName = "")
        {
            PropertyChanged?.Invoke(this, propName);
        }

        internal event EventHandler<string?>? PropertyChanged;

        #region Preset Configurations

        /// <summary>
        /// Create MaterialCardBackdropConfiguration by name.
        /// </summary>
        /// <param name="text">
        /// Support text is defined in 
        /// <see cref="BlueFire.Toolkit.WinUI3.SystemBackdrops.MaterialCardBackdropConfigurations"/>.
        /// </param>
        /// <param name="configuration"></param>
        public static bool TryParse(string? text, [NotNullWhen(true)] out MaterialCardBackdropConfiguration? configuration)
        {
            configuration = text switch
            {
                nameof(MaterialCardBackdropConfigurations.Mica) or nameof(MaterialCardBackdropConfigurations.MicaBase) or nameof(MaterialCardBackdropConfigurations.DefaultMica) or nameof(MaterialCardBackdropConfigurations.DefaultMicaBase) => CreateMica(SystemBackdropTheme.Default, MicaKind.Base),
                nameof(MaterialCardBackdropConfigurations.MicaAlt) or nameof(MaterialCardBackdropConfigurations.DefaultMicaAlt) => CreateMica(SystemBackdropTheme.Default, MicaKind.BaseAlt),

                nameof(MaterialCardBackdropConfigurations.LightMica) or nameof(MaterialCardBackdropConfigurations.LightMicaBase) => CreateMica(SystemBackdropTheme.Light, MicaKind.Base),
                nameof(MaterialCardBackdropConfigurations.LightMicaAlt) => CreateMica(SystemBackdropTheme.Light, MicaKind.BaseAlt),

                nameof(MaterialCardBackdropConfigurations.DarkMica) or nameof(MaterialCardBackdropConfigurations.DarkMicaBase) => CreateMica(SystemBackdropTheme.Dark, MicaKind.Base),
                nameof(MaterialCardBackdropConfigurations.DarkMicaAlt) => CreateMica(SystemBackdropTheme.Dark, MicaKind.BaseAlt),

                nameof(MaterialCardBackdropConfigurations.Acrylic) or nameof(MaterialCardBackdropConfigurations.DefaultAcrylic) or nameof(MaterialCardBackdropConfigurations.AcrylicDefault) or nameof(MaterialCardBackdropConfigurations.DefaultAcrylicDefault) => CreateAcrylic(SystemBackdropTheme.Default, DesktopAcrylicKind.Default),
                nameof(MaterialCardBackdropConfigurations.AcrylicBase) or nameof(MaterialCardBackdropConfigurations.DefaultAcrylicBase) => CreateAcrylic(SystemBackdropTheme.Default, DesktopAcrylicKind.Base),
                nameof(MaterialCardBackdropConfigurations.AcrylicThin) or nameof(MaterialCardBackdropConfigurations.DefaultAcrylicThin) => CreateAcrylic(SystemBackdropTheme.Default, DesktopAcrylicKind.Thin),

                nameof(MaterialCardBackdropConfigurations.LightAcrylic) or nameof(MaterialCardBackdropConfigurations.LightAcrylicDefault) => CreateAcrylic(SystemBackdropTheme.Light, DesktopAcrylicKind.Default),
                nameof(MaterialCardBackdropConfigurations.LightAcrylicBase) => CreateAcrylic(SystemBackdropTheme.Light, DesktopAcrylicKind.Base),
                nameof(MaterialCardBackdropConfigurations.LightAcrylicThin) => CreateAcrylic(SystemBackdropTheme.Light, DesktopAcrylicKind.Thin),

                nameof(MaterialCardBackdropConfigurations.DarkAcrylic) or nameof(MaterialCardBackdropConfigurations.DarkAcrylicDefault) => CreateAcrylic(SystemBackdropTheme.Dark, DesktopAcrylicKind.Default),
                nameof(MaterialCardBackdropConfigurations.DarkAcrylicBase) => CreateAcrylic(SystemBackdropTheme.Dark, DesktopAcrylicKind.Base),
                nameof(MaterialCardBackdropConfigurations.DarkAcrylicThin) => CreateAcrylic(SystemBackdropTheme.Dark, DesktopAcrylicKind.Thin),

                _ => null
            };

            return configuration != null;
        }

        /// <inheritdoc cref="BlueFire.Toolkit.WinUI3.SystemBackdrops.MaterialCardBackdropConfiguration.TryParse(string?, out MaterialCardBackdropConfiguration?)"/>
        /// <exception cref="FormatException"></exception>
        public static MaterialCardBackdropConfiguration Parse(string? text)
        {
            if (TryParse(text, out MaterialCardBackdropConfiguration? configuration))
            {
                return configuration;
            }
            throw new FormatException($"String '{text}' was not recognized as a valid MaterialCardBackdropConfiguration.");
        }

        private static MaterialCardBackdropConfiguration CreateMica(SystemBackdropTheme theme, MicaKind kind)
        {
            var configuration = new MicaBackdropConfiguration()
            {
                theme = theme
            };
            configuration.SetKind(kind);
            return configuration;
        }

        private static MaterialCardBackdropConfiguration CreateAcrylic(SystemBackdropTheme theme, DesktopAcrylicKind kind)
        {
            var configuration = new AcrylicBackdropConfiguration()
            {
                theme = theme
            };
            configuration.SetKind(kind);
            return configuration;
        }


        #endregion Preset Configurations
    }

    /// <summary>
    /// All 
    /// <see cref="BlueFire.Toolkit.WinUI3.SystemBackdrops.MaterialCardBackdropConfiguration.Parse(string?)"/>
    /// supported names.
    /// </summary>
    public static class MaterialCardBackdropConfigurations
    {
        [MaterialCardBackdropConfigurationName(Name = nameof(Mica))]
        public static MaterialCardBackdropConfiguration Mica => Create(nameof(Mica));

        [MaterialCardBackdropConfigurationName(Name = nameof(Mica))]
        public static MaterialCardBackdropConfiguration MicaBase => Create(nameof(MicaBase));

        [MaterialCardBackdropConfigurationName(Name = nameof(Mica))]
        public static MaterialCardBackdropConfiguration DefaultMica => Create(nameof(DefaultMica));

        [MaterialCardBackdropConfigurationName(Name = nameof(Mica))]
        public static MaterialCardBackdropConfiguration DefaultMicaBase => Create(nameof(DefaultMicaBase));

        [MaterialCardBackdropConfigurationName(Name = nameof(MicaAlt))]
        public static MaterialCardBackdropConfiguration MicaAlt => Create(nameof(MicaAlt));

        [MaterialCardBackdropConfigurationName(Name = nameof(MicaAlt))]
        public static MaterialCardBackdropConfiguration DefaultMicaAlt => Create(nameof(DefaultMicaAlt));

        [MaterialCardBackdropConfigurationName(Name = nameof(LightMica))]
        public static MaterialCardBackdropConfiguration LightMica => Create(nameof(LightMica));

        [MaterialCardBackdropConfigurationName(Name = nameof(LightMica))]
        public static MaterialCardBackdropConfiguration LightMicaBase => Create(nameof(LightMicaBase));

        [MaterialCardBackdropConfigurationName(Name = nameof(LightMicaAlt))]
        public static MaterialCardBackdropConfiguration LightMicaAlt => Create(nameof(LightMicaAlt));

        [MaterialCardBackdropConfigurationName(Name = nameof(DarkMica))]
        public static MaterialCardBackdropConfiguration DarkMica => Create(nameof(DarkMica));

        [MaterialCardBackdropConfigurationName(Name = nameof(DarkMica))]
        public static MaterialCardBackdropConfiguration DarkMicaBase => Create(nameof(DarkMicaBase));

        [MaterialCardBackdropConfigurationName(Name = nameof(DarkMicaAlt))]
        public static MaterialCardBackdropConfiguration DarkMicaAlt => Create(nameof(DarkMicaAlt));

        [MaterialCardBackdropConfigurationName(Name = nameof(Acrylic))]
        public static MaterialCardBackdropConfiguration Acrylic => Create(nameof(Acrylic));

        [MaterialCardBackdropConfigurationName(Name = nameof(Acrylic))]
        public static MaterialCardBackdropConfiguration DefaultAcrylic => Create(nameof(DefaultAcrylic));

        [MaterialCardBackdropConfigurationName(Name = nameof(Acrylic))]
        public static MaterialCardBackdropConfiguration AcrylicDefault => Create(nameof(AcrylicDefault));

        [MaterialCardBackdropConfigurationName(Name = nameof(Acrylic))]
        public static MaterialCardBackdropConfiguration DefaultAcrylicDefault => Create(nameof(DefaultAcrylicDefault));

        [MaterialCardBackdropConfigurationName(Name = nameof(AcrylicBase))]
        public static MaterialCardBackdropConfiguration AcrylicBase => Create(nameof(AcrylicBase));

        [MaterialCardBackdropConfigurationName(Name = nameof(AcrylicBase))]
        public static MaterialCardBackdropConfiguration DefaultAcrylicBase => Create(nameof(DefaultAcrylicBase));

        [MaterialCardBackdropConfigurationName(Name = nameof(AcrylicThin))]
        public static MaterialCardBackdropConfiguration AcrylicThin => Create(nameof(AcrylicThin));

        [MaterialCardBackdropConfigurationName(Name = nameof(AcrylicThin))]
        public static MaterialCardBackdropConfiguration DefaultAcrylicThin => Create(nameof(DefaultAcrylicThin));

        [MaterialCardBackdropConfigurationName(Name = nameof(LightAcrylic))]
        public static MaterialCardBackdropConfiguration LightAcrylic => Create(nameof(LightAcrylic));

        [MaterialCardBackdropConfigurationName(Name = nameof(LightAcrylic))]
        public static MaterialCardBackdropConfiguration LightAcrylicDefault => Create(nameof(LightAcrylicDefault));

        [MaterialCardBackdropConfigurationName(Name = nameof(LightAcrylicBase))]
        public static MaterialCardBackdropConfiguration LightAcrylicBase => Create(nameof(LightAcrylicBase));

        [MaterialCardBackdropConfigurationName(Name = nameof(LightAcrylicThin))]
        public static MaterialCardBackdropConfiguration LightAcrylicThin => Create(nameof(LightAcrylicThin));

        [MaterialCardBackdropConfigurationName(Name = nameof(DarkAcrylic))]
        public static MaterialCardBackdropConfiguration DarkAcrylic => Create(nameof(DarkAcrylic));

        [MaterialCardBackdropConfigurationName(Name = nameof(DarkAcrylic))]
        public static MaterialCardBackdropConfiguration DarkAcrylicDefault => Create(nameof(DarkAcrylicDefault));

        [MaterialCardBackdropConfigurationName(Name = nameof(DarkAcrylicBase))]
        public static MaterialCardBackdropConfiguration DarkAcrylicBase => Create(nameof(DarkAcrylicBase));

        [MaterialCardBackdropConfigurationName(Name = nameof(DarkAcrylicThin))]
        public static MaterialCardBackdropConfiguration DarkAcrylicThin => Create(nameof(DarkAcrylicThin));


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static MaterialCardBackdropConfiguration Create(string name)
        {
            MaterialCardBackdropConfiguration.TryParse(name, out var configuration);
            return configuration!;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class MaterialCardBackdropConfigurationNameAttribute : Attribute
    {
        public string? Name { get; set; }
    }

    public sealed class AcrylicBackdropConfiguration : MaterialCardBackdropConfiguration
    {
        private double tintOpacity;
        private double? tintLuminosityOpacity;
        private Color tintColor;
        private DesktopAcrylicKind desktopAcrylicKind = DesktopAcrylicKind.Default;
        private double blurAmount;
        private bool useHostBackdropBrush = AcrylicBrushProvider.IsDwmHostBackdropBrushSupported;

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

        public double BlurAmount
        {
            get => blurAmount;
            set => SetProperty(ref blurAmount, value);
        }

        public bool UseHostBackdropBrush
        {
            get => useHostBackdropBrush;
            set
            {
                AcrylicBrushProvider.ThrowUseHostBackdropBrushNotSupportedException(value);
                SetProperty(ref useHostBackdropBrush, value);
            }
        }

        public void SetKind(DesktopAcrylicKind desktopAcrylicKind)
        {
            this.desktopAcrylicKind = desktopAcrylicKind;
            SetTheme(Theme);
        }

        protected override void SetThemeCore(SystemBackdropTheme theme)
        {
            BlurAmount = 30;

            if (desktopAcrylicKind == DesktopAcrylicKind.Default)
            {
                if (theme == SystemBackdropTheme.Light)
                {
                    FallbackColor = Color.FromArgb(255, 249, 249, 249);
                    TintColor = Color.FromArgb(255, 252, 252, 252);
                    TintOpacity = 0;
                    TintLuminosityOpacity = 0.85;
                }
                else
                {
                    FallbackColor = Color.FromArgb(255, 44, 44, 44);
                    TintColor = Color.FromArgb(255, 44, 44, 44);
                    TintOpacity = 0.15;
                    TintLuminosityOpacity = 0.96;
                }
            }
            else if (desktopAcrylicKind == DesktopAcrylicKind.Thin)
            {
                if (theme == SystemBackdropTheme.Light)
                {
                    FallbackColor = Color.FromArgb(255, 211, 211, 211);
                    TintColor = Color.FromArgb(255, 211, 211, 211);
                    TintOpacity = 0;
                    TintLuminosityOpacity = 0.44;
                }
                else
                {
                    FallbackColor = Color.FromArgb(255, 84, 84, 84);
                    TintColor = Color.FromArgb(255, 84, 84, 84);
                    TintOpacity = 0;
                    TintLuminosityOpacity = 0.64;
                }
            }
            else
            {
                if (theme == SystemBackdropTheme.Light)
                {
                    FallbackColor = Color.FromArgb(255, 238, 238, 238);
                    TintColor = Color.FromArgb(255, 243, 243, 243);
                    TintOpacity = 0;
                    TintLuminosityOpacity = 0.9;
                }
                else
                {
                    FallbackColor = Color.FromArgb(255, 28, 28, 28);
                    TintColor = Color.FromArgb(255, 32, 32, 32);
                    TintOpacity = 0.5;
                    TintLuminosityOpacity = 0.96;
                }
            }
        }
    }

    public sealed class MicaBackdropConfiguration : MaterialCardBackdropConfiguration
    {
        private double tintOpacity;
        private double luminosityOpacity;
        private Color tintColor;
        private MicaKind micaKind;

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

        public void SetKind(MicaKind micaKind)
        {
            this.micaKind = micaKind;
            SetTheme(Theme);
        }

        protected override void SetThemeCore(SystemBackdropTheme theme)
        {
            if (micaKind == MicaKind.Base)
            {
                if (theme == SystemBackdropTheme.Light)
                {
                    FallbackColor = Color.FromArgb(255, 243, 243, 243);
                    TintColor = Color.FromArgb(255, 243, 243, 243);
                    TintOpacity = 0.5;
                    LuminosityOpacity = 1;
                }
                else
                {
                    FallbackColor = Color.FromArgb(255, 32, 32, 32);
                    TintColor = Color.FromArgb(255, 32, 32, 32);
                    TintOpacity = 0.8;
                    LuminosityOpacity = 1;
                }
            }
            else
            {
                if (theme == SystemBackdropTheme.Light)
                {
                    FallbackColor = Color.FromArgb(255, 232, 232, 232);
                    TintColor = Color.FromArgb(255, 218, 218, 218);
                    TintOpacity = 0.5;
                    LuminosityOpacity = 1;
                }
                else
                {
                    FallbackColor = Color.FromArgb(255, 32, 32, 32);
                    TintColor = Color.FromArgb(255, 10, 10, 10);
                    TintOpacity = 0;
                    LuminosityOpacity = 1;
                }
            }
        }
    }

}
