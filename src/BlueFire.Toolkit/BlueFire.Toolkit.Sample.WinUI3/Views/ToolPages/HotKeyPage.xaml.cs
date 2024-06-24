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
using BlueFire.Toolkit.WinUI3.Input;
using BlueFire.Toolkit.WinUI3.Extensions;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BlueFire.Toolkit.Sample.WinUI3.Views.ToolPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HotKeyPage : Page
    {
        public HotKeyPage()
        {
            this.InitializeComponent();
        }

        public ToolModel ToolModel => ViewModelLocator.Instance.MainWindowViewModel.AllTools
            .First(c => c.Name == "HotKey").ToolModel;

        private HotKeyModel hotKeyModel;

        #region Block 2

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            hotKeyModel = HotKeyManager.RegisterKey("Test", HotKeyModifiers.MOD_CONTROL | HotKeyModifiers.MOD_ALT, VirtualKeys.VK_Q);
            hotKeyModel.Label = "Test Key";
            hotKeyModel.Invoked += HotKeyModel_Invoked;

        }

        #endregion Block 2

        #region Block 4

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);

            HotKeyManager.Unregister(hotKeyModel.Id);
            hotKeyModel.Invoked -= HotKeyModel_Invoked;
            hotKeyModel = null;
        }

        #endregion Block 4


        #region Block 3

        void HotKeyModel_Invoked(HotKeyModel sender, HotKeyInvokedEventArgs args)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                OnHotKeyInvoked();
            });
        }

        #endregion Block 3

        private async void OnHotKeyInvoked()
        {
            var cd = new ContentDialog()
            {
                Content = "HotKey Invoked!",
                PrimaryButtonText = "OK"
            };

            await cd.ShowModalWindowAsync();
        }
    }
}
