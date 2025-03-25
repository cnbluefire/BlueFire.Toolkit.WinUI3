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
using BlueFire.Toolkit.WinUI3.Core.Dispatching;
using BlueFire.Toolkit.WinUI3.Extensions;
using System.Diagnostics.CodeAnalysis;
using BlueFire.Toolkit.Extensions;

namespace BlueFire.Toolkit.WinUI3.Resources
{
    internal class SystemSettings
    {
        private string homeRegion;
        private bool highContrast;
        private string highContrastScheme;
        private SystemHighContrastTheme systemHighContrastTheme;

        public SystemSettings()
        {
            var weakThis = new WeakReference<SystemSettings>(this);

            var weakListener = new WeakEventListener<SystemSettings, object?, SystemEvents.UserPreferenceChangedEventArgs>(
                this,
                (i, s, a) => OnUserPreferenceChanged(s, a),
                w => SystemEvents.UserPreferenceChanged -= w.OnEvent);

            SystemEvents.UserPreferenceChanged += weakListener.OnEvent;

            UpdateHighContrast();
            UpdateHomeRegion();
        }

        public string HomeRegion => homeRegion;

        public bool HighContrast => highContrast;

        public string HighContrastScheme => highContrastScheme;

        public SystemHighContrastTheme SystemHighContrastTheme => systemHighContrastTheme;


        public event EventHandler? HighContrastChanged;

        public event EventHandler? HomeRegionChanged;

        private void OnUserPreferenceChanged(object? sender, SystemEvents.UserPreferenceChangedEventArgs e)
        {
            if (e.Category == SystemEvents.UserPreferenceCategory.Accessibility)
            {
                if (e.GetSystemParametersInfoAction() == Windows.Win32.UI.WindowsAndMessaging.SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETHIGHCONTRAST)
                {
                    UpdateHighContrast();
                    HighContrastChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            else if (e.Category == SystemEvents.UserPreferenceCategory.Locale)
            {
                UpdateHomeRegion();
                HomeRegionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        [MemberNotNull(nameof(homeRegion))]
        private void UpdateHomeRegion()
        {
            var region = new Windows.Globalization.GeographicRegion();

            var qualifierValue = region.CodeTwoLetter;
            if (qualifierValue == "ZZ")
            {
                qualifierValue = region.CodeThreeDigit;
                if (qualifierValue == "999")
                {
                    qualifierValue = "001";
                }
            }
            homeRegion = qualifierValue;
        }

        [MemberNotNull(nameof(highContrastScheme))]
        private unsafe void UpdateHighContrast()
        {
            HIGHCONTRASTW value = default;
            value.cbSize = (uint)sizeof(HIGHCONTRASTW);

            if (!Windows.Win32.PInvoke.SystemParametersInfo(
                Windows.Win32.UI.WindowsAndMessaging.SYSTEM_PARAMETERS_INFO_ACTION.SPI_GETHIGHCONTRAST,
                (uint)sizeof(HIGHCONTRASTW),
                (void*)&value,
                0))
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }

            highContrast = (value.dwFlags & 1) == 1;
            highContrastScheme = value.lpszDefaultScheme.ToString();
            systemHighContrastTheme = GetSystemHighContrastTheme(highContrast);
        }

        private static SystemHighContrastTheme GetSystemHighContrastTheme(bool highContrast)
        {
            var foreground = PInvoke.GetSysColor(SYS_COLOR_INDEX.COLOR_WINDOWTEXT);
            var background = PInvoke.GetSysColor(SYS_COLOR_INDEX.COLOR_WINDOW);

            if (highContrast)
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

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        struct HIGHCONTRASTW
        {
            public uint cbSize;
            public uint dwFlags;
            public Windows.Win32.Foundation.PWSTR lpszDefaultScheme;
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
