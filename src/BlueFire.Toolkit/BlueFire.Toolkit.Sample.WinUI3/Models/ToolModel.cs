using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueFire.Toolkit.Sample.WinUI3.Models
{
    public partial class ToolModel : ObservableObject
    {
        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private string _displayName;

        [ObservableProperty]
        private string _namespace;

        [ObservableProperty]
        private string _description;

        public ObservableCollection<ToolSourceFileModel> SourceFiles { get; } = new ObservableCollection<ToolSourceFileModel>();
    }

    public class ToolSourceFileModel
    {
        public ToolSourceFileModel(ToolSourceFileType type, string path)
        {
            Type = type;
            Path = path;
        }

        public ToolSourceFileType Type { get; }

        public string Path { get; }

        public Uri? Uri
        {
            get
            {
                var path = Path;
                if (string.IsNullOrEmpty(path)) return null;

                path = path.TrimStart('/');
                return new Uri($"https://github.com/cnbluefire/BlueFire.Toolkit.WinUI3/blob/main/{path}");
            }
        }

        public string Name
        {
            get
            {
                var idx = Path.LastIndexOf('/');
                if (idx == -1 || idx == Path.Length - 1) return string.Empty;
                return Path[(idx + 1)..];
            }
        }
    };

    public enum ToolSourceFileType
    {
        Tool,
        SamplePage
    }
}
