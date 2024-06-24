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
using BlueFire.Toolkit.WinUI3.Input;
using Microsoft.UI.Input;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BlueFire.Toolkit.Sample.WinUI3.Views.ToolPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class KeyboardHelperPage : Page
    {
        public KeyboardHelperPage()
        {
            this.InitializeComponent();
        }

        public ToolModel ToolModel => ViewModelLocator.Instance.MainWindowViewModel.AllTools
            .First(c => c.Name == "KeyboardHelper").ToolModel;

        private void SendKeyButton_Click(object sender, RoutedEventArgs e)
        {
            InputBox.Focus(FocusState.Keyboard);

            #region Block 1

            KeyboardHelper.SendKey(VirtualKeys.VK_LSHIFT, false);

            KeyboardHelper.SendKey(VirtualKeys.VK_T, false);
            KeyboardHelper.SendKey(VirtualKeys.VK_T, true);

            KeyboardHelper.SendKey(VirtualKeys.VK_E, false);
            KeyboardHelper.SendKey(VirtualKeys.VK_E, true);

            KeyboardHelper.SendKey(VirtualKeys.VK_S, false);
            KeyboardHelper.SendKey(VirtualKeys.VK_S, true);

            KeyboardHelper.SendKey(VirtualKeys.VK_T, false);
            KeyboardHelper.SendKey(VirtualKeys.VK_T, true);

            KeyboardHelper.SendKey(VirtualKeys.VK_LSHIFT, true);

            #endregion Block 1
        }

        #region Block 2
        private void TextBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            e.Handled = true;
            var textBox = (TextBox)sender;

            var ctrlState = InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control);
            var shiftState = InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift);
            var menuState = InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Menu);
            var lWinState = InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.LeftWindows);
            var rWinState = InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.RightWindows);

            HotKeyModifiers modifiers = 0;
            if ((ctrlState & Windows.UI.Core.CoreVirtualKeyStates.Down) != 0)
                modifiers |= HotKeyModifiers.MOD_CONTROL;
            if ((shiftState & Windows.UI.Core.CoreVirtualKeyStates.Down) != 0)
                modifiers |= HotKeyModifiers.MOD_SHIFT;
            if ((menuState & Windows.UI.Core.CoreVirtualKeyStates.Down) != 0)
                modifiers |= HotKeyModifiers.MOD_ALT;
            if ((lWinState & Windows.UI.Core.CoreVirtualKeyStates.Down) != 0 || (rWinState & Windows.UI.Core.CoreVirtualKeyStates.Down) != 0)
                modifiers |= HotKeyModifiers.MOD_WIN;

            textBox.Text = KeyboardHelper.MapKeyToString(modifiers, (VirtualKeys)e.Key);
        }
        #endregion Block 2
    }
}
