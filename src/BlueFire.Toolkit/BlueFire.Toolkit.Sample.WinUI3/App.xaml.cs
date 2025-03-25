using BlueFire.Toolkit.WinUI3;
using BlueFire.Toolkit.WinUI3.Graphics;
using Microsoft.Graphics.Canvas;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinRT;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BlueFire.Toolkit.Sample.WinUI3
{
    [GeneratedComInterface]
    [Guid("D21768E1-23A4-4823-A14B-7C3EBA85D658")]
    public partial interface ID2D1Device1
    {
        [PreserveSig]
        int GetFactory(out nint factory);
    }

    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            //MessageBox(0, $"""
            //    time1: {time1}
            //    time2: {time2}
            //    time3: {time3}
            //    """, "", 0);
            Toolkit.WinUI3.Resources.Localizer.Default.GetResource("MyRect.Fill");

            [DllImport("user32.dll")]
            static extern int MessageBox(nint hWnd, string text, string caption, uint type);

            this.InitializeComponent();
        }

        public static new App Current => (App)Application.Current;

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            m_window.Activate();
        }

        private WindowEx m_window;
    }
}
