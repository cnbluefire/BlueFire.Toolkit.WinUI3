using BlueFire.Toolkit.Sample.WinUI3.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BlueFire.Toolkit.Sample.WinUI3.Controls
{
    public sealed partial class ToolTitleControl : UserControl
    {
        public ToolTitleControl()
        {
            this.InitializeComponent();
        }

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(ToolTitleControl), new PropertyMetadata("", (s, a) =>
            {
                if (s is ToolTitleControl sender)
                {
                    sender.TitleTextBlock.Text = ((string)a.NewValue) ?? string.Empty;
                }
            }));

        public string Namespace
        {
            get { return (string)GetValue(NamespaceProperty); }
            set { SetValue(NamespaceProperty, value); }
        }

        public static readonly DependencyProperty NamespaceProperty =
            DependencyProperty.Register("Namespace", typeof(string), typeof(ToolTitleControl), new PropertyMetadata("", (s, a) =>
            {
                if (s is ToolTitleControl sender)
                {
                    sender.NamespaceTextBlock.Text = ((string)a.NewValue) ?? string.Empty;
                }
            }));




        public ObservableCollection<ToolSourceFileModel> SourceFiles
        {
            get { return (ObservableCollection<ToolSourceFileModel>)GetValue(SourceFilesProperty); }
            set { SetValue(SourceFilesProperty, value); }
        }

        public static readonly DependencyProperty SourceFilesProperty =
            DependencyProperty.Register("SourceFiles", typeof(ObservableCollection<ToolSourceFileModel>), typeof(ToolTitleControl), new PropertyMetadata(null, (s, a) =>
            {
                if (s is ToolTitleControl sender)
                {
                    var weakThis = new WeakReference(sender);

                    if (a.OldValue is ObservableCollection<ToolSourceFileModel> oldValue)
                    {
                        oldValue.CollectionChanged -= OnCollectionChanged;
                        if (oldValue is INotifyPropertyChanged oldValue2)
                        {
                            oldValue2.PropertyChanged -= OnPropertyChanged;
                        }
                    }

                    sender.UpdateSourceFiles();

                    if (a.NewValue is ObservableCollection<ToolSourceFileModel> newValue)
                    {
                        newValue.CollectionChanged += OnCollectionChanged;
                        if (newValue is INotifyPropertyChanged newValue2)
                        {
                            newValue2.PropertyChanged += OnPropertyChanged;
                        }
                    }

                    void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
                    {
                        var thisRef = (ToolTitleControl?)weakThis.Target;
                        thisRef.UpdateSourceFiles();
                    }

                    void OnCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
                    {
                        var thisRef = (ToolTitleControl?)weakThis.Target;
                        thisRef.UpdateSourceFiles();
                    }

                }
            }));

        private void UpdateSourceFiles()
        {
            ToolSourceCodeItems.ItemsSource = null;
            SamplePageSourceCodeItems.ItemsSource = null;

            SourceButton.Visibility = Visibility.Collapsed;
            ToolSourceCodeTitle.Visibility = Visibility.Collapsed;
            ToolSourceCodeItems.Visibility = Visibility.Collapsed;
            SamplePageSourceCodeTitle.Visibility = Visibility.Collapsed;
            SamplePageSourceCodeItems.Visibility = Visibility.Collapsed;

            var sourceFiles = SourceFiles;

            if (sourceFiles == null || sourceFiles.Count == 0) return;

            SourceButton.Visibility = Visibility.Visible;

            var dict = sourceFiles.GroupBy(c => c.Type)
                .ToDictionary(c => c.Key, c => c.ToArray());

            if (dict.TryGetValue(ToolSourceFileType.Tool, out var sourceFiles1))
            {
                ToolSourceCodeTitle.Visibility = Visibility.Visible;
                ToolSourceCodeItems.Visibility = Visibility.Visible;

                ToolSourceCodeItems.ItemsSource = sourceFiles1;
            }
            if (dict.TryGetValue(ToolSourceFileType.SamplePage, out var sourceFiles2))
            {
                SamplePageSourceCodeTitle.Visibility = Visibility.Visible;
                SamplePageSourceCodeItems.Visibility = Visibility.Visible;

                SamplePageSourceCodeItems.ItemsSource = sourceFiles2;
            }
        }

        private async void FeedbackButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await Launcher.LaunchUriAsync(new Uri("https://github.com/cnbluefire/BlueFire.Toolkit.WinUI3/issues/new"));
            }
            catch { }
        }

    }
}
