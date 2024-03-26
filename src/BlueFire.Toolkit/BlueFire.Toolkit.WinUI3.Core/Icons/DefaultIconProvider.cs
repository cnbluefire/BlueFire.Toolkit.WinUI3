using BlueFire.Toolkit.WinUI3.Extensions;
using BlueFire.Toolkit.WinUI3.Icons.InternalIcons;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueFire.Toolkit.WinUI3.Icons
{
    /// <summary>
    /// Icon provider for packaged and unpackaged app.
    /// </summary>
    public sealed class DefaultIconProvider : IconProvider
    {
        private static ProcessDefaultIcon? processDefaultIcon;
        private static object locker = new object();
        private static DefaultIconProvider? instance;

        public DefaultIconProvider()
        {
        }

        internal override ComposedIcon? GetComposedIconCore(ApplicationTheme requestedTheme, bool highContrast)
        {
            if (PackageInfo.IsPackagedApp) return PackageDefaultIcon.GetPackageDefaultIcon(requestedTheme, highContrast);

            if (processDefaultIcon == null)
            {
                lock (locker)
                {
                    if (processDefaultIcon == null)
                    {
                        processDefaultIcon = new ProcessDefaultIcon();
                    }
                }
            }

            return processDefaultIcon;
        }

        public static DefaultIconProvider Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (locker)
                    {
                        if (instance == null)
                        {
                            instance = new DefaultIconProvider();
                        }
                    }
                }
                return instance;
            }
        }
    }
}
