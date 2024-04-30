using BlueFire.Toolkit.WinUI3.SystemBackdrops;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueFire.Toolkit.WinUI3.SystemBackdrops
{
    public class ColorBackdrop : TransparentBackdrop
    {
        /// <summary>
        /// Window background color. Can be a translucent color.
        /// </summary>
        public Windows.UI.Color BackgroundColor
        {
            get { return (Windows.UI.Color)GetValue(BackgroundColorProperty); }
            set { SetValue(BackgroundColorProperty, value); }
        }

        public static readonly DependencyProperty BackgroundColorProperty =
            DependencyProperty.Register("BackgroundColor", typeof(Windows.UI.Color), typeof(TransparentBackdrop), new PropertyMetadata(Windows.UI.Color.FromArgb(0, 255, 255, 255), (s, a) =>
            {
                if (s is ColorBackdrop sender && !Equals(a.NewValue, a.OldValue))
                {
                    var color = (Windows.UI.Color)a.NewValue;

                    lock (sender.ControllerEntries)
                    {
                        foreach (var item in sender.ControllerEntries.OfType<ColorBackdropControllerEntry>())
                        {
                            item.BackgroundColor = color;
                        }
                    }
                }
            }));

        internal override TransparentBackdropControllerEntry CreateControllerEntry(ICompositionSupportsSystemBackdrop connectedTarget, XamlRoot xamlRoot)
        {
            return new ColorBackdropControllerEntry(connectedTarget, xamlRoot.ContentIslandEnvironment.AppWindowId);
        }

        private class ColorBackdropControllerEntry : TransparentBackdropControllerEntry
        {
            private Windows.UI.Composition.CompositionColorBrush? colorBrush;

            internal ColorBackdropControllerEntry(ICompositionSupportsSystemBackdrop connectedTarget, WindowId windowId) : base(connectedTarget, windowId)
            {
            }

            protected override void OnAttached(ICompositionSupportsSystemBackdrop connectedTarget, WindowId windowId)
            {
                base.OnAttached(connectedTarget, windowId);

                colorBrush = Compositions.WindowsCompositionHelper.Compositor.CreateColorBrush(Windows.UI.Color.FromArgb(0, 255, 255, 255));
                connectedTarget.SystemBackdrop = colorBrush;
            }

            protected override void OnDetached(ICompositionSupportsSystemBackdrop connectedTarget, WindowId windowId)
            {
                base.OnDetached(connectedTarget, windowId);

                if (!CloseRequested)
                {
                    connectedTarget.SystemBackdrop = null;
                    colorBrush?.Dispose();
                    colorBrush = null;
                }
                else
                {
                    Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                    {
                        colorBrush?.Dispose();
                        colorBrush = null;
                    });
                }
            }


            internal Windows.UI.Color BackgroundColor
            {
                get => colorBrush?.Color ?? Windows.UI.Color.FromArgb(0, 255, 255, 255);
                set
                {
                    if (colorBrush != null)
                    {
                        colorBrush.Color = value;
                    }
                }
            }

        }
    }
}
