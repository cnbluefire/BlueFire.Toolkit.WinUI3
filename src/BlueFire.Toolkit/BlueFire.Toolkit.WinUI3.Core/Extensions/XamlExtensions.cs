using BlueFire.Toolkit.WinUI3.WindowBase;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueFire.Toolkit.WinUI3.Extensions
{
    public static class XamlExtensions
    {
        /// <summary>
        /// Get the window that hosts the UIElement.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static WindowId GetWindowId(this UIElement element)
        {
            if (element is WindowEx windoeEx)
            {
                return windoeEx.AppWindow.Id;
            }
            else if (element?.XamlRoot != null)
            {
                return element.XamlRoot.ContentIslandEnvironment.AppWindowId;
            }
            return default;
        }

        /// <summary>
        /// Try get the manager of the window hosting the UIElement.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static WindowManager? TryGetWindowManager(this UIElement element)
        {
            return WindowManager.Get(GetWindowId(element));
        }

        /// <summary>
        /// Try get the <see cref="BlueFire.Toolkit.WinUI3.WindowEx"/> that hosts the UIElement.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static WindowEx? TryGetWindowEx(this UIElement element)
        {
            return TryGetWindowManager(element)?.WindowExInternal;
        }
    }
}
