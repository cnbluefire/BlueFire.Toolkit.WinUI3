using BlueFire.Toolkit.WinUI3.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueFire.Toolkit.WinUI3.Media
{
    public static class IconElementExtensions
    {
        public static IconSource GetIconSource(IconSourceElement obj)
        {
            return (IconSource)obj.GetValue(IconSourceProperty);
        }

        public static void SetIconSource(IconSourceElement obj, IconSource value)
        {
            obj.SetValue(IconSourceProperty, value);
        }

        public static readonly DependencyProperty IconSourceProperty =
            DependencyProperty.RegisterAttached("IconSource", typeof(IconSource), typeof(IconElementExtensions), new PropertyMetadata(null, (s, a) =>
            {
                if (s is IconSourceElement sender && !Equals(a.NewValue, a.OldValue))
                {
                    if (a.NewValue is PathIconSource pathIconSource)
                    {
                        if (sender.IconSource is PathIconSource oldSource)
                        {
                            oldSource.ClearValue(IconSource.ForegroundProperty);
                        }

                        var newSource = new PathIconSource
                        {
                            Data = pathIconSource.Data?.CloneGeometry(),
                        };

                        sender.IconSource = newSource;
                    }
                    else
                    {
                        sender.IconSource = (IconSource)a.NewValue;
                    }
                }
            }));
    }
}