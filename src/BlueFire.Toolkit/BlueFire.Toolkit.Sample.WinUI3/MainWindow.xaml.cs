using BlueFire.Toolkit.WinUI3.Compositions;
using BlueFire.Toolkit.WinUI3.Extensions;
using BlueFire.Toolkit.WinUI3.Icons;
using BlueFire.Toolkit.WinUI3.Media;
using BlueFire.Toolkit.WinUI3.SystemBackdrops;
using BlueFire.Toolkit.WinUI3;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinRT;
using Microsoft.UI.Content;
using BlueFire.Toolkit.WinUI3.Input;
using Microsoft.Graphics.Canvas.Text;
using BlueFire.Toolkit.WinUI3.Graphics;
using System.Globalization;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BlueFire.Toolkit.Sample.WinUI3
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : WindowEx
    {
        public MainWindow()
        {
            this.InitializeComponent();

            backdrop = new TransparentBackdrop();

            //this.SystemBackdrop = backdrop;

            this.AppWindow.Closing += AppWindow_Closing;

            var rootVisual = WindowsCompositionHelper.Compositor.CreateSpriteVisual();
            rootVisual.RelativeSizeAdjustment = System.Numerics.Vector2.One;

            surfaceLoader = CompositionSurfaceLoader.StartLoadFromUri(new Uri("https://www.microsoft.com/favicon.ico?v2"));

            var brush = WindowsCompositionHelper.Compositor.CreateSurfaceBrush(surfaceLoader.Surface);

            brush.Stretch = Windows.UI.Composition.CompositionStretch.Uniform;
            brush.HorizontalAlignmentRatio = 0.5f;
            brush.VerticalAlignmentRatio = 0.5f;

            rootVisual.Brush = brush;

            this.RootVisual = rootVisual;
        }

        private void HotKeyModel_Invoked(HotKeyModel sender, HotKeyInvokedEventArgs args)
        {
            Debug.WriteLine(sender);
        }

        CompositionSurfaceLoader surfaceLoader;

        TransparentBackdrop backdrop;

        private void AppWindow_Closing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
        {
            AppWindow.SetDialogResult(true);
        }

        bool dark = false;

        private async void myButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = new MainWindow();
            var result = await mainWindow.AppWindow.ShowDialogAsync(AppWindow.Id);

            Debug.WriteLine($"DialogResult: {result}");

            if (this.SystemBackdrop == null) this.SystemBackdrop = backdrop;
            else this.SystemBackdrop = null;
        }


        protected override void OnSizeChanged(WindowExSizeChangedEventArgs args)
        {
            base.OnSizeChanged(args);

            Debug.WriteLine($"NewSize: {args.NewSize}, PreviousSize: {args.PreviousSize}");
        }

        protected override void OnDpiChanged(WindowExDpiChangedEventArgs args)
        {
            base.OnDpiChanged(args);

            Debug.WriteLine($"NewDpi: {args.NewDpi}, PreviousDpi: {args.PreviousDpi}");
        }

        protected override void OnWindowMessageReceived(WindowMessageReceivedEventArgs e)
        {
            base.OnWindowMessageReceived(e);
        }

        private void myCanvasControl_Draw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
        {
            using (var format = new CanvasTextFormat())
            {
                CanvasTextFormatHelper.SetFontFamilySource(
                    format,
                    "Monotype Corsiva, serif",
                    CultureInfo.CurrentUICulture.Name,
                    (obj, fontFamilyName) => obj.FontFamily = fontFamilyName,
                    (fontFileUri) => new CanvasFontSet(fontFileUri));

                using (var layout = new CanvasTextLayout(sender, "≤‚ ‘“ªœ¬ABC", format, float.MaxValue, float.MaxValue))
                {
                    args.DrawingSession.DrawTextLayout(layout, 0, 0, Windows.UI.Color.FromArgb(255, 255, 0, 0));
                }
            }
        }
    }
}
