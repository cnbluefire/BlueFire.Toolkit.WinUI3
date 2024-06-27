using BlueFire.Toolkit.Sample.WinUI3.Models;
using BlueFire.Toolkit.Sample.WinUI3.ViewModels;
using BlueFire.Toolkit.WinUI3.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BlueFire.Toolkit.Sample.WinUI3.Views.ToolPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LinearGradientBlurPanelPage : Page
    {
        public LinearGradientBlurPanelPage()
        {
            this.InitializeComponent();

            #region Block 2

            PointRadioButtons.ItemsSource = new[]
            {
                new PointModel(new Point(0,0), new Point(0,1)),
                new PointModel(new Point(0,0), new Point(1,1)),
                new PointModel(new Point(0,0), new Point(1,0)),
                new PointModel(new Point(0,1), new Point(1,0)),
                new PointModel(new Point(0,1), new Point(0,0)),
                new PointModel(new Point(1,1), new Point(0,0)),
                new PointModel(new Point(1,0), new Point(0,0)),
            };

            #endregion Block 2
        }

        public ToolModel ToolModel => ViewModelLocator.Instance.MainWindowViewModel.AllTools
            .First(c => c.Name == "LinearGradientBlurPanel").ToolModel;

        private void EndPointSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (linearGradientBlurPanel == null) return;
            if (e.NewValue < 0.5)
            {
                linearGradientBlurPanel.EndPoint = new Point(e.NewValue / 0.5, 1);
            }
            else
            {
                linearGradientBlurPanel.EndPoint = new Point(1, 1 - (e.NewValue - 0.5) / 0.5);
            }
        }

        #region Block 3

        private void StartAnimationButton_Click(object sender, RoutedEventArgs e)
        {
            ((Button)sender).IsEnabled = false;

            var compositor = Microsoft.UI.Xaml.Hosting.ElementCompositionPreview
                .GetElementVisual(this).Compositor;

            var scope = compositor.CreateScopedBatch(Microsoft.UI.Composition.CompositionBatchTypes.Animation);

            var animation = compositor.CreateScalarKeyFrameAnimation();
            animation.InsertKeyFrame(0f, 64f);
            animation.InsertKeyFrame(0.5f, 0.5f);
            animation.InsertKeyFrame(1f, 64f);
            animation.Duration = TimeSpan.FromSeconds(2);

            linearGradientBlurPanel.StartMaxBlurAmountAnimation(animation);

            scope.End();

            scope.Completed += (s, a) =>
            {
                ((Button)sender).IsEnabled = true;
            };
        }

        #endregion Block 3

        private record class PointModel(Point StartPoint, Point EndPoint)
        {
            public override string ToString()
            {
                return $"({StartPoint.X}, {StartPoint.Y}) --> ({EndPoint.X}, {EndPoint.Y})";
            }
        }
    }
}
