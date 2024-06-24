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
using BlueFire.Toolkit.WinUI3.SystemBackdrops;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BlueFire.Toolkit.Sample.WinUI3.Views.ToolPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MaterialCardBackdropPage : Page
    {
        public MaterialCardBackdropPage()
        {
            this.InitializeComponent();
        }

        public ToolModel ToolModel => ViewModelLocator.Instance.MainWindowViewModel.AllTools
            .First(c => c.Name == "MaterialCardBackdrop").ToolModel;

        private void ClickWindowButton_Click(object sender, RoutedEventArgs e)
        {
            #region Block 1

            var window = new BackdropSampleWindow(nameof(MaterialCardBackdrop));
            window.Activate();

            #endregion Block 1
        }
    }
}
