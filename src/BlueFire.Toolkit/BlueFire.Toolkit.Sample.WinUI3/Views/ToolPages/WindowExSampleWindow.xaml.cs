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
using BlueFire.Toolkit.WinUI3;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BlueFire.Toolkit.Sample.WinUI3.Views.ToolPages
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    #region Block 2
    public sealed partial class WindowExSampleWindow : WindowEx
    {
        public WindowExSampleWindow()
        {
            this.InitializeComponent();
        }

        protected override void OnDpiChanged(WindowExDpiChangedEventArgs args)
        {
        }

        protected override void OnSizeChanged(WindowExSizeChangedEventArgs args)
        {
        }

        protected override void OnWindowMessageReceived(WindowMessageReceivedEventArgs e)
        {
        }
    }
    #endregion Block 2
}
