using BlueFire.Toolkit.WinUI3.Extensions;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueFire.Toolkit.WinUI3.SystemBackdrops
{
    public class WindowBackdropBase : SystemBackdrop
    {
        protected override void OnTargetConnected(ICompositionSupportsSystemBackdrop connectedTarget, XamlRoot xamlRoot)
        {
            base.OnTargetConnected(connectedTarget, xamlRoot);
            this.OnTargetConnected(xamlRoot.ContentIslandEnvironment.AppWindowId, connectedTarget, xamlRoot);
        }

        protected virtual void OnTargetConnected(WindowId windowId, ICompositionSupportsSystemBackdrop connectedTarget, XamlRoot xamlRoot) { }

        protected override void OnDefaultSystemBackdropConfigurationChanged(ICompositionSupportsSystemBackdrop target, XamlRoot xamlRoot)
        {
            //base.OnDefaultSystemBackdropConfigurationChanged(target, xamlRoot);
        }
    }
}
