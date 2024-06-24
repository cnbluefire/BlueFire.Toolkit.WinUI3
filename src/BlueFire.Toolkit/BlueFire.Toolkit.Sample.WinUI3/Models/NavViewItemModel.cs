using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueFire.Toolkit.Sample.WinUI3.Models
{
    public partial class NavViewItemModel : ObservableObject
    {
        public NavViewItemModel() { }

        public NavViewItemModel(
            string name,
            string displayName,
            object? icon = null,
            Type? pageType = null,
            IEnumerable<NavViewItemModel>? subMenuItems = null,
            Func<Task>? clickAction = null,
            ToolModel? tool = null) : this()
        {
            this.Name = name;
            this.DisplayName = displayName;
            this.IconSource = MapToIconSource(icon);
            this.PageType = pageType;
            this.SubMenuItems = subMenuItems != null ? new ObservableCollection<NavViewItemModel>(subMenuItems) : null;
            this.ClickAction = clickAction;
            this.ToolModel = tool;
        }

        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private string _displayName;

        [ObservableProperty]
        private Func<Task>? _clickAction;

        [ObservableProperty]
        private Type? _pageType;

        [ObservableProperty]
        private IconSource? _iconSource;

        [ObservableProperty]
        private ObservableCollection<NavViewItemModel> _subMenuItems;

        [ObservableProperty]
        private ToolModel _toolModel;

        public NavViewItemModel Self => this;

        public bool SelectsOnInvoked => PageType != null;

        public override string ToString() => DisplayName;


        public static implicit operator NavViewItemModel(string name)
        {
            if (name.All(c => c == '-')) return new NavViewSeparatorModel();
            return null;
        }

        private static IconSource? MapToIconSource(object? icon) =>
            icon switch
            {
                IconSource iconSource => iconSource,
                Symbol symbol => new SymbolIconSource()
                {
                    Symbol = symbol
                },
                string glyph when glyph.Length < 5 => new FontIconSource()
                {
                    Glyph = glyph
                },
                _ => null
            };
    }

    public class NavViewSeparatorModel : NavViewItemModel { }
}
