using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace BlueFire.Toolkit.Sample.WinUI3.Utils
{
    internal static class TitleBarHelper
    {
        public static void UpdateTitleBarTheme(AppWindowTitleBar titleBar, ElementTheme actualTheme)
        {
            var (WindowCaptionBackground,
                WindowCaptionBackgroundDisabled,
                WindowCaptionForeground,
                WindowCaptionForegroundDisabled,
                WindowCaptionButtonBackgroundPointerOver,
                WindowCaptionButtonBackgroundPressed) = actualTheme switch
                {
                    ElementTheme.Light => (
                        Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF),
                        Color.FromArgb(0xFF, 0xF3, 0xF3, 0xF3),
                        Color.FromArgb(0xFF, 0x00, 0x00, 0x00),
                        Color.FromArgb(0x66, 0x00, 0x00, 0x00),
                        Color.FromArgb(0xFF, 0xE9, 0xE9, 0xE9),
                        Color.FromArgb(0x66, 0xF0, 0xF0, 0xF0)),
                    _ => (
                        Color.FromArgb(0xFF, 0x00, 0x00, 0x00),
                        Color.FromArgb(0xFF, 0x02, 0x02, 0x02),
                        Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF),
                        Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF),
                        Color.FromArgb(0x33, 0x16, 0x16, 0x16),
                        Color.FromArgb(0x66, 0x0F, 0x0F, 0x0F)),
                };

            titleBar.BackgroundColor =
                titleBar.ButtonBackgroundColor = WindowCaptionBackground;
            titleBar.InactiveBackgroundColor =
                titleBar.ButtonInactiveBackgroundColor = WindowCaptionBackgroundDisabled;

            titleBar.ButtonHoverBackgroundColor = WindowCaptionButtonBackgroundPointerOver;
            titleBar.ButtonPressedBackgroundColor = WindowCaptionButtonBackgroundPressed;

            titleBar.ForegroundColor =
                titleBar.ButtonForegroundColor =
                titleBar.ButtonHoverForegroundColor =
                titleBar.ButtonPressedForegroundColor = WindowCaptionForeground;

            titleBar.InactiveForegroundColor =
                titleBar.ButtonInactiveForegroundColor =
                WindowCaptionForegroundDisabled;
        }
    }
}
