using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueFire.Toolkit.Sample.WinUI3.Controls
{
    public partial class HeaderedContentControl : ContentControl
    {
        public HeaderedContentControl()
        {
            this.DefaultStyleKey = typeof(HeaderedContentControl);

            this.Loaded += (_, _) =>
            {
                UpdateOrientationState();
                UpdateHeaderVisibilityState();
            };
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            var test = GetTemplateChild("HeaderPresenter");
            UpdateOrientationState();
            UpdateHeaderVisibilityState();
        }

        public object Header
        {
            get { return (object)GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }
        public DataTemplate HeaderTemplate
        {
            get { return (DataTemplate)GetValue(HeaderTemplateProperty); }
            set { SetValue(HeaderTemplateProperty, value); }
        }

        public DataTemplateSelector HeaderTemplateSelector
        {
            get { return (DataTemplateSelector)GetValue(HeaderTemplateSelectorProperty); }
            set { SetValue(HeaderTemplateSelectorProperty, value); }
        }

        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register("Header", typeof(object), typeof(HeaderedContentControl), new PropertyMetadata(null, (s, a) =>
            {
                if (s is HeaderedContentControl sender && !Equals(a.NewValue, a.OldValue))
                {
                    sender.UpdateHeaderVisibilityState();
                }
            }));

        public static readonly DependencyProperty HeaderTemplateProperty =
            DependencyProperty.Register("HeaderTemplate", typeof(DataTemplate), typeof(HeaderedContentControl), new PropertyMetadata(null));

        public static readonly DependencyProperty HeaderTemplateSelectorProperty =
            DependencyProperty.Register("HeaderTemplateSelector", typeof(DataTemplateSelector), typeof(HeaderedContentControl), new PropertyMetadata(null));



        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register("Orientation", typeof(Orientation), typeof(HeaderedContentControl), new PropertyMetadata(Orientation.Vertical, (s, a) =>
            {
                if (s is HeaderedContentControl sender && !Equals(a.NewValue, a.OldValue))
                {
                    sender.UpdateOrientationState();
                }
            }));

        private void UpdateHeaderVisibilityState()
        {
            VisualStateManager.GoToState(this, Header is null ? "HeaderCollapsed" : "HeaderVisible", true);
        }

        private void UpdateOrientationState()
        {
            VisualStateManager.GoToState(this, Orientation switch
            {
                Orientation.Vertical => "Vertical",
                _ => "Horizontal",
            }, true);
        }

    }
}
