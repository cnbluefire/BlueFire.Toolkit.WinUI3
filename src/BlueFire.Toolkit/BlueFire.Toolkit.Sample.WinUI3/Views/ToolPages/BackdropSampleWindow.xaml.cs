using BlueFire.Toolkit.WinUI3;
using BlueFire.Toolkit.WinUI3.SystemBackdrops;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BlueFire.Toolkit.Sample.WinUI3.Views.ToolPages
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BackdropSampleWindow : WindowEx
    {
        public BackdropSampleWindow(string backdropType)
        {
            this.InitializeComponent();

            this.backdropType = backdropType;
            materialCardBackdropType = Array.Empty<string>();

            if (backdropType == nameof(ColorBackdrop))
            {
                #region ColorBackdrop Block

                colors = new Color[]
                {
                    Color.FromArgb(127, 0, 120, 212),
                    Color.FromArgb(127, 241, 199, 17),
                    Color.FromArgb(127, 216, 59, 1),
                    Color.FromArgb(127, 34, 34, 34),
                    Color.FromArgb(127, 187, 25, 71),
                    Color.FromArgb(127, 116, 39, 116),
                    Color.FromArgb(127, 0, 129, 114),
                    Color.FromArgb(0, 255, 255, 255),
                };

                this.SystemBackdrop = new ColorBackdrop()
                {
                    BackgroundColor = colors[0]
                };

                #endregion ColorBackdrop Block

                myButton.Content = $"#{colors[colorIndex].A:X2}{colors[colorIndex].R:X2}{colors[colorIndex].G:X2}{colors[colorIndex].B:X2}";
                CustomTitlebar.Visibility = Visibility.Collapsed;
            }
            if (backdropType == nameof(LinearGradientBlurBackdrop))
            {
                #region LinearGradientBlurBackdrop Block

                this.SystemBackdrop = new LinearGradientBlurBackdrop();

                #endregion LinearGradientBlurBackdrop Block

                CustomTitlebar.Visibility = Visibility.Collapsed;
                myButton.Visibility = Visibility.Collapsed;
            }
            else if (backdropType == nameof(MaterialCardBackdrop))
            {
                #region MaterialCardBackdrop Block

                this.SystemBackdrop = new MaterialCardBackdrop()
                {
                    MaterialConfiguration = new MicaBackdropConfiguration(),
                    Margin = new Thickness(20),
                    CornerRadius = 8,
                    BorderColor = Color.FromArgb(127, 0, 120, 212)
                };

                // remove window border
                var windowManager = WindowManager.Get(this.AppWindow);
                windowManager.WindowStyle &=
                    ~(WindowManager.WindowStyleFlags.WS_BORDER
                    | WindowManager.WindowStyleFlags.WS_CAPTION
                    | WindowManager.WindowStyleFlags.WS_THICKFRAME);
                windowManager.RedrawFrame();
                windowManager.WindowMessageReceived += (s, a) =>
                {
                    const uint WM_SYSCOMMAND = 0x0112;
                    const nuint SC_MAXIMIZE = 0xF030;
                    if (a.MessageId == WM_SYSCOMMAND && (a.WParam & 0xFFF0) == SC_MAXIMIZE)
                    {
                        a.LResult = 0;
                        a.Handled = true;
                    }
                };

                #endregion MaterialCardBackdrop Block

                myButton.Content = "Mica";
                materialCardBackdropType = typeof(MaterialCardBackdropConfigurations)
                    .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                    .Select(c => c.GetCustomAttributes<MaterialCardBackdropConfigurationNameAttribute>()?.FirstOrDefault()?.Name)
                    .Where(c => !string.IsNullOrEmpty(c))
                    .Distinct()
                    .ToArray();
                UpdateTitleBarForeground();

                var ncPointerSource = InputNonClientPointerSource.GetForWindowId(AppWindow.Id);

                CustomTitlebar.SizeChanged += (_, _) =>
                {
                    if (CustomTitlebar.ActualWidth > 0 && CustomTitlebar.ActualHeight > 0)
                    {
                        ncPointerSource.SetRegionRects(
                            NonClientRegionKind.Caption,
                            new[]
                            {
                                new RectInt32(
                                    0,
                                    0,
                                    (int)((CustomTitlebar.Margin.Left + CustomTitlebar.ActualWidth - 44) * WindowDpi / 96d),
                                    (int)((CustomTitlebar.Margin.Top + CustomTitlebar.ActualHeight) * WindowDpi / 96d))
                            });
                    }
                };
            }
        }

        private string backdropType;
        private int materialCardBackdropTypeIndex = 0;
        private int colorIndex = 0;
        private string[]? materialCardBackdropType;
        private Color[]? colors;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (backdropType == nameof(ColorBackdrop))
            {
                colorIndex++;
                colorIndex %= colors.Length;
                myButton.Content = colors[colorIndex].A == 0 ?
                    "Transparent" : $"#{colors[colorIndex].A:X2}{colors[colorIndex].R:X2}{colors[colorIndex].G:X2}{colors[colorIndex].B:X2}";
                ((ColorBackdrop)SystemBackdrop).BackgroundColor = colors[colorIndex];
            }
            else if (backdropType == nameof(MaterialCardBackdrop))
            {
                materialCardBackdropTypeIndex++;
                materialCardBackdropTypeIndex %= materialCardBackdropType.Length;
                myButton.Content = materialCardBackdropType[materialCardBackdropTypeIndex];
                ((MaterialCardBackdrop)SystemBackdrop).MaterialConfiguration = MaterialCardBackdropConfiguration.Parse(materialCardBackdropType[materialCardBackdropTypeIndex]);
                UpdateTitleBarForeground();
            }
        }

        private void UpdateTitleBarForeground()
        {
            CloseButton.Foreground = IsLightThemeColor(((MaterialCardBackdrop)SystemBackdrop).MaterialConfiguration switch
            {
                MicaBackdropConfiguration micaConfiguration => micaConfiguration.TintColor,
                AcrylicBackdropConfiguration acrylicConfiguration => acrylicConfiguration.TintColor,
                _ => Color.FromArgb(0, 255, 255, 255)
            }) ? new SolidColorBrush(Color.FromArgb(255, 0, 0, 0)) : new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));

            static bool IsLightThemeColor(Color _color)
            {
                return ((5 * _color.G + 2 * _color.R + _color.B) > 8 * 128);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.AppWindow.Destroy();
        }
    }
}
