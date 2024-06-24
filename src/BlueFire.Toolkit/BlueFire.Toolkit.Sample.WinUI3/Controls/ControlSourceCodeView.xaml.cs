using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Markup;
using System.Text;
using BlueFire.Toolkit.Sample.WinUI3.Utils;
using Microsoft.UI.Xaml.Documents;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.UI.Xaml.Media.Animation;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BlueFire.Toolkit.Sample.WinUI3.Controls
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [ContentProperty(Name = nameof(Child))]
    public sealed partial class ControlSourceCodeView : UserControl
    {
        public ControlSourceCodeView()
        {
            this.InitializeComponent();
            this.SourceCodesList.ItemTemplateSelector = new CodeBlockTemplateSelector()
            {
                CodeBlockTemplate = (DataTemplate)this.SourceCodesList.Resources["CodeBlockTemplate"],
                SeparatorTemplate = (DataTemplate)this.SourceCodesList.Resources["SeparatorTemplate"],
            };

            this.ActualThemeChanged += (s, a) =>
            {
                UpdateSourceCodeText();
            };
        }

        private bool updateSourceCodeTextFlag;

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(ControlSourceCodeView), new PropertyMetadata(""));

        public UIElement Child
        {
            get { return (UIElement)GetValue(ChildProperty); }
            set { SetValue(ChildProperty, value); }
        }

        public static readonly DependencyProperty ChildProperty =
            DependencyProperty.Register("Child", typeof(UIElement), typeof(ControlSourceCodeView), new PropertyMetadata(null, (s, a) =>
            {
                if (s is ControlSourceCodeView sender)
                {
                    sender.ChildBorder.Visibility = (a.NewValue is FrameworkElement) ? Visibility.Visible : Visibility.Collapsed;
                    sender.SourceCodeExpander.CornerRadius = (a.NewValue is FrameworkElement) ? new CornerRadius(0, 0, 8, 8) : new CornerRadius(8);
                }
            }));



        public string ToolName
        {
            get { return (string)GetValue(ToolNameProperty); }
            set { SetValue(ToolNameProperty, value); }
        }

        public static readonly DependencyProperty ToolNameProperty =
            DependencyProperty.Register("ToolName", typeof(string), typeof(ControlSourceCodeView), new PropertyMetadata("", (s, a) =>
            {
                if (s is ControlSourceCodeView sender)
                {
                    sender.updateSourceCodeTextFlag = true;
                    sender.DispatcherQueue.TryEnqueue(() =>
                    {
                        sender.UpdateSourceCodeText();
                    });
                }
            }));



        public string XamlBlocks
        {
            get { return (string)GetValue(XamlBlocksProperty); }
            set { SetValue(XamlBlocksProperty, value); }
        }

        public static readonly DependencyProperty XamlBlocksProperty =
            DependencyProperty.Register("XamlBlocks", typeof(string), typeof(ControlSourceCodeView), new PropertyMetadata("", (s, a) =>
            {
                if (s is ControlSourceCodeView sender)
                {
                    sender.updateSourceCodeTextFlag = true;
                    sender.DispatcherQueue.TryEnqueue(() =>
                    {
                        sender.UpdateSourceCodeText();
                    });
                }
            }));

        public string CSharpBlocks
        {
            get { return (string)GetValue(CSharpBlocksProperty); }
            set { SetValue(CSharpBlocksProperty, value); }
        }

        public static readonly DependencyProperty CSharpBlocksProperty =
            DependencyProperty.Register("CSharpBlocks", typeof(string), typeof(ControlSourceCodeView), new PropertyMetadata("", (s, a) =>
            {
                if (s is ControlSourceCodeView sender)
                {
                    sender.updateSourceCodeTextFlag = true;
                    sender.DispatcherQueue.TryEnqueue(() =>
                    {
                        sender.UpdateSourceCodeText();
                    });
                }
            }));



        public bool IsSourceCodeExpanded
        {
            get { return (bool)GetValue(IsSourceCodeExpandedProperty); }
            set { SetValue(IsSourceCodeExpandedProperty, value); }
        }

        public static readonly DependencyProperty IsSourceCodeExpandedProperty =
            DependencyProperty.Register("IsSourceCodeExpanded", typeof(bool), typeof(ControlSourceCodeView), new PropertyMetadata(false));



        private void UpdateSourceCodeText()
        {
            if (!updateSourceCodeTextFlag) return;
            updateSourceCodeTextFlag = false;

            SourceCodesList.ItemsSource = null;

            if (string.IsNullOrEmpty(ToolName)) return;
            if (string.IsNullOrEmpty(XamlBlocks) && string.IsNullOrEmpty(CSharpBlocks)) return;

            var xamlCodes = GetCodeBlocks(ToolName, XamlBlocks.Split(',').Select(c => c.Trim()), ToolPageCodeBlockHelper.FileType.Xaml);
            var csharpCodes = GetCodeBlocks(ToolName, CSharpBlocks.Split(',').Select(c => c.Trim()), ToolPageCodeBlockHelper.FileType.CSharp);

            var list = xamlCodes.Select(c => new CodeBlockModel()
            {
                Type = "Xaml",
                FileType = ToolPageCodeBlockHelper.FileType.Xaml,
                Code = c
            })
            .Concat(csharpCodes.Select(c => new CodeBlockModel()
            {
                Type = "C#",
                FileType = ToolPageCodeBlockHelper.FileType.CSharp,
                Code = c
            })).ToList();

            for (int i = list.Count - 1; i > 0; i--)
            {
                list.Insert(i, new CodeBlockModel() { Type = "---" });
            }

            SourceCodesList.ItemsSource = list;

            static string GetCode(string _toolName, string _blockName, ToolPageCodeBlockHelper.FileType _fileType)
            {
                if (string.IsNullOrEmpty(_blockName)) return null;
                return ToolPageCodeBlockHelper.GetFileBlockContent(_toolName, _blockName, _fileType);
            }

            static IReadOnlyList<string> GetCodeBlocks(string _toolName, IEnumerable<string> _codeBlocks, ToolPageCodeBlockHelper.FileType _fileType)
            {
                return _codeBlocks
                    .Select(c => GetCode(_toolName, c, _fileType))
                    .Where(c => c != null)
                    .ToArray();
            }
        }


        private void CodeTextBlock_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateTextBlockContent(sender as TextBlock);
        }

        private void CodeTextBlock_ActualThemeChanged(FrameworkElement sender, object args)
        {
            UpdateTextBlockContent(sender as TextBlock);
        }

        private static void UpdateTextBlockContent(TextBlock textBlock)
        {
            if (textBlock == null) return;
            textBlock.Inlines.Clear();

            if (textBlock.Tag is CodeBlockModel model)
            {
                var blocks = model.FileType switch
                {
                    ToolPageCodeBlockHelper.FileType.Xaml => CodeTextHelper.CreateXamlCodeBlock(model.Code, textBlock.ActualTheme),
                    ToolPageCodeBlockHelper.FileType.CSharp => CodeTextHelper.CreateCSharpCodeBlock(model.Code, textBlock.ActualTheme),
                    _ => null
                };

                if (blocks is Paragraph paragraph)
                {
                    var list = paragraph.Inlines.ToList();
                    paragraph.Inlines.Clear();

                    foreach (var item in list)
                    {
                        textBlock.Inlines.Add(item);
                    }
                }
            }

        }

        private async void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            var animation = (Storyboard)((Button)sender).Resources["CopiedAnimation"];
            if (animation.GetCurrentState() != ClockState.Stopped)
            {
                animation.Stop();
            }

            ((Button)sender).IsEnabled = false;

            for (int i = 0; i < 3; i++)
            {
                try
                {
                    if (((Button)sender).Tag is string code)
                    {
                        var dataPackage = new DataPackage();
                        dataPackage.RequestedOperation = DataPackageOperation.Copy;
                        dataPackage.SetText(code);
                        Clipboard.SetContent(dataPackage);
                        Clipboard.Flush();

                        animation.Begin();
                    }
                    break;
                }
                catch
                {
                    await Task.Delay(100);
                }
            }

            ((Button)sender).IsEnabled = true;
        }

        private class CodeBlockModel
        {
            public string Type { get; set; }

            public ToolPageCodeBlockHelper.FileType FileType { get; set; }

            public string Code { get; set; }
        }

        private class CodeBlockTemplateSelector : DataTemplateSelector
        {
            public DataTemplate SeparatorTemplate { get; set; }

            public DataTemplate CodeBlockTemplate { get; set; }

            protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
            {
                if (item is CodeBlockModel model && model.Type?.StartsWith("---") is true)
                {
                    return SeparatorTemplate;
                }

                return CodeBlockTemplate;
            }
        }
    }
}
