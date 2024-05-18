using Microsoft.UI.System;
using Microsoft.UI.Windowing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.Win32;
using SYS_COLOR_INDEX = Windows.Win32.Graphics.Gdi.SYS_COLOR_INDEX;
using AppPolicyWindowingModel = Windows.Win32.Storage.Packaging.Appx.AppPolicyWindowingModel;
using WIN32_ERROR = Windows.Win32.Foundation.WIN32_ERROR;

namespace BlueFire.Toolkit.WinUI3.Resources
{
    internal class AccessibilitySettings
    {
        public AccessibilitySettings()
        {
            var weakThis = new WeakReference<AccessibilitySettings>(this);

            AccessibilitySettingsStatics.Changed += AccessibilitySettingsStatics_Changed;

            void AccessibilitySettingsStatics_Changed(object? sender, EventArgs e)
            {
                if (weakThis.TryGetTarget(out var _this))
                {
                    _this.HighContrastChanged?.Invoke(this, null);
                }
                else
                {
                    AccessibilitySettingsStatics.Changed -= AccessibilitySettingsStatics_Changed;
                }
            }
        }

        public bool HighContrast => AccessibilitySettingsStatics.HighContrast;

        public string HighContrastScheme => AccessibilitySettingsStatics.HighContrastScheme;

        public SystemHighContrastTheme SystemHighContrastTheme => AccessibilitySettingsStatics.GetSystemHighContrastTheme();

        public event TypedEventHandler<AccessibilitySettings, object?>? HighContrastChanged;

        private static class AccessibilitySettingsStatics
        {
            private static bool isUWPApplication = IsUWPApplication;
            private static ThemeSettings? themeSettings;
            private static Windows.UI.ViewManagement.AccessibilitySettings? accessibilitySettings;
            private static AppWindow? messageWindow;
            private static object locker = new object();

            public static bool HighContrast => isUWPApplication ? EnsureAccessibilitySettings().HighContrast : EnsureThemeSettings().HighContrast;

            public static string HighContrastScheme => isUWPApplication ? EnsureAccessibilitySettings().HighContrastScheme : EnsureThemeSettings().HighContrastScheme;

            public static SystemHighContrastTheme GetSystemHighContrastTheme()
            {
                var foreground = PInvoke.GetSysColor(SYS_COLOR_INDEX.COLOR_WINDOWTEXT);
                var background = PInvoke.GetSysColor(SYS_COLOR_INDEX.COLOR_WINDOW);

                if (HighContrast)
                {
                    // Redstone Bug #6417331: Xbox uses video-safe black (0x101010) and white (0xEBEBEB) when in High Contrast
                    if ((0x00FFFFFF == background && 0x0 == foreground) || (0x00EBEBEB == background && 0x00101010 == foreground))
                    {
                        return SystemHighContrastTheme.HighContrastWhite;
                    }
                    else if ((0x0 == background && 0x00FFFFFF == foreground) || (0x00101010 == background && 0x00EBEBEB == foreground))
                    {
                        return SystemHighContrastTheme.HighContrastBlack;
                    }
                    else
                    {
                        return SystemHighContrastTheme.HighContrastCustom;
                    }
                }

                return SystemHighContrastTheme.HighContrastNone;
            }

            public static event EventHandler? Changed;

            private static void AccessibilitySettings_HighContrastChanged(Windows.UI.ViewManagement.AccessibilitySettings sender, object args)
            {
                Changed?.Invoke(null, EventArgs.Empty);
            }

            private static void ThemeSettings_Changed(ThemeSettings sender, object args)
            {
                Changed?.Invoke(null, EventArgs.Empty);
            }

            private static ThemeSettings EnsureThemeSettings()
            {
                if (isUWPApplication) throw new NotSupportedException("Microsoft.UI.System.ThemeSettings");

                if (themeSettings == null)
                {
                    lock (locker)
                    {
                        if (themeSettings == null)
                        {
                            messageWindow = AppWindow.Create();
                            messageWindow.IsShownInSwitchers = false;
                            var presenter = (OverlappedPresenter)messageWindow.Presenter;
                            presenter.IsMaximizable = false;
                            presenter.IsMinimizable = false;
                            presenter.IsResizable = false;
                            presenter.SetBorderAndTitleBar(false, false);
                            messageWindow.MoveAndResize(default);
                            messageWindow.Show();
                            messageWindow.Hide();

                            themeSettings = ThemeSettings.CreateForWindowId(messageWindow.Id);
                            themeSettings.Changed += ThemeSettings_Changed;
                        }
                    }
                }

                return themeSettings;
            }

            private static Windows.UI.ViewManagement.AccessibilitySettings EnsureAccessibilitySettings()
            {
                if (!isUWPApplication) throw new NotSupportedException("Windows.UI.ViewManagement.AccessibilitySettings");

                if (accessibilitySettings == null)
                {
                    lock (locker)
                    {
                        if (accessibilitySettings == null)
                        {
                            accessibilitySettings = new Windows.UI.ViewManagement.AccessibilitySettings();
                            accessibilitySettings.HighContrastChanged += AccessibilitySettings_HighContrastChanged;
                        }
                    }
                }

                return accessibilitySettings;
            }

            private unsafe static bool IsUWPApplication
            {
                get
                {
                    var windowingModel = AppPolicyWindowingModel.AppPolicyWindowingModel_None;
                    if (PInvoke.AppPolicyGetWindowingModel(PInvoke.GetCurrentThreadEffectiveToken(), &windowingModel) == WIN32_ERROR.NO_ERROR)
                    {
                        return windowingModel == AppPolicyWindowingModel.AppPolicyWindowingModel_Universal;
                    }
                    return false;
                }
            }
        }

    }

    internal enum SystemHighContrastTheme
    {
        HighContrastNone = 0x00,
        HighContrast = 0x04,
        HighContrastWhite = 0x08,
        HighContrastBlack = 0x0C,
        HighContrastCustom = 0x10,
    }
}
