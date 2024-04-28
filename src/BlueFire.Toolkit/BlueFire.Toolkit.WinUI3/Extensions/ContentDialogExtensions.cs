using BlueFire.Toolkit.WinUI3.Extensions;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace BlueFire.Toolkit.WinUI3.Extensions
{
    public static class ContentDialogExtensions
    {
        public static async Task<ContentDialogResult> ShowModalWindowAsync(this ContentDialog contentDialog, WindowId ownerWindow)
        {
            var ownerHandle = Win32Interop.GetWindowFromWindowId(ownerWindow);

            var window = new Controls.Primitives.ContentDialogHostWindow(
                contentDialog, ownerHandle);

            await window.AppWindow.ShowDialogAsync(ownerWindow);
            return window.ContentDialogResult;
        }

    }
}
