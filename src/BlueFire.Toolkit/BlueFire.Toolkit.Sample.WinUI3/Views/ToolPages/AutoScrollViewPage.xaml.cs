using BlueFire.Toolkit.Sample.WinUI3.Models;
using BlueFire.Toolkit.Sample.WinUI3.Utils;
using BlueFire.Toolkit.Sample.WinUI3.ViewModels;
using BlueFire.Toolkit.WinUI3.Extensions;
using BlueFire.Toolkit.WinUI3.Input;
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
using Windows.Graphics.Imaging;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BlueFire.Toolkit.Sample.WinUI3.Views.ToolPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AutoScrollViewPage : Page
    {
        public AutoScrollViewPage()
        {
            this.InitializeComponent();

            PlayButton.Click += async (s, a) =>
            {
                var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Owl.jpg"));
                using (var fs = await file.OpenReadAsync())
                {
                    var decoder = await BitmapDecoder.CreateAsync(fs);
                    var bitmap = await decoder.GetSoftwareBitmapAsync();
                    var bitmap2 = bitmap.GetReadOnlyView();
                    bitmap.Dispose();
                    bitmap = null;

                    using(var fs2 = File.OpenWrite("C:\\Users\\blue-fire\\Downloads\\迅雷下载\\test.icon"))
                    {
                        await Graphics.IconHelper.ConvertBitmapToIconStreamAsync(bitmap2, [16, 20, 24, 32, 40, 48, 64, 256], fs2);

                        fs2.SetLength(fs2.Position);
                    }
                }


                //RuntimeEnabledFeatureDetector.SetFeatureOverride(RuntimeEnabledFeatureDetector.RuntimeEnabledFeature.DisableGlobalAnimations, true, out _);

                //var type = BlueFire.Toolkit.WinUI3.Input.InputManager.GetLastInputDeviceType();
                //System.Diagnostics.Debug.WriteLine(type);
            };
        }

        public ToolModel ToolModel => ViewModelLocator.Instance.MainWindowViewModel.AllTools
            .First(c => c.Name == "AutoScrollView").ToolModel;
    }
}
