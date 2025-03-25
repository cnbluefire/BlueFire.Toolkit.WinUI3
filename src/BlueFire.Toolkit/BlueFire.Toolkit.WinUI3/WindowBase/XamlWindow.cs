using BlueFire.Toolkit.WinUI3.Extensions;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32;

namespace BlueFire.Toolkit.WinUI3.WindowBase
{
    public partial class XamlWindow : UserControl
    {
        private Window? window;
        private AppWindow? appWindow;
        private WindowId appWindowId;
        private WindowManager? windowManager;
        private bool sizeLoop;

        private ConcurrentDictionary<DependencyProperty, long> propertyChangedCallbackTokens;

        public XamlWindow()
        {
            propertyChangedCallbackTokens = new ConcurrentDictionary<DependencyProperty, long>();
        }

        public WindowId EnsureHandle()
        {
            if (window == null)
            {
                window = new Window();
                appWindow = window.AppWindow;
                appWindowId = appWindow.Id;
                windowManager = WindowManager.Get(appWindowId);
                if (windowManager != null)
                {
                    windowManager.WindowMessageReceived += WindowManager_WindowMessageReceived;

                    propertyChangedCallbackTokens[MinWidthProperty] = RegisterPropertyChangedCallback(MinWidthProperty, OnMinMaxSizeChanged);
                    propertyChangedCallbackTokens[MinHeightProperty] = RegisterPropertyChangedCallback(MinHeightProperty, OnMinMaxSizeChanged);
                    propertyChangedCallbackTokens[MaxWidthProperty] = RegisterPropertyChangedCallback(MaxWidthProperty, OnMinMaxSizeChanged);
                    propertyChangedCallbackTokens[MaxHeightProperty] = RegisterPropertyChangedCallback(MaxHeightProperty, OnMinMaxSizeChanged);

                    propertyChangedCallbackTokens[WidthProperty] = RegisterPropertyChangedCallback(WidthProperty, OnSizePropertyChanged);
                    propertyChangedCallbackTokens[HeightProperty] = RegisterPropertyChangedCallback(HeightProperty, OnSizePropertyChanged);
                }

                window.Content = this;
            }
            return appWindowId;
        }

        private void OnSizePropertyChanged(DependencyObject sender, DependencyProperty dp)
        {
            var appWindow = this.appWindow;
            if (appWindow != null)
            {
                var dpi = appWindow.GetDpiForWindow();
                appWindow.Resize(new Windows.Graphics.SizeInt32(
                    (int)(Width * dpi / 96d),
                    (int)(Height * dpi / 96d)));
            }
        }

        private void OnMinMaxSizeChanged(DependencyObject sender, DependencyProperty dp)
        {
            var windowManager = this.windowManager;
            if (windowManager != null)
            {
                if (dp == MinWidthProperty) windowManager.MinWidth = MinWidth;
                if (dp == MinHeightProperty) windowManager.MinHeight = MinHeight;
                if (dp == MinWidthProperty) windowManager.MaxWidth = MaxWidth;
                if (dp == MaxHeightProperty) windowManager.MaxHeight = MaxHeight;
            }
        }

        private void WindowManager_WindowMessageReceived(object sender, Toolkit.WindowBase.WindowMessageReceivedEventArgs e)
        {
            if (e.MessageId == PInvoke.WM_SIZE)
            {
                sizeLoop = true;
                Width = (int)(e.LParam & 0xFFFF);
                Height = (int)(e.LParam >> 16);
                UpdateLayout();
                sizeLoop = false;
            }

            OnWindowMessageReceived(e);
        }

        protected virtual void OnWindowMessageReceived(Toolkit.WindowBase.WindowMessageReceivedEventArgs e)
        {

        }
    }
}
