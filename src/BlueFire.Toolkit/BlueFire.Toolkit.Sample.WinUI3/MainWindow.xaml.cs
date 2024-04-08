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
using BlueFire.Toolkit.WinUI3.Text;
using System.Threading;
using Microsoft.UI.Input;
using Microsoft.UI.Composition;
using Microsoft.UI.Windowing;
using Microsoft.Graphics.Canvas;
using BlueFire.Toolkit.WinUI3.Core.Dispatching;
using BlueFire.Toolkit.WinUI3.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BlueFire.Toolkit.Sample.WinUI3
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : WindowEx
    {
        private WindowMessageListener windowMessageListener;

        unsafe public MainWindow()
        {
            this.InitializeComponent();

            if (backdrop == null)
            {
                backdrop = new TransparentBackdrop();
            }

            this.SystemBackdrop = backdrop;

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

            this.Loaded += MainWindow_Loaded;

            myHotKeyInputBox.HotKeyModel = HotKeyManager.RegisterKey("Test", HotKeyModifiers.MOD_CONTROL | HotKeyModifiers.MOD_ALT, VirtualKeys.VK_LEFT);
            HotKeyManager.HotKeyInvoked += HotKeyManager_HotKeyInvoked;
            myHotKeyInputBox.HotKeyModel.VirtualKey = VirtualKeys.VK_RIGHT;

            AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        }

        private void HotKeyManager_HotKeyInvoked(HotKeyInvokedEventArgs args)
        {
            Debug.WriteLine(args.Model.Id);
        }

        private void enableHotKeySwitcher_Toggled(object sender, RoutedEventArgs e)
        {
            HotKeyManager.IsEnabled = enableHotKeySwitcher.IsOn;
        }

        int threadId;

        private unsafe void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var compositeFont = new CompositeFontFamily()
            {
                FontFamilyName = "Custom Font",
                FamilyMaps = new List<CompositeFontFamilyMap>()
                {
                    new CompositeFontFamilyMap()
                    {
                        Target = "Segoe Print, Simsun",
                        LanguageTag = "zh",
                        UnicodeRanges = new[]
                        {
                            new UnicodeRange()
                            {
                                first = '测',
                                last = '测'
                            },
                            new UnicodeRange()
                            {
                                first = 'A',
                                last = 'A'
                            }
                        }
                    }
                }
            };

            CompositeFontManager.Register(compositeFont);
        }

        private void HotKeyModel_Invoked(HotKeyModel sender, HotKeyInvokedEventArgs args)
        {
            Debug.WriteLine(sender);
        }

        CompositionSurfaceLoader surfaceLoader;

        static TransparentBackdrop backdrop;

        private void AppWindow_Closing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
        {
            AppWindow.SetDialogResult(true);
        }

        bool dark = false;

        private async void myButton_Click(object sender, RoutedEventArgs e)
        {
            var test = WindowManager.TryGetAllWindowIds();
            return;

            var mainWindow = new MainWindow();
            var result = await mainWindow.AppWindow.ShowDialogAsync(AppWindow.Id);

            //Debug.WriteLine($"DialogResult: {result}");

            //if (this.SystemBackdrop == null) this.SystemBackdrop = backdrop;
            //else this.SystemBackdrop = null;
        }


        protected override void OnSizeChanged(WindowExSizeChangedEventArgs args)
        {
            base.OnSizeChanged(args);

            //Debug.WriteLine($"NewSize: {args.NewSize}, PreviousSize: {args.PreviousSize}");
        }

        protected override void OnDpiChanged(WindowExDpiChangedEventArgs args)
        {
            base.OnDpiChanged(args);

            //Debug.WriteLine($"NewDpi: {args.NewDpi}, PreviousDpi: {args.PreviousDpi}");
        }

        protected override void OnWindowMessageReceived(WindowMessageReceivedEventArgs e)
        {
            base.OnWindowMessageReceived(e);

            if (e.MessageId == 533U)
            {
                // WM_CAPTURECHANGED

                Debug.WriteLine($"This: 0x{e.WindowId.Value:x8}, Captured: 0x{e.LParam:x8}");
            }

            //Debug.WriteLine(e.ToString());
        }

        private void myCanvasControl_Draw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
        {
            //using var formattedText = new FormattedText(
            //    "测试一下ABC测试一下ABC测试一下ABC",
            //    "en",
            //    FlowDirection.LeftToRight,
            //    new FormattedTextTypeface(
            //        "Custom Font, 方正舒体, Wide Latin",
            //        Microsoft.UI.Text.FontWeights.Normal,
            //        Windows.UI.Text.FontStyle.Normal,
            //        Windows.UI.Text.FontStretch.Normal),
            //    24,
            //    true,
            //    true);

            //formattedText.MaxTextWidth = sender.ActualWidth;
            //formattedText.TextWrapping = TextWrapping.Wrap;
            //formattedText.LineHeight = 40;

            //using var layout = formattedText.CreateCanvasTextLayout(sender);
            //args.DrawingSession.DrawTextLayout(layout, 0, 0, Windows.UI.Color.FromArgb(255, 255, 0, 0));

            //for (int i = 0; i < formattedText.LineGlyphRuns.Count; i++)
            //{
            //    for (int j = 0; j < formattedText.LineGlyphRuns[i].GlyphRuns.Length; j++)
            //    {
            //        var glyphRun = formattedText.LineGlyphRuns[i].GlyphRuns[j];
            //        args.DrawingSession.FillRectangle(glyphRun.LayoutBounds, RandomColor(0.5));
            //    }
            //}

            //static Windows.UI.Color RandomColor(double opacity)
            //{
            //    return Windows.UI.Color.FromArgb(
            //        (byte)Math.Clamp(opacity * 255, 0, 255),
            //        (byte)Random.Shared.Next(0, 256),
            //        (byte)Random.Shared.Next(0, 256),
            //        (byte)Random.Shared.Next(0, 256));
            //}
        }

        private void Grid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            //((UIElement)sender).CancelDirectManipulations();

            //Debug.WriteLine($"PointerId: {e.Pointer.PointerId}");


            //var p = e.GetCurrentPoint((UIElement)sender);
            //if (p.Properties.IsPrimary)
            //{
            //    e.Handled = true;

            //    SendMessage(this.GetWindowHandle(), 0x0202, 0, 0);
            //    SendMessage(this.GetWindowHandle(), 0x0112, 0xF010 + 2, 0);
            //}
        }

        private void Grid_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            //WindowDragExtensions.Test(e);
        }



        [DllImport("user32.dll")]
        private extern static int SendMessage(nint hWnd, int msg, nint wParam, nint lParam);

        [DllImport("user32.dll")]
        private extern static bool ReleaseCapture();

        [DllImport("user32.dll")]
        private extern static nint GetSystemMenu(nint hWnd, bool bRevert);

        [DllImport("user32.dll")]
        private extern static bool RemoveMenu(nint hWnd, uint uPosition, uint uFlags);

        [DllImport("user32.dll")]
        private extern static bool DrawMenuBar(nint hWnd);

        [DllImport("user32.dll")]
        private extern static nint FindWindowEx(nint hWndParent, nint hWndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll")]
        private extern static nint GetWindowLongPtr(nint hWnd, int nIndex);

        [DllImport("user32.dll")]
        private extern static nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong);

        [DllImport("comctl32.dll")]
        private extern static bool SetWindowSubclass(nint hWnd, SUBCLASSPROC pfnSubclass, nuint uIdSubclass, nint dwRefData);

        [DllImport("comctl32.dll")]
        private extern static nint DefSubclassProc(nint hWnd, int msg, nint wParam, nint lParam);

        private delegate nint SUBCLASSPROC(nint hWnd, int msg, nint wParam, nint lParam, nuint uIdSubclass, nint dwRefData);

    }
}
