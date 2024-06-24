using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using BlueFire.Toolkit.Sample.WinUI3.Models;
using BlueFire.Toolkit.Sample.WinUI3.ViewModels;
using BlueFire.Toolkit.WinUI3;
using System.Diagnostics;
using BlueFire.Toolkit.WinUI3.Text;
using Microsoft.UI.Text;
using Microsoft.Graphics.Canvas;
using System.Numerics;
using Windows.UI;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.UI.Composition;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BlueFire.Toolkit.Sample.WinUI3.Views.ToolPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FormattedTextPage : Page
    {
        #region Sample Text
        private const string SampleText = "I can eat glass, it doesn't hurt me.\n我能吞下玻璃而不伤身体";
        #endregion Sample Text

        public FormattedTextPage()
        {
            this.InitializeComponent();

            this.SizeChanged += (s, a) =>
            {
                TextCanvas.Invalidate();
                UpdateCompositionShapeText();
            };

            this.ActualThemeChanged += (s, a) =>
            {
                TextCanvas.Invalidate();
                UpdateCompositionShapeText();
            };
        }

        public ToolModel ToolModel => ViewModelLocator.Instance.MainWindowViewModel.AllTools
            .First(c => c.Name == "FormattedText").ToolModel;


        private void TextCanvas_Draw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
        {
            try
            {
                args.DrawingSession.Transform = Matrix3x2.CreateTranslation(20, 20);
                var textLayoutWidth = sender.ActualWidth - 20;

                var textColor = this.ActualTheme switch
                {
                    ElementTheme.Light => Color.FromArgb(255, 0, 0, 0),
                    _ => Color.FromArgb(255, 255, 255, 255),
                };

                #region Block 1

                args.DrawingSession.DrawLine(
                    new Vector2(-100, 0),
                    new Vector2((float)sender.ActualWidth + 100, 0),
                    Color.FromArgb(255, 127, 127, 127));

                args.DrawingSession.DrawLine(
                    new Vector2(0, -100),
                    new Vector2(0, (float)sender.ActualHeight + 100),
                    Color.FromArgb(255, 127, 127, 127));

                using var formattedText = new FormattedText(
                    text: SampleText,
                    localeName: "en-US",
                    flowDirection: FlowDirection.LeftToRight,
                    typeface: new FormattedTextTypeface(
                        FontFamily: "Comic Sans MS, Simsun",
                        FontWeight: FontWeights.Bold,
                        FontStyle: Windows.UI.Text.FontStyle.Normal,
                        FontStretch: Windows.UI.Text.FontStretch.Normal),
                    fontSize: 18,
                    isPixelSnappingEnabled: false,
                    isColorFontEnabled: false)
                {
                    MaxTextWidth = textLayoutWidth,
                    TextTrimming = TextTrimming.None,
                    TextWrapping = TextWrapping.Wrap,
                };

                var layoutBox = new Rect(0, 0, formattedText.Width, formattedText.Height);
                var boundingBox = new Rect(
                    -formattedText.OverhangLeading,
                    formattedText.Height + formattedText.OverhangAfter - formattedText.Extent,
                    formattedText.Width - formattedText.OverhangLeading - formattedText.OverhangTrailing,
                    formattedText.Extent);

                args.DrawingSession.FillRectangle(
                    layoutBox,
                    Color.FromArgb(127, 0, 127, 0));

                args.DrawingSession.DrawRectangle(
                    boundingBox,
                    Color.FromArgb(200, 255, 0, 0));

                foreach (var line in formattedText.LineGlyphRuns)
                {
                    args.DrawingSession.DrawRectangle(line.Bounds, Color.FromArgb(255, 127, 127, 127));
                }

                using var textLayout = formattedText.CreateCanvasTextLayout(sender);
                args.DrawingSession.DrawTextLayout(textLayout, new Vector2(0, 0), textColor);

                #endregion Block 1

            }
            catch (Exception ex) when (sender.Device.IsDeviceLost(ex.HResult))
            {
                sender.Device.RaiseDeviceLost();
            }
        }



        private void UpdateCompositionShapeText()
        {
            var strokeColor = this.ActualTheme switch
            {
                ElementTheme.Light => Color.FromArgb(255, 0, 0, 0),
                _ => Color.FromArgb(255, 255, 255, 255),
            };

            var fillColor = this.ActualTheme switch
            {
                ElementTheme.Light => Color.FromArgb(255, 255, 255, 255),
                _ => Color.FromArgb(255, 0, 0, 0),
            };

            #region Block 2

            var compositor = ElementCompositionPreview.GetElementVisual(CompositionShapeHost).Compositor;

            using var formattedText = new FormattedText(
                text: SampleText,
                localeName: "en-US",
                flowDirection: FlowDirection.LeftToRight,
                typeface: new FormattedTextTypeface(
                    FontFamily: FontFamily.XamlAutoFontFamily.Source,
                    FontWeight: FontWeights.Bold,
                    FontStyle: Windows.UI.Text.FontStyle.Normal,
                    FontStretch: Windows.UI.Text.FontStretch.Normal),
                fontSize: 20,
                isPixelSnappingEnabled: false,
                isColorFontEnabled: false)
            {
                MaxTextWidth = CompositionShapeHost.ActualWidth,
                TextTrimming = TextTrimming.None,
                TextWrapping = TextWrapping.Wrap,
            };

            using var textLayout = formattedText.CreateCanvasTextLayout(CanvasDevice.GetSharedDevice());
            using var geometry = CanvasGeometry.CreateText(textLayout);

            var compositionPath = new CompositionPath(geometry);
            var compositionGeometry = compositor.CreatePathGeometry(compositionPath);
            var shape = compositor.CreateSpriteShape(compositionGeometry);
            shape.StrokeThickness = 1;
            shape.FillBrush = compositor.CreateColorBrush(fillColor);
            shape.StrokeBrush = compositor.CreateColorBrush(strokeColor);

            var shapeVisual = compositor.CreateShapeVisual();
            shapeVisual.Shapes.Add(shape);
            shapeVisual.RelativeSizeAdjustment = Vector2.One;
            shapeVisual.IsPixelSnappingEnabled = true;
            ElementCompositionPreview.SetElementChildVisual(CompositionShapeHost, shapeVisual);

            #endregion Block 2
        }
    }
}
