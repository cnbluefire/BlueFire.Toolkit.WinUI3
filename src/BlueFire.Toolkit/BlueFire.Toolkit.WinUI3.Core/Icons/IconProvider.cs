using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlueFire.Toolkit.WinUI3.Icons.InternalIcons;
using Microsoft.UI;
using Microsoft.UI.System;
using Microsoft.UI.Xaml;
using PInvoke = Windows.Win32.PInvoke;

namespace BlueFire.Toolkit.WinUI3.Icons
{
    /// <summary>
    /// Win32 icons provider.
    /// </summary>
    public class IconProvider : IDisposable
    {
        private bool disposedValue;

        protected bool IsDisposed => disposedValue;

        protected IconProvider() { }

        internal IconId GetSharedIcon(int width, int height, ApplicationTheme requestedTheme, bool highContrast)
        {
            var icon = GetComposedIcon(requestedTheme, highContrast);

            return icon.GetSharedIcon(width, height);
        }

        internal IconId GetSharedLargeIcon(uint dpi, ApplicationTheme requestedTheme, bool highContrast)
        {
            var width = (int)(PInvoke.GetSystemMetrics(Windows.Win32.UI.WindowsAndMessaging.SYSTEM_METRICS_INDEX.SM_CXICON) * dpi / 96);
            var height = (int)(PInvoke.GetSystemMetrics(Windows.Win32.UI.WindowsAndMessaging.SYSTEM_METRICS_INDEX.SM_CYICON) * dpi / 96);

            return GetSharedIcon(width, height, requestedTheme, highContrast);
        }

        internal IconId GetSharedSmallIcon(uint dpi, ApplicationTheme requestedTheme, bool highContrast)
        {
            var width = (int)(PInvoke.GetSystemMetrics(Windows.Win32.UI.WindowsAndMessaging.SYSTEM_METRICS_INDEX.SM_CXSMICON) * dpi / 96);
            var height = (int)(PInvoke.GetSystemMetrics(Windows.Win32.UI.WindowsAndMessaging.SYSTEM_METRICS_INDEX.SM_CYSMICON) * dpi / 96);

            return GetSharedIcon(width, height, requestedTheme, highContrast);
        }

        /// <summary>
        /// Gets an icon of the specified size and theme.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="requestedTheme"></param>
        /// <param name="highContrast"></param>
        /// <returns></returns>
        public SafeHIconHandle GetIcon(int width, int height, ApplicationTheme requestedTheme, bool highContrast)
        {
            var icon = GetComposedIcon(requestedTheme, highContrast);

            return icon.GetIcon(width, height);
        }

        /// <summary>
        /// Get an icon of the large size.
        /// </summary>
        /// <param name="dpi"></param>
        /// <param name="requestedTheme"></param>
        /// <param name="highContrast"></param>
        /// <returns></returns>
        public SafeHIconHandle GetLargeIcon(uint dpi, ApplicationTheme requestedTheme, bool highContrast)
        {
            var width = (int)(PInvoke.GetSystemMetrics(Windows.Win32.UI.WindowsAndMessaging.SYSTEM_METRICS_INDEX.SM_CXICON) * dpi / 96);
            var height = (int)(PInvoke.GetSystemMetrics(Windows.Win32.UI.WindowsAndMessaging.SYSTEM_METRICS_INDEX.SM_CYICON) * dpi / 96);

            return GetIcon(width, height, requestedTheme, highContrast);
        }

        /// <summary>
        /// Get an icon of the small size.
        /// </summary>
        /// <param name="dpi"></param>
        /// <param name="requestedTheme"></param>
        /// <param name="highContrast"></param>
        /// <returns></returns>
        public SafeHIconHandle GetSmallIcon(uint dpi, ApplicationTheme requestedTheme, bool highContrast)
        {
            var width = (int)(PInvoke.GetSystemMetrics(Windows.Win32.UI.WindowsAndMessaging.SYSTEM_METRICS_INDEX.SM_CXSMICON) * dpi / 96);
            var height = (int)(PInvoke.GetSystemMetrics(Windows.Win32.UI.WindowsAndMessaging.SYSTEM_METRICS_INDEX.SM_CYSMICON) * dpi / 96);

            return GetIcon(width, height, requestedTheme, highContrast);
        }

        private ComposedIcon GetComposedIcon(ApplicationTheme requestedTheme, bool highContrast)
        {
            var icon = GetComposedIconCore(requestedTheme, highContrast)
                ?? GetComposedIconCore(ApplicationTheme.Light, false);

            if (icon == null) throw new ArgumentException(nameof(icon));

            return icon;
        }

        internal virtual ComposedIcon? GetComposedIconCore(ApplicationTheme requestedTheme, bool highContrast)
        {
            return null;
        }

        protected virtual void DisposeCore(bool disposing)
        {

        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                DisposeCore(disposing);
                disposedValue = true;
            }
        }

        ~IconProvider()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
