using ColorCode;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueFire.Toolkit.Sample.WinUI3.Utils
{
    public static class CodeTextHelper
    {
        public static Block? CreateCSharpCodeBlock(string code, ElementTheme theme) =>
            CreateCodeBlockCore(code, theme, Languages.CSharp);

        public static Block? CreateXamlCodeBlock(string code, ElementTheme theme) =>
            CreateCodeBlockCore(code, theme, Languages.Xml);

        private static Block? CreateCodeBlockCore(string code, ElementTheme theme, ILanguage language)
        {
            var formatter = new RichTextBlockFormatter(theme);
            var paragraph = new Paragraph()
            {
                FontSize = 14,
            };

            try
            {
                formatter.FormatInlines(code, language, paragraph.Inlines);
            }
            catch { paragraph.Inlines.Clear(); }
            return paragraph.Inlines.Count > 0 ? paragraph : null;
        }
    }
}
