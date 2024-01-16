using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using WinRT;
using System.Runtime.InteropServices;
using Microsoft.UI;

namespace BlueFire.Toolkit.WinUI3.SystemBackdrops
{
    public class TransparentBackdrop : SystemBackdrop
    {
        private Dictionary<WindowId, TransparentBackdropControllerEntry> entries = new Dictionary<WindowId, TransparentBackdropControllerEntry>();

        protected override void OnTargetConnected(ICompositionSupportsSystemBackdrop connectedTarget, XamlRoot xamlRoot)
        {
            var windowId = xamlRoot.ContentIslandEnvironment.AppWindowId;

            var test = GetIUnknownPtr(connectedTarget);

            lock (entries)
            {
                if (entries.ContainsKey(windowId)) return;

                var entry = new TransparentBackdropControllerEntry(connectedTarget, windowId);
                entry.OnClear += Entry_OnClear;
                entries[windowId] = entry;
            }
        }

        protected override void OnTargetDisconnected(ICompositionSupportsSystemBackdrop disconnectedTarget)
        {
            lock (entries)
            {
                TransparentBackdropControllerEntry? entry = null;

                foreach (var item in entries.Values)
                {
                    if (GetIUnknownPtr(item.ConnectedTarget) == GetIUnknownPtr(disconnectedTarget))
                    {
                        entry = item;
                        break;
                    }
                }

                if (entry != null)
                {
                    entries.Remove(entry.WindowId);
                    entry.OnClear -= Entry_OnClear;
                    entry.Dispose();
                }
            }
        }

        private void Entry_OnClear(object? sender, EventArgs e)
        {
            var entry = (TransparentBackdropControllerEntry)sender!;
            lock (entries)
            {
                entries.Remove(entry.WindowId);
                entry.OnClear -= Entry_OnClear;
                entry.Dispose();
            }
        }

        private static nint GetIUnknownPtr(ICompositionSupportsSystemBackdrop? target)
        {
            if (target is null) return 0;

            if (target is IWinRTObject winRtObj) return winRtObj.NativeObject.ThisPtr;

            var ptr = Marshal.GetIUnknownForObject(target);
            Marshal.Release(ptr);
            return ptr;
        }

        public Windows.UI.Color BackgroundColor
        {
            get { return (Windows.UI.Color)GetValue(BackgroundColorProperty); }
            set { SetValue(BackgroundColorProperty, value); }
        }

        public static readonly DependencyProperty BackgroundColorProperty =
            DependencyProperty.Register("BackgroundColor", typeof(Windows.UI.Color), typeof(TransparentBackdrop), new PropertyMetadata(Windows.UI.Color.FromArgb(0, 255, 255, 255), (s, a) =>
            {
                if (s is TransparentBackdrop sender && !Equals(a.NewValue, a.OldValue))
                {
                    var color = (Windows.UI.Color)a.NewValue;

                    lock (sender.entries)
                    {
                        foreach (var item in sender.entries.Values)
                        {
                            item.BackgroundColor = color;
                        }
                    }
                }
            }));
    }
}
