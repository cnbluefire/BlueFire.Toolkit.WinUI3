using BlueFire.Toolkit.Sample.WinUI3.Models;
using BlueFire.Toolkit.Sample.WinUI3.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueFire.Toolkit.Sample.WinUI3.Services
{
    public class NavigationViewAdapter
    {
        private readonly NavigationService navigationService;
        private NavigationView navigationView;

        public NavigationViewAdapter(NavigationService navigationService)
        {
            this.navigationService = navigationService;
            navigationService.Navigated += NavigationService_Navigated;
        }

        public void SetNavigationView(NavigationView navigationView)
        {
            this.navigationView = navigationView;
            navigationView.ItemInvoked += NavigationView_ItemInvoked;
        }

        public object? SelectedItem
        {
            get => navigationView.SelectedItem;
            set
            {
                if (navigationView.SelectedItem != value)
                {
                    navigationView.SelectedItem = value;

                    if (value is NavViewItemModel itemModel)
                    {
                        OnItemInvoked(itemModel, null);
                    }
                }
            }
        }

        private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItem is NavViewItemModel itemModel)
            {
                OnItemInvoked(itemModel, args.RecommendedNavigationTransitionInfo);
            }
        }

        private void OnItemInvoked(NavViewItemModel itemModel, NavigationTransitionInfo? navigationTransitionInfo)
        {
            if (itemModel.PageType != null && navigationService.SourcePageType != itemModel.PageType)
            {
                navigationService.Navigate(itemModel.PageType, null, navigationTransitionInfo);
            }

            if (itemModel.ClickAction != null)
            {
                _ = itemModel.ClickAction.Invoke();
            }
        }

        private void NavigationService_Navigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            navigationView.SelectedItem = ViewModelLocator.Instance.MainWindowViewModel.AllTools.FirstOrDefault(c => c.PageType == e.SourcePageType);
        }
    }
}
