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

namespace BlueFire.Toolkit.WinUI3.WindowBase
{
    [ContentProperty(Name = nameof(Content))]
    public class WindowEx : DependencyObject
    {
        private XamlWindow xamlWindow;
        private WindowManager windowManager;
        private bool windowInitialized;
        private HWND hWnd;
        private uint dpi;
        private bool destroying;
        private DesktopWindowTarget? desktopWindowTarget;
        private bool hasShowed;

        public WindowEx()
        {
            xamlWindow = new XamlWindow();

            hWnd = new HWND((nint)xamlWindow.AppWindow.Id.Value);

            windowManager = WindowManager.Get(xamlWindow.AppWindow)!;
            windowManager.WindowExInternal = this;

            Title = xamlWindow.Title;

            dpi = PInvoke.GetDpiForWindow(hWnd);

            Width = 800;
            Height = 600;

            xamlWindow.Content = Content;
            xamlWindow.SystemBackdrop = SystemBackdrop;

            windowInitialized = true;

            windowManager.GetMonitorInternal().WindowMessageBeforeReceived += WindowManager_WindowMessageBeforeReceived;

            SetWindowSize(Width, Height);
        }

        public XamlWindow XamlWindow => xamlWindow;

        public AppWindow AppWindow => XamlWindow.AppWindow;

        public uint WindowDpi => dpi;

        public Windows.UI.Composition.Visual RootVisual
        {
            get => EnsureDesktopWindowTarget().Root;
            set => EnsureDesktopWindowTarget().Root = value;
        }

        public UIElement Content
        {
            get { return (UIElement)GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        public SystemBackdrop SystemBackdrop
        {
            get { return (SystemBackdrop)GetValue(SystemBackdropProperty); }
            set { SetValue(SystemBackdropProperty, value); }
        }

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public double Width
        {
            get { return (double)GetValue(WidthProperty); }
            set { SetValue(WidthProperty, value); }
        }

        public double Height
        {
            get { return (double)GetValue(HeightProperty); }
            set { SetValue(HeightProperty, value); }
        }

        public double MinWidth
        {
            get { return (double)GetValue(MinWidthProperty); }
            set { SetValue(MinWidthProperty, value); }
        }

        public double MinHeight
        {
            get { return (double)GetValue(MinHeightProperty); }
            set { SetValue(MinHeightProperty, value); }
        }

        public double MaxWidth
        {
            get { return (double)GetValue(MaxWidthProperty); }
            set { SetValue(MaxWidthProperty, value); }
        }

        public double MaxHeight
        {
            get { return (double)GetValue(MaxHeightProperty); }
            set { SetValue(MaxHeightProperty, value); }
        }



        public static readonly DependencyProperty ContentProperty =
            DependencyProperty.Register("Content", typeof(UIElement), typeof(WindowEx), new PropertyMetadata(null, (s, a) =>
            {
                if (s is WindowEx sender && !Equals(a.NewValue, a.OldValue) && sender.windowInitialized)
                {
                    sender.xamlWindow.Content = a.NewValue as UIElement;
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

        public static readonly DependencyProperty WidthProperty =
            DependencyProperty.Register("Width", typeof(double), typeof(WindowEx), new PropertyMetadata(0d, (s, a) =>
            {
                if (s is WindowEx sender && !Equals(a.NewValue, a.OldValue) && sender.windowInitialized)
                {
                    sender.SetWindowSize(sender.Width, sender.Height);
                }
            }));

        public static readonly DependencyProperty HeightProperty =
            DependencyProperty.Register("Height", typeof(double), typeof(WindowEx), new PropertyMetadata(0d, (s, a) =>
            {
                if (s is WindowEx sender && !Equals(a.NewValue, a.OldValue) && sender.windowInitialized)
                {
                    sender.SetWindowSize(sender.Width, sender.Height);
                }
            }));

        public static readonly DependencyProperty MinWidthProperty =
            DependencyProperty.Register("MinWidth", typeof(double), typeof(WindowEx), new PropertyMetadata(0d, (s, a) =>
            {
                if (s is WindowEx sender && !Equals(a.NewValue, a.OldValue) && sender.windowInitialized)
                {
                    sender.windowManager.MinWidth = (double)a.NewValue;
                }
            }));

        public static readonly DependencyProperty MinHeightProperty =
            DependencyProperty.Register("MinHeight", typeof(double), typeof(WindowEx), new PropertyMetadata(0d, (s, a) =>
            {
                if (s is WindowEx sender && !Equals(a.NewValue, a.OldValue) && sender.windowInitialized)
                {
                    sender.windowManager.MinHeight = (double)a.NewValue;
                }
            }));

        public static readonly DependencyProperty MaxWidthProperty =
            DependencyProperty.Register("MaxWidth", typeof(double), typeof(WindowEx), new PropertyMetadata(0d, (s, a) =>
            {
                if (s is WindowEx sender && !Equals(a.NewValue, a.OldValue) && sender.windowInitialized)
                {
                    sender.windowManager.MaxWidth = (double)a.NewValue;
                }
            }));

        public static readonly DependencyProperty MaxHeightProperty =
            DependencyProperty.Register("MaxHeight", typeof(double), typeof(WindowEx), new PropertyMetadata(0d, (s, a) =>
            {
                if (s is WindowEx sender && !Equals(a.NewValue, a.OldValue) && sender.windowInitialized)
                {
                    sender.windowManager.MaxHeight = (double)a.NewValue;
                }
            }));

        public void Activate()
        {
            xamlWindow.Activate();
        }


        private void WindowManager_WindowMessageBeforeReceived(WindowManager sender, WindowMessageReceivedEventArgs e)
        {
            if (e.MessageId == PInvoke.WM_DPICHANGED)
            {
                var oldDpi = dpi;
                dpi = PInvoke.GetDpiForWindow(hWnd);
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
                        OnSizeChanged(new Windows.Foundation.Size(Width, Height), new Windows.Foundation.Size(0, 0));
                    }
                }
            }
            else if (e.MessageId == PInvoke.WM_DESTROY)
            {
                destroying = true;

                desktopWindowTarget?.Dispose();
                desktopWindowTarget = null;
            }

            OnWindowMessageReceived(e);
        }

        private void SetWindowSize(double width, double height)
        {
            xamlWindow.AppWindow.Resize(new Windows.Graphics.SizeInt32((int)(width * dpi / 96), (int)(height * dpi / 96)));
        }

        private DesktopWindowTarget EnsureDesktopWindowTarget()
        {
            if (destroying) throw new ObjectDisposedException(nameof(WindowEx));

            if (desktopWindowTarget == null)
            {
                desktopWindowTarget = WindowsCompositionHelper.CreateDesktopWindowTarget(xamlWindow.AppWindow.Id, false);
            }

            return desktopWindowTarget;
        }

        public event WindowExSizeChangedEventHandler? SizeChanged;
        public event WindowExDpiChangedEventHandler? DpiChanged;
        public event WindowExMessageReceivedEventHandler? WindowMessageReceived;

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
