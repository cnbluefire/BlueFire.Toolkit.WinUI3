using BlueFire.Toolkit.WinUI3.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueFire.Toolkit.WinUI3.Test
{
    [TestClass]
    public class ResourcesTest
    {
        [UITestMethod]
        public void TestLocalizer()
        {
            Localizer.Default.Language = "zh-Hans";
            Assert.AreEqual(Localizer.Default.GetLocalizedText("/LanguageName.Text", null), "简体中文");
            Localizer.Default.Language = "en-US";
            Assert.AreEqual(Localizer.Default.GetLocalizedText("/LanguageName.Text", null), "English");
        }
    }
}
