using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using WinRT;
using System.Runtime.InteropServices;
using Microsoft.UI;

namespace BlueFire.Toolkit.WinUI3.SystemBackdrops
{
    /// <summary>
    /// Transparent backdrop for window.
    /// </summary>
    public class TransparentBackdrop : SystemBackdrop
    {
        private Dictionary<WindowId, TransparentBackdropControllerEntry> entries = new Dictionary<WindowId, TransparentBackdropControllerEntry>();
        
        internal IReadOnlyCollection<TransparentBackdropControllerEntry> ControllerEntries => entries.Values;

        protected override void OnTargetConnected(ICompositionSupportsSystemBackdrop connectedTarget, XamlRoot xamlRoot)
        {
            var windowId = xamlRoot.ContentIslandEnvironment.AppWindowId;

            lock (entries)
            {
                if (entries.ContainsKey(windowId)) return;

                var entry = CreateControllerEntry(connectedTarget, xamlRoot);
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

        internal virtual TransparentBackdropControllerEntry CreateControllerEntry(ICompositionSupportsSystemBackdrop connectedTarget, XamlRoot xamlRoot) => new TransparentBackdropControllerEntry(connectedTarget, xamlRoot.ContentIslandEnvironment.AppWindowId);
    }
}
