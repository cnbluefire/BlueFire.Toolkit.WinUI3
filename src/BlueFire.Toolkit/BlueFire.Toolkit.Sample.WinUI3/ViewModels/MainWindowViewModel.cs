using BlueFire.Toolkit.Sample.WinUI3.Models;
using BlueFire.Toolkit.Sample.WinUI3.Services;
using BlueFire.Toolkit.Sample.WinUI3.Views.ToolPages;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueFire.Toolkit.Sample.WinUI3.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly DispatcherQueue dispatcherQueue;

        public MainWindowViewModel(
            DispatcherQueue dispatcherQueue,
            NavigationViewAdapter navigationViewAdapter,
            NavigationService navigationService)
        {
            this.dispatcherQueue = dispatcherQueue;
            NavigationViewAdapter = navigationViewAdapter;
            NavigationService = navigationService;
            InitNavViewMenuItems();

            dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                NavigationViewAdapter.SelectedItem = AllTools[0];
            });
        }

        public NavigationViewAdapter NavigationViewAdapter { get; }

        public NavigationService NavigationService { get; }

        public IReadOnlyList<NavViewItemModel> NavViewMenuItems { get; private set; }

        public IReadOnlyList<NavViewItemModel> AllNavViewMenuItems { get; private set; }

        public IReadOnlyList<NavViewItemModel> AllTools { get; private set; }

        private void InitNavViewMenuItems()
        {
            /*

Controls
AutoScrollView
OpacityMaskView

Window
WindowManager
WindowEx
IconProvider

Input
HotKey
KeyboardHelper

Text
FormattedText
CompositeFontManager

SystemBackdrop
TransparentBackdrop
ColorBackdrop
MaterialCardBackdrop

Resources
Localizer
ResourceExtension

Direct2d
Direct2DInterop

Helpers
Geometry
PackageInfo

WindowDialog
ContentDialog


 */

            const string DefaultIcon = "\uE7C8";

            NavViewMenuItems = new ObservableCollection<NavViewItemModel>()
            {
                //new ("Home", "主页", Symbol.Home, clickAction: () => Task.CompletedTask),
                //new("AllTools", "全部工具", Symbol.AllApps),
                //new NavViewSeparatorModel(),
                new ("Controls", "控件", "\uECAA", subMenuItems: new NavViewItemModel[]
                {
                    new ("AutoScrollView", "AutoScrollView", DefaultIcon, pageType: typeof(AutoScrollViewPage), tool: new ToolModel()
                    {
                        Name = "AutoScrollView",
                        DisplayName = "AutoScrollView",
                        Namespace = "BlueFire.Toolkit.WinUI3.Controls",
                        Description = "A container that can scroll content automatically.",
                        SourceFiles =
                        {
                            new ToolSourceFileModel(ToolSourceFileType.Tool, "/src/BlueFire.Toolkit/BlueFire.Toolkit.WinUI3/Controls/AutoScrollView.cs"),
                            new ToolSourceFileModel(ToolSourceFileType.SamplePage, "/src/BlueFire.Toolkit/BlueFire.Toolkit.Sample.WinUI3/Views/ToolPages/AutoScrollViewPage.xaml"),
                            new ToolSourceFileModel(ToolSourceFileType.SamplePage, "/src/BlueFire.Toolkit/BlueFire.Toolkit.Sample.WinUI3/Views/ToolPages/AutoScrollViewPage.xaml.cs"),
                        }
                    }),
                    new ("OpacityMaskView", "OpacityMaskView", DefaultIcon, pageType: typeof(OpacityMaskViewPage), tool: new ToolModel()
                    {
                        Name = "OpacityMaskView",
                        DisplayName = "OpacityMaskView",
                        Namespace = "BlueFire.Toolkit.WinUI3.Controls",
                        Description = "Gets or sets an opacity mask, as a Brush implementation that is applied to any alpha-channel masking for the child of this element.",
                        SourceFiles =
                        {
                            new ToolSourceFileModel(ToolSourceFileType.Tool, "/src/BlueFire.Toolkit/BlueFire.Toolkit.WinUI3/Controls/OpacityMaskView.cs"),
                            new ToolSourceFileModel(ToolSourceFileType.SamplePage, "/src/BlueFire.Toolkit/BlueFire.Toolkit.Sample.WinUI3/Views/ToolPages/OpacityMaskViewPage.xaml"),
                            new ToolSourceFileModel(ToolSourceFileType.SamplePage, "/src/BlueFire.Toolkit/BlueFire.Toolkit.Sample.WinUI3/Views/ToolPages/OpacityMaskViewPage.xaml.cs"),
                        }

                    }),
                }),
                new ("Window", "窗口", "\uE737", subMenuItems: new NavViewItemModel[]
                {
                    new ("WindowManager", "WindowManager", "\uE737", pageType: typeof(WindowManagerPage), tool: new ToolModel()
                    {
                        Name = "WindowManager",
                        DisplayName = "WindowManager",
                        Namespace = "BlueFire.Toolkit.WinUI3",
                        Description = "Provide more customization features for the window.",
                        SourceFiles =
                        {
                            new ToolSourceFileModel(ToolSourceFileType.Tool, "/src/BlueFire.Toolkit/BlueFire.Toolkit.WinUI3/WindowBase/WindowManager.cs"),
                            new ToolSourceFileModel(ToolSourceFileType.Tool, "/src/BlueFire.Toolkit/BlueFire.Toolkit.WinUI3/WindowBase/WindowManager.Statics.cs"),
                            new ToolSourceFileModel(ToolSourceFileType.SamplePage, "/src/BlueFire.Toolkit/BlueFire.Toolkit.Sample.WinUI3/Views/ToolPages/WindowManagerPage.xaml"),
                            new ToolSourceFileModel(ToolSourceFileType.SamplePage, "/src/BlueFire.Toolkit/BlueFire.Toolkit.Sample.WinUI3/Views/ToolPages/WindowManagerPage.xaml.cs"),
                        }
                    }),
                    new ("WindowEx", "WindowEx", "\uE737", pageType: typeof(WindowExPage), tool: new ToolModel()
                    {
                        Name = "WindowEx",
                        DisplayName = "WindowEx",
                        Namespace = "BlueFire.Toolkit.WinUI3",
                        Description = "Provide more customization features for the window.",
                        SourceFiles =
                        {
                            new ToolSourceFileModel(ToolSourceFileType.Tool, "/src/BlueFire.Toolkit/BlueFire.Toolkit.WinUI3/WindowBase/WindowEx.cs"),
                            new ToolSourceFileModel(ToolSourceFileType.SamplePage, "/src/BlueFire.Toolkit/BlueFire.Toolkit.Sample.WinUI3/Views/ToolPages/WindowExPage.xaml"),
                            new ToolSourceFileModel(ToolSourceFileType.SamplePage, "/src/BlueFire.Toolkit/BlueFire.Toolkit.Sample.WinUI3/Views/ToolPages/WindowExPage.xaml.cs"),
                        }
                    }),
                }),
                new ("Input", "输入", Symbol.Keyboard, subMenuItems: new NavViewItemModel[]
                {
                    new ("HotKey", "热键", Symbol.Keyboard, pageType: typeof(HotKeyPage), tool: new ToolModel()
                    {
                        Name = "HotKey",
                        DisplayName = "热键",
                        Namespace = "BlueFire.Toolkit.WinUI3.Input",
                        Description = "Register and manage Hot Keys.",
                        SourceFiles =
                        {
                            new ToolSourceFileModel(ToolSourceFileType.Tool, "/src/BlueFire.Toolkit/BlueFire.Toolkit.WinUI3.Core/Input/HotKey/HotKeyManager.cs"),
                            new ToolSourceFileModel(ToolSourceFileType.Tool, "/src/BlueFire.Toolkit/BlueFire.Toolkit.WinUI3/Controls/HotKeyInputBox.cs"),
                            new ToolSourceFileModel(ToolSourceFileType.Tool, "/src/BlueFire.Toolkit/BlueFire.Toolkit.WinUI3/Controls/HotKeyInputBox.xaml"),
                            new ToolSourceFileModel(ToolSourceFileType.SamplePage, "/src/BlueFire.Toolkit/BlueFire.Toolkit.Sample.WinUI3/Views/ToolPages/HotKeyPage.xaml"),
                            new ToolSourceFileModel(ToolSourceFileType.SamplePage, "/src/BlueFire.Toolkit/BlueFire.Toolkit.Sample.WinUI3/Views/ToolPages/HotKeyPage.xaml.cs"),
                        }
                    }),
                    new ("KeyboardHelper", "KeyboardHelper", Symbol.Keyboard, pageType: typeof(KeyboardHelperPage), tool: new ToolModel()
                    {
                        Name = "KeyboardHelper",
                        DisplayName = "KeyboardHelper",
                        Namespace = "BlueFire.Toolkit.WinUI3.Input",
                        Description = "Keyboard Helper Class.",
                        SourceFiles =
                        {
                            new ToolSourceFileModel(ToolSourceFileType.Tool, "/src/BlueFire.Toolkit/BlueFire.Toolkit.WinUI3.Core/Input/KeyboardHelper.cs"),
                            new ToolSourceFileModel(ToolSourceFileType.SamplePage, "/src/BlueFire.Toolkit/BlueFire.Toolkit.Sample.WinUI3/Views/ToolPages/KeyboardHelperPage.xaml"),
                            new ToolSourceFileModel(ToolSourceFileType.SamplePage, "/src/BlueFire.Toolkit/BlueFire.Toolkit.Sample.WinUI3/Views/ToolPages/KeyboardHelperPage.xaml.cs"),
                        }
                    }),
                }),
                new ("Text", "文本", Symbol.Character, subMenuItems: new NavViewItemModel[]
                {
                    new ("FormattedText", "FormattedText", Symbol.Character, pageType: typeof(FormattedTextPage), tool: new ToolModel()
                    {
                        Name = "FormattedText",
                        DisplayName = "FormattedText",
                        Namespace = "BlueFire.Toolkit.WinUI3.Text",
                        Description = "Provides low-level control for drawing text.",
                        SourceFiles =
                        {
                            new ToolSourceFileModel(ToolSourceFileType.Tool, "/src/BlueFire.Toolkit/BlueFire.Toolkit.WinUI3/Text/FormattedText/FormattedText.cs"),
                            new ToolSourceFileModel(ToolSourceFileType.SamplePage, "/src/BlueFire.Toolkit/BlueFire.Toolkit.Sample.WinUI3/Views/ToolPages/FormattedTextPage.xaml"),
                            new ToolSourceFileModel(ToolSourceFileType.SamplePage, "/src/BlueFire.Toolkit/BlueFire.Toolkit.Sample.WinUI3/Views/ToolPages/FormattedTextPage.xaml.cs"),
                        }
                    }),
                }),
                new ("SystemBackdrop", "SystemBackdrop", "\uE790", subMenuItems: new NavViewItemModel[]
                {
                    new ("ColorBackdrop", "ColorBackdrop", "\uE790", pageType: typeof(ColorBackdropPage), tool: new ToolModel()
                    {
                        Name = "ColorBackdrop",
                        DisplayName = "ColorBackdrop",
                        Namespace = "BlueFire.Toolkit.WinUI3.SystemBackdrops",
                        Description = "Set the window to a solid color background.",
                        SourceFiles =
                        {
                            new ToolSourceFileModel(ToolSourceFileType.Tool, "/src/BlueFire.Toolkit/BlueFire.Toolkit.WinUI3/SystemBackdrops/ColorBackdrop.cs"),
                            new ToolSourceFileModel(ToolSourceFileType.SamplePage, "/src/BlueFire.Toolkit/BlueFire.Toolkit.Sample.WinUI3/Views/ToolPages/ColorBackdropPage.xaml"),
                            new ToolSourceFileModel(ToolSourceFileType.SamplePage, "/src/BlueFire.Toolkit/BlueFire.Toolkit.Sample.WinUI3/Views/ToolPages/ColorBackdropPage.xaml.cs"),
                        }
                    }),
                    new ("MaterialCardBackdrop", "MaterialCardBackdrop", "\uE790", pageType: typeof(MaterialCardBackdropPage), tool: new ToolModel()
                    {
                        Name = "MaterialCardBackdrop",
                        DisplayName = "MaterialCardBackdrop",
                        Namespace = "BlueFire.Toolkit.WinUI3.SystemBackdrops",
                        Description = "Using a material card as a window background.",
                        SourceFiles =
                        {
                            new ToolSourceFileModel(ToolSourceFileType.Tool, "/src/BlueFire.Toolkit/BlueFire.Toolkit.WinUI3/SystemBackdrops/MaterialCardBackdrop.cs"),
                            new ToolSourceFileModel(ToolSourceFileType.SamplePage, "/src/BlueFire.Toolkit/BlueFire.Toolkit.Sample.WinUI3/Views/ToolPages/MaterialCardBackdropPage.xaml"),
                            new ToolSourceFileModel(ToolSourceFileType.SamplePage, "/src/BlueFire.Toolkit/BlueFire.Toolkit.Sample.WinUI3/Views/ToolPages/MaterialCardBackdropPage.xaml.cs"),
                        }
                    }),
                }),
                //new ("Resources", "资源管理", "\uE82D", subMenuItems: new NavViewItemModel[]
                //{
                //    new ("Localizer", "Localizer", "\uE82D"),
                //    new ("ResourceExtension", "ResourceExtension", "\uE82D"),
                //}),
                //new ("Direct2DInterop", "Direct2DInterop", Symbol.Sync),
                new ("Helpers", "帮助类", "\uECAD", subMenuItems: new NavViewItemModel[]
                {
                    //new ("Geometry", "Geometry", "\uEE56"),
                    new ("PackageInfo", "PackageInfo", "\uE7B8", pageType: typeof(PackageInfoPage), tool: new ToolModel()
                    {
                        Name = "PackageInfo",
                        DisplayName = "PackageInfo",
                        Namespace = "BlueFire.Toolkit.WinUI3.Extensions",
                        Description = "Get software package information.",
                        SourceFiles =
                        {
                            new ToolSourceFileModel(ToolSourceFileType.Tool, "/src/BlueFire.Toolkit/BlueFire.Toolkit.WinUI3.Core/Extensions/PackageInfo.cs"),
                            new ToolSourceFileModel(ToolSourceFileType.SamplePage, "/src/BlueFire.Toolkit/BlueFire.Toolkit.Sample.WinUI3/Views/ToolPages/PackageInfoPage.xaml"),
                            new ToolSourceFileModel(ToolSourceFileType.SamplePage, "/src/BlueFire.Toolkit/BlueFire.Toolkit.Sample.WinUI3/Views/ToolPages/PackageInfoPage.xaml.cs"),
                        }
                    }),
                    //new ("WindowDialog", "WindowDialog", "\uE737"),
                    //new("ContentDialog", "ContentDialog", "\uE737"),
                }),
            };

            AllNavViewMenuItems = NavViewMenuItems.SelectMany(c => Flatten(c)).Where(c => !string.IsNullOrEmpty(c.Name)).ToList();

            AllTools = AllNavViewMenuItems.SelectMany(c => Flatten(c)).Where(c => c.PageType != null).ToList();

            static IEnumerable<NavViewItemModel> Flatten(NavViewItemModel _item)
            {
                yield return _item;

                if (_item.SubMenuItems != null && _item.SubMenuItems.Count > 0)
                {
                    foreach (var _subItem in _item.SubMenuItems)
                    {
                        foreach (var _subItem2 in Flatten(_subItem))
                        {
                            yield return _subItem2;
                        }
                    }
                }
            }
        }
    }
}
