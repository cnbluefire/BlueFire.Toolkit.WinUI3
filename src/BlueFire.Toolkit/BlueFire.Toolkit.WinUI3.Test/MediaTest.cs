using BlueFire.Toolkit.WinUI3.Extensions;
using BlueFire.Toolkit.WinUI3.Media;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueFire.Toolkit.WinUI3.Test
{
    [TestClass]
    public class MediaTest
    {
        [TestMethod]
        public async Task CreateSurfaceLoader()
        {
            var loader = CompositionSurfaceLoader.StartLoadFromUri(new Uri("https://www.microsoft.com/favicon.ico?v2"));
            var tcs = new TaskCompletionSource();

            loader.LoadCompleted += (s, a) =>
            {
                if (a.Status == Microsoft.UI.Xaml.Media.LoadedImageSourceLoadStatus.Success)
                {
                    tcs.SetResult();
                }
                else
                {
                    tcs.SetException(a.Exception ?? new Exception());
                }
            };

            await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));

            Assert.AreNotEqual(loader.NaturalSize.Width, 0);
            Assert.AreNotEqual(loader.NaturalSize.Height, 0);
        }

        //[TestMethod]
        public void CloneGeometry()
        {
            var geometry = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), "M0 0L1 1");

            var geometry2 = GeometryExtensions.CloneGeometry(geometry);
            
            Assert.AreEqual(((PathGeometry)geometry2!).Figures[0].StartPoint, new Windows.Foundation.Point(0, 0));
            Assert.AreEqual(((LineSegment)((PathGeometry)geometry2).Figures[0].Segments[0]).Point, new Windows.Foundation.Point(1, 1));
        }
    }
}
