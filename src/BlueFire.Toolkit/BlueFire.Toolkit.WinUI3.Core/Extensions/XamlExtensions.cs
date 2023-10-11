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

        public static WindowManager? TryGetWindowManager(this UIElement element)
        {
            return WindowManager.Get(GetWindowId(element));
        }

        public static WindowEx? TryGetWindowEx(this UIElement element)
        {
            return TryGetWindowManager(element)?.WindowExInternal;
        }
    }
}
