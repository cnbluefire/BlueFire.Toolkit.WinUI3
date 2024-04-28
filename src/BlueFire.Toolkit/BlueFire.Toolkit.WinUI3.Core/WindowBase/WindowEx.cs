using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XamlWindow = Microsoft.UI.Xaml.Window;
using PInvoke = Windows.Win32.PInvoke;
using Windows.Win32.Foundation;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.UI.Composition.Desktop;
using BlueFire.Toolkit.WinUI3.Compositions;
using Windows.Foundation;
using Microsoft.UI;
using Microsoft.UI.Content;
using System.Numerics;

namespace BlueFire.Toolkit.WinUI3
{
    /// <summary>
    /// WindowEx object extends FrameworkElement.
    /// </summary>
    [ContentProperty(Name = nameof(Content))]
    public class WindowEx : FrameworkElement
    {
        private XamlWindow xamlWindow;
        private WindowManager windowManager;
        private bool windowInitialized;
        private HWND hWnd;
        private uint dpi;
        private bool destroying;
        private bool hasShowed;
        private bool isActivated;
        private Windows.UI.Composition.Visual? rootVisual;

        private FrameworkElement? loadHelper;

        private bool isLoading;
        private bool isLoaded;
        private TypedEventHandler<FrameworkElement, object?>? loadingHandler;
        private RoutedEventHandler? loadedHandler;

        public WindowEx()
        {
            xamlWindow = new XamlWindow();

            var manager = WindowManager.Get(xamlWindow.AppWindow);

            if (manager == null) throw new ArgumentException(null, nameof(xamlWindow.AppWindow));

            windowManager = manager;
            windowManager.WindowExInternal = this;

            hWnd = windowManager.HWND;

            windowManager.UseDefaultIcon = true;

            Title = xamlWindow.Title;

            dpi = windowManager.WindowDpi;

            Width = 800;
            Height = 600;

            xamlWindow.Content = Content;
            xamlWindow.SystemBackdrop = SystemBackdrop;

            windowInitialized = true;

            windowManager.GetMonitorInternal().WindowMessageBeforeReceived += WindowManager_WindowMessageBeforeReceived;

            SetWindowSize(Width, Height);

            RegisterPropertyChangedCallback(WidthProperty, OnSizePropertyChanged);
            RegisterPropertyChangedCallback(HeightProperty, OnSizePropertyChanged);
            RegisterPropertyChangedCallback(MinWidthProperty, OnSizePropertyChanged);
            RegisterPropertyChangedCallback(MinHeightProperty, OnSizePropertyChanged);
            RegisterPropertyChangedCallback(MaxWidthProperty, OnSizePropertyChanged);
            RegisterPropertyChangedCallback(MaxHeightProperty, OnSizePropertyChanged);
        }

        internal HWND Handle => hWnd;

        public XamlWindow XamlWindow => xamlWindow;

        public AppWindow AppWindow => XamlWindow.AppWindow;

        public uint WindowDpi => dpi;

        public new bool IsLoaded => isLoaded;

        public Windows.UI.Composition.Visual? RootVisual
        {
            get => rootVisual;
            set
            {
                if (value != rootVisual)
                {
                    rootVisual = value;

                    windowManager.WindowContentVisual.Children.RemoveAll();
                    if (rootVisual != null)
                    {
                        windowManager.WindowContentVisual.Children.InsertAtTop(rootVisual);
                    }
                }
            }
        }

        public UIElement? Content
        {
            get { return (UIElement?)GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        public SystemBackdrop? SystemBackdrop
        {
            get { return (SystemBackdrop?)GetValue(SystemBackdropProperty); }
            set { SetValue(SystemBackdropProperty, value); }
        }

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }


        public static readonly DependencyProperty ContentProperty =
            DependencyProperty.Register("Content", typeof(UIElement), typeof(WindowEx), new PropertyMetadata(null, (s, a) =>
            {
                if (s is WindowEx sender && !Equals(a.NewValue, a.OldValue) && sender.windowInitialized)
                {
                    sender.xamlWindow.Content = a.NewValue as UIElement;
                    sender.UpdateLoadHelper(a.NewValue as FrameworkElement);
                    sender.XamlRoot = sender.xamlWindow.Content?.XamlRoot;
                }
            }));


        public static readonly DependencyProperty SystemBackdropProperty =
            DependencyProperty.Register("SystemBackdrop", typeof(SystemBackdrop), typeof(WindowEx), new PropertyMetadata(null, (s, a) =>
            {
                if (s is WindowEx sender && !Equals(a.NewValue, a.OldValue) && sender.windowInitialized)
                {
                    sender.xamlWindow.SystemBackdrop = a.NewValue as SystemBackdrop;
                }
            }));


        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(WindowEx), new PropertyMetadata("", (s, a) =>
            {
                if (s is WindowEx sender && !Equals(a.NewValue, a.OldValue) && sender.windowInitialized)
                {
                    sender.xamlWindow.Title = (string)a.NewValue ?? string.Empty;
                }
            }));

        private static void OnSizePropertyChanged(DependencyObject sender, DependencyProperty dp)
        {
            var window = (WindowEx)sender;
            if (dp == WidthProperty || dp == HeightProperty)
            {
                window.SetWindowSize(window.Width, window.Height);
            }
            else if (window.windowManager != null)
            {
                if (dp == MinWidthProperty)
                {
                    window.windowManager.MinWidth = (double)window.MinWidth;
                }
                else if (dp == MinHeightProperty)
                {
                    window.windowManager.MinHeight = (double)window.MinHeight;
                }
                else if (dp == MaxWidthProperty)
                {
                    window.windowManager.MaxWidth = (double)window.MaxWidth;
                }
                else if (dp == MaxHeightProperty)
                {
                    window.windowManager.MaxHeight = (double)window.MaxHeight;
                }
            }
        }

        public void Activate()
        {
            xamlWindow.Activate();
        }

        private void UpdateLoadHelper(FrameworkElement? loadHelper)
        {
            if (isLoading) return;

            var oldHelper = this.loadHelper;
            this.loadHelper = null;

            if (oldHelper != null)
            {
                oldHelper.Loading += LoadHelper_Loading;
                oldHelper.Loaded += LoadHelper_Loaded;
            }

            if (loadHelper != null)
            {
                this.loadHelper = loadHelper;

                loadHelper.Loading += LoadHelper_Loading;
                loadHelper.Loaded += LoadHelper_Loaded;
            }
        }

        private void WindowManager_WindowMessageBeforeReceived(WindowManager sender, WindowMessageReceivedEventArgs e)
        {
            if (e.MessageId == PInvoke.WM_DPICHANGED)
            {
                var oldDpi = dpi;
                dpi = windowManager.WindowDpi;
                windowInitialized = false;
                try
                {
                    var size = xamlWindow.AppWindow.Size;
                    Width = size.Width * 96d / dpi;
                    Height = size.Height * 96d / dpi;
                }
                finally
                {
                    windowInitialized = true;
                }
                OnDpiChanged(dpi, oldDpi);
            }
            else if (e.MessageId == PInvoke.WM_SIZE)
            {
                var oldWidth = Width;
                var oldHeight = Height;
                var size = AppWindow.Size;

                windowInitialized = false;
                try
                {
                    Width = size.Width * 96d / dpi;
                    Height = size.Height * 96d / dpi;
                }
                finally
                {
                    windowInitialized = true;
                }

                if (hasShowed)
                {
                    if ((int)(oldWidth * dpi / 96) != size.Width || (int)(oldHeight * dpi / 96) != size.Height)
                    {
                        OnSizeChanged(new Windows.Foundation.Size(Width, Height), new Windows.Foundation.Size(oldWidth, oldHeight));
                    }
                }
            }
            else if (e.MessageId == PInvoke.WM_SHOWWINDOW)
            {
                if (!hasShowed)
                {
                    hasShowed = e.WParam != 0;

                    if (hasShowed)
                    {
                        RaiseLoadEvent();
                    }
                }
            }
            else if (e.MessageId == PInvoke.WM_ACTIVATE)
            {
                isActivated = unchecked((ushort)e.WParam) != 0;
            }
            else if (e.MessageId == PInvoke.WM_DESTROY)
            {
                destroying = true;
            }

            OnWindowMessageReceived(e);
        }

        private void SetWindowSize(double width, double height)
        {
            xamlWindow.AppWindow.Resize(new Windows.Graphics.SizeInt32((int)(width * dpi / 96), (int)(height * dpi / 96)));
        }

        public new event WindowExSizeChangedEventHandler? SizeChanged;
        public event WindowExDpiChangedEventHandler? DpiChanged;
        public event WindowExMessageReceivedEventHandler? WindowMessageReceived;

        public new event TypedEventHandler<FrameworkElement, object?>? Loading
        {
            add
            {
                loadingHandler += value;
            }
            remove
            {
                loadingHandler -= value;
            }
        }

        public new event RoutedEventHandler? Loaded
        {
            add
            {
                loadedHandler += value;
            }
            remove
            {
                loadedHandler -= value;
            }
        }

        private void OnSizeChanged(Windows.Foundation.Size newSize, Windows.Foundation.Size previousSize)
        {
            var args = new WindowExSizeChangedEventArgs(newSize, previousSize);
            OnSizeChanged(args);
            SizeChanged?.Invoke(this, args);
        }

        protected virtual void OnSizeChanged(WindowExSizeChangedEventArgs args)
        {
        }

        private void OnDpiChanged(uint newDpi, uint previousDpi)
        {
            var args = new WindowExDpiChangedEventArgs(newDpi, previousDpi);
            OnDpiChanged(args);
            DpiChanged?.Invoke(this, args);
        }

        protected virtual void OnDpiChanged(WindowExDpiChangedEventArgs args)
        {
        }

        protected virtual void OnWindowMessageReceived(WindowMessageReceivedEventArgs e)
        {
            WindowMessageReceived?.Invoke(this, e);
        }


        private void LoadHelper_Loading(FrameworkElement sender, object args)
        {
            isLoading = true;

            sender.Loading -= LoadHelper_Loading;

            loadingHandler?.Invoke(this, null);
        }

        private void LoadHelper_Loaded(object sender, RoutedEventArgs e)
        {
            isLoaded = true;

            var ele = (FrameworkElement)sender;

            this.XamlRoot = ele.XamlRoot;
            
            ele.Loaded -= LoadHelper_Loaded;
            loadHelper = null;

            loadedHandler?.Invoke(this, new RoutedEventArgs());
        }

        private void RaiseLoadEvent()
        {
            var loadHelper = this.loadHelper;

            if (loadHelper == null)
            {
                isLoading = true;
                loadingHandler?.Invoke(this, null);
            }
            OnSizeChanged(new Windows.Foundation.Size(Width, Height), new Windows.Foundation.Size(0, 0));

            if (loadHelper == null)
            {
                DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                {
                    if (!destroying)
                    {
                        isLoaded = true;
                        loadedHandler?.Invoke(this, new RoutedEventArgs());
                    }
                });
            }
        }
    }

    public delegate void WindowExSizeChangedEventHandler(WindowEx sender, WindowExSizeChangedEventArgs args);

    public delegate void WindowExDpiChangedEventHandler(WindowEx sender, WindowExDpiChangedEventArgs args);

    public delegate void WindowExMessageReceivedEventHandler(WindowEx sender, WindowMessageReceivedEventArgs e);

    public class WindowExSizeChangedEventArgs
    {
        internal WindowExSizeChangedEventArgs(Windows.Foundation.Size newSize, Windows.Foundation.Size previousSize)
        {
            NewSize = newSize;
            PreviousSize = previousSize;
        }

        public Windows.Foundation.Size NewSize { get; }

        public Windows.Foundation.Size PreviousSize { get; }
    }

    public class WindowExDpiChangedEventArgs
    {
        internal WindowExDpiChangedEventArgs(uint newDpi, uint previousDpi)
        {
            NewDpi = newDpi;
            PreviousDpi = previousDpi;
        }

        public uint NewDpi { get; }

        public uint PreviousDpi { get; }
    }
}
