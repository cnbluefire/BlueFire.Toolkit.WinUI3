using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;

namespace BlueFire.Toolkit.Sample.WinUI3.Services
{
    public class NavigationService
    {
        private Frame frame;

        public void SetFrame(Frame frame)
        {
            if (this.frame != null)
            {
                this.frame.Navigating -= Frame_Navigating;
                this.frame.Navigated -= Frame_Navigated;
                this.frame.NavigationFailed -= Frame_NavigationFailed;
                this.frame.NavigationStopped -= Frame_NavigationStopped;
            }

            this.frame = frame;

            if (this.frame != null)
            {
                this.frame.Navigating += Frame_Navigating;
                this.frame.Navigated += Frame_Navigated;
                this.frame.NavigationFailed += Frame_NavigationFailed;
                this.frame.NavigationStopped += Frame_NavigationStopped;
            }
        }

        public Frame Frame => frame;

        public int BackStackDepth => frame.BackStackDepth;

        public int CacheSize => frame.CacheSize;

        public bool CanGoBack => frame.CanGoBack;

        public bool CanGoForward => frame.CanGoForward;

        public Type CurrentSourcePageType => frame.CurrentSourcePageType;

        public bool IsNavigationStackEnabled
        {
            get => frame.IsNavigationStackEnabled;
            set => frame.IsNavigationStackEnabled = value;
        }

        public Type SourcePageType => frame.SourcePageType;

        public event NavigatedEventHandler? Navigated;

        public event NavigatingCancelEventHandler? Navigating;

        public event NavigationFailedEventHandler? NavigationFailed;

        public event NavigationStoppedEventHandler? NavigationStopped;

        public void GoBack() => frame.GoBack();

        public void GoBack(NavigationTransitionInfo transitionInfoOverride) => frame.GoBack(transitionInfoOverride);

        public void GoForward() => frame.GoForward();

        public bool Navigate(Type sourcePageType, object parameter) => frame.Navigate(sourcePageType, parameter);

        public bool Navigate(Type sourcePageType, object parameter, NavigationTransitionInfo infoOverride) =>
            frame.Navigate(sourcePageType, parameter, infoOverride);

        public bool NavigateToType(Type sourcePageType, object parameter, FrameNavigationOptions navigationOptions) =>
            frame.NavigateToType(sourcePageType, parameter, navigationOptions);


        private void Frame_Navigated(object sender, NavigationEventArgs e)
        {
            Navigated?.Invoke(this, e);
        }

        private void Frame_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            Navigating?.Invoke(this, e);
        }

        private void Frame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            NavigationFailed?.Invoke(this, e);
        }

        private void Frame_NavigationStopped(object sender, NavigationEventArgs e)
        {
            NavigationStopped?.Invoke(this, e);
        }
    }
}
