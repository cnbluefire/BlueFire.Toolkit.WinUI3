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
using WinRT;

namespace BlueFire.Toolkit.WinUI3.Extensions
{
    public static class ContentDialogExtensions
    {
        private static Dictionary<nint, WeakReference<Controls.Primitives.ContentDialogHostWindow>> hostWindows = new Dictionary<nint, WeakReference<Controls.Primitives.ContentDialogHostWindow>>();

        public static async Task<ContentDialogResult> ShowModalWindowAsync(this ContentDialog contentDialog, ShowDialogOptions? options = null)
        {
            var ptr = (nint)((IWinRTObject)contentDialog).NativeObject.ThisPtr;

            Controls.Primitives.ContentDialogHostWindow window;

            lock (hostWindows)
            {
                if (hostWindows.ContainsKey(ptr)) throw new ArgumentException(null, nameof(contentDialog));

                window = new Controls.Primitives.ContentDialogHostWindow(
                    contentDialog);

                hostWindows[ptr] = new WeakReference<Controls.Primitives.ContentDialogHostWindow>(window);
            }

            try
            {
                await window.AppWindow.ShowDialogAsync(options);
                return window.ContentDialogResult;
            }
            finally
            {
                lock (hostWindows)
                {
                    hostWindows.Remove(ptr);
                }
            }
        }

        public static bool TryGetModalWindowId(this ContentDialog contentDialog, out WindowId windowId)
        {
            windowId = default;

            var ptr = (nint)((IWinRTObject)contentDialog).NativeObject.ThisPtr;

            lock (hostWindows)
            {
                if (hostWindows.TryGetValue(ptr, out var weakReference)
                    && weakReference.TryGetTarget(out var target))
                {
                    windowId = target.AppWindow?.Id ?? default;
                }
            }

            return windowId.Value != 0;
        }
    }
}
