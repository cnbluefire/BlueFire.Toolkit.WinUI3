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
using BlueFire.Toolkit.Sample.WinUI3.Models;
using BlueFire.Toolkit.Sample.WinUI3.ViewModels;
using BlueFire.Toolkit.WinUI3;
using System.Diagnostics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BlueFire.Toolkit.Sample.WinUI3.Views.ToolPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class WindowManagerPage : Page
    {
        public WindowManagerPage()
        {
            this.InitializeComponent();
        }

        public ToolModel ToolModel => ViewModelLocator.Instance.MainWindowViewModel.AllTools
            .First(c => c.Name == "WindowManager").ToolModel;

        /*
         
        #region Block 1

        var manager = WindowManager.Get(myWindow);
        manager.MinWidth = 100;
        manager.MinHeight = 100;
        manager.MaxWidth = 800;
        manager.MaxHeight = 800;

        manager.WindowStyle &= ~(WindowManager.WindowStyleFlags.WS_MAXIMIZEBOX 
            | WindowManager.WindowStyleFlags.WS_MINIMIZEBOX
            | WindowManager.WindowStyleFlags.WS_MAXIMIZE
            | WindowManager.WindowStyleFlags.WS_MINIMIZE);

        #endregion Block 1


        #region Block 2

        var manager = WindowManager.Get(myWindow);
        manager.WindowMessageReceived += Manager_WindowMessageReceived;

        void Manager_WindowMessageReceived(WindowManager sender, WindowMessageReceivedEventArgs e)
        {
            const uint WM_SIZE = 0x0005;

            const uint SIZE_RESTORED = 0;
            const uint SIZE_MINIMIZED = 1;
            const uint SIZE_MAXIMIZED = 2;
            const uint SIZE_MAXSHOW = 3;
            const uint SIZE_MAXHIDE = 4;

            if (e.MessageId == WM_SIZE)
            {
                var state = e.WParam switch
                {
                    SIZE_RESTORED => nameof(SIZE_RESTORED),
                    SIZE_MINIMIZED => nameof(SIZE_MINIMIZED),
                    SIZE_MAXIMIZED => nameof(SIZE_MAXIMIZED),
                    SIZE_MAXSHOW => nameof(SIZE_MAXSHOW),
                    SIZE_MAXHIDE => nameof(SIZE_MAXHIDE),
                    _ => string.Empty
                };

                var newWidth = e.LParam & 0xFFFF;
                var newHeight = e.LParam >> 16;

                Debug.WriteLine($"state: {state}, size: ({newWidth}, {newHeight})");
            }
        }

        #endregion Block 2

        #region Block 3

        var manager = WindowManager.Get(myWindow);
        manager.UseDefaultIcon = true;

        #endregion Block 3

         */
    }
}
