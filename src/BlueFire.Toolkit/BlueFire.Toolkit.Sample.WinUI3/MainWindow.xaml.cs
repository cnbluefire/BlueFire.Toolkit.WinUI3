using BlueFire.Toolkit.WinUI3.Extensions;
using BlueFire.Toolkit.WinUI3.Media;
using BlueFire.Toolkit.WinUI3.SystemBackdrops;
using BlueFire.Toolkit.WinUI3;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using BlueFire.Toolkit.WinUI3.Input;
using BlueFire.Toolkit.WinUI3.Text;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using BlueFire.Toolkit.WinUI3.Core.Dispatching;
using Microsoft.UI.Composition.SystemBackdrops;
using Windows.UI;
using Microsoft.UI.Xaml.Media;
using BlueFire.Toolkit.Sample.WinUI3.Utils;
using System.Collections.ObjectModel;
using BlueFire.Toolkit.Sample.WinUI3.Models;
using System.Linq;
using BlueFire.Toolkit.Sample.WinUI3.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BlueFire.Toolkit.Sample.WinUI3
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : WindowEx
    {
        unsafe public MainWindow()
        {
            this.InitializeComponent();

            VM.NavigationViewAdapter.SetNavigationView(MainNavigationView);
            VM.NavigationService.SetFrame(RootFrame);

            UpdateBackdropTheme();

            this.LayoutRoot.ActualThemeChanged += (s, a) => UpdateBackdropTheme();
        }

        private MainWindowViewModel VM => ViewModelLocator.Instance.MainWindowViewModel;

        private void UpdateBackdropTheme()
        {
            if (this.AppWindow != null)
            {
                ((MaterialCardBackdrop)this.SystemBackdrop).MaterialConfiguration.SetTheme(LayoutRoot.ActualTheme switch
                {
                    ElementTheme.Light => SystemBackdropTheme.Light,
                    _ => SystemBackdropTheme.Dark
                });

                TitleBarHelper.UpdateTitleBarTheme(this.AppWindow.TitleBar, LayoutRoot.ActualTheme);
            }
        }

        private void MainNavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                if (LayoutRoot.ActualTheme == ElementTheme.Light)
                {
                    LayoutRoot.RequestedTheme = ElementTheme.Dark;
                }
                else
                {
                    LayoutRoot.RequestedTheme = ElementTheme.Light;
                }
            }
        }
    }

    public class MainNavViewTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ItemTemplate { get; set; }

        public DataTemplate SeparatorTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            if (item is NavViewSeparatorModel) return SeparatorTemplate;
            return ItemTemplate;
        }
    }
}
