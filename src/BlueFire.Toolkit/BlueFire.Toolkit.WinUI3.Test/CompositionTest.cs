using BlueFire.Toolkit.WinUI3.Compositions;

namespace BlueFire.Toolkit.WinUI3.Test
{
    [TestClass]
    public class CompositionTest
    {
        [TestMethod]
        public void CreateCompositor()
        {
            var compositor = WindowsCompositionHelper.Compositor;
            compositor.CreateSpriteVisual();
        }
    }
}