using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32.Foundation;
using PInvoke = Windows.Win32.PInvoke;
using INPUT = Windows.Win32.UI.Input.KeyboardAndMouse.INPUT;
using INPUT_TYPE = Windows.Win32.UI.Input.KeyboardAndMouse.INPUT_TYPE;
using KEYBDINPUT = Windows.Win32.UI.Input.KeyboardAndMouse.KEYBDINPUT;
using KEYBD_EVENT_FLAGS = Windows.Win32.UI.Input.KeyboardAndMouse.KEYBD_EVENT_FLAGS;
using MAP_VIRTUAL_KEY_TYPE = Windows.Win32.UI.Input.KeyboardAndMouse.MAP_VIRTUAL_KEY_TYPE;
using VIRTUAL_KEY = Windows.Win32.UI.Input.KeyboardAndMouse.VIRTUAL_KEY;
using VirtualKey = Windows.System.VirtualKey;
using VirtualKeyModifiers = Windows.System.VirtualKeyModifiers;

namespace BlueFire.Toolkit.WinUI3.Input
{
    public static class KeyboardHelper
    {
        public static unsafe bool SendKey(VirtualKeys key, bool keyUp)
        {
            var inputs = stackalloc INPUT[1]
            {
                new INPUT()
                {
                    type = INPUT_TYPE.INPUT_KEYBOARD,
                    Anonymous = new INPUT._Anonymous_e__Union()
                    {
                        ki = new KEYBDINPUT()
                        {
                            dwFlags = keyUp ? KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP : 0,
                            wVk = (VIRTUAL_KEY)(ushort)key,
                        }
                    }
                }
            };
            return PInvoke.SendInput(1, inputs, System.Runtime.InteropServices.Marshal.SizeOf<INPUT>()) != 0;
        }

        public static string MapKeyToString(HotKeyModifiers modifiers, VirtualKeys key, bool compact = false)
        {
            return HotKeyHelper.MapKeyToString(modifiers, key, compact);
        }
    }
}
