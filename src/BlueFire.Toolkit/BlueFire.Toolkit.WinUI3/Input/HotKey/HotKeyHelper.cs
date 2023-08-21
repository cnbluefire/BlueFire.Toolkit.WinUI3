using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
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
    internal static class HotKeyHelper
    {
        private static Dictionary<VirtualKeys, string> keyNames = new Dictionary<VirtualKeys, string>()
        {
            [VirtualKeys.VK_PRIOR] = "Page Up",
            [VirtualKeys.VK_NEXT] = "Page Down",
            [VirtualKeys.VK_HOME] = "Home",
            [VirtualKeys.VK_END] = "End",
            [VirtualKeys.VK_LEFT] = "←",
            [VirtualKeys.VK_RIGHT] = "→",
            [VirtualKeys.VK_UP] = "↑",
            [VirtualKeys.VK_DOWN] = "↓",

            [VirtualKeys.VK_MULTIPLY] = "Num *",
            [VirtualKeys.VK_ADD] = "Num +",
            [VirtualKeys.VK_SUBTRACT] = "Num -",
            [VirtualKeys.VK_DECIMAL] = "Num .",
            [VirtualKeys.VK_DIVIDE] = "Num /",

            //[VirtualKeys.VK_OEM_PLUS] = "+",
            //[VirtualKeys.VK_OEM_COMMA] = ",",
            //[VirtualKeys.VK_OEM_MINUS] = "-",
            //[VirtualKeys.VK_OEM_PERIOD] = ".",
            //[VirtualKeys.VK_OEM_1] = ";",
            //[VirtualKeys.VK_OEM_2] = "?",
            //[VirtualKeys.VK_OEM_3] = "~",
            //[VirtualKeys.VK_OEM_4] = "[",
            //[VirtualKeys.VK_OEM_5] = "\\",
            //[VirtualKeys.VK_OEM_6] = "]",
            //[VirtualKeys.VK_OEM_7] = "'",
        };

        private static Dictionary<HotKeyModifiers, string[]> modifierNames = new Dictionary<HotKeyModifiers, string[]>();
        private static Dictionary<HotKeyModifiers, VirtualKeys[]> modifierKeys = new Dictionary<HotKeyModifiers, VirtualKeys[]>();

        public static string[] MapKeyToString(HotKeyModifiers modifiers)
        {
            HotKeyModifiers modifiers2 = default;

            int capacity = 0;

            if ((modifiers & HotKeyModifiers.MOD_CONTROL) != 0)
            {
                capacity++;
                modifiers2 |= HotKeyModifiers.MOD_CONTROL;
            }
            if ((modifiers & HotKeyModifiers.MOD_ALT) != 0)
            {
                capacity++;
                modifiers2 |= HotKeyModifiers.MOD_ALT;
            }
            if ((modifiers & HotKeyModifiers.MOD_SHIFT) != 0)
            {
                capacity++;
                modifiers2 |= HotKeyModifiers.MOD_SHIFT;
            }
            if ((modifiers & HotKeyModifiers.MOD_WIN) != 0)
            {
                capacity++;
                modifiers2 |= HotKeyModifiers.MOD_WIN;
            }

            if (capacity == 0) return Array.Empty<string>();

            lock (modifierNames)
            {
                if (modifierNames.TryGetValue(modifiers2, out var value)) return value;

                value = new string[capacity];
                var idx = 0;

                if ((modifiers2 & HotKeyModifiers.MOD_CONTROL) != 0) value[idx++] = "Ctrl";
                if ((modifiers2 & HotKeyModifiers.MOD_ALT) != 0) value[idx++] = "Alt";
                if ((modifiers2 & HotKeyModifiers.MOD_SHIFT) != 0) value[idx++] = "Shift";
                if ((modifiers2 & HotKeyModifiers.MOD_WIN) != 0) value[idx++] = "Win";

                return value;
            }
        }

        public static unsafe string MapKeyToString(VirtualKeys key)
        {
            lock (keyNames)
            {
                if (keyNames.TryGetValue(key, out var name)) return name;

                else if (key == VirtualKeys.VK_OEM_PLUS        // 加号 +
                    || key == VirtualKeys.VK_OEM_COMMA    // 逗号 ,
                    || key == VirtualKeys.VK_OEM_MINUS    // 减号 -
                    || key == VirtualKeys.VK_OEM_PERIOD   // 句点 .
                    || key == VirtualKeys.VK_OEM_1        // 分号 ;
                    || key == VirtualKeys.VK_OEM_2        // 问号 ?
                    || key == VirtualKeys.VK_OEM_3        // 波浪线号 ~
                    || key == VirtualKeys.VK_OEM_4        // 左中括号 [
                    || key == VirtualKeys.VK_OEM_5        // 反斜线 \
                    || key == VirtualKeys.VK_OEM_6        // 右中括号 ]
                    || key == VirtualKeys.VK_OEM_7)       // 单引号 '
                {

                    var scanCode = PInvoke.MapVirtualKey((uint)key, MAP_VIRTUAL_KEY_TYPE.MAPVK_VK_TO_VSC);

                    var pString = stackalloc char[65];

                    var length = PInvoke.GetKeyNameText((int)(scanCode << 16), pString, 64);

                    if (length > 0)
                    {
                        name = new string(pString, 0, length);

                        if (key == VirtualKeys.VK_OEM_3 && name == "`")
                        {
                            name = "~";
                        }
                    }
                    else
                    {
                        if (key == VirtualKeys.VK_OEM_PLUS) name = "+";
                        else if (key == VirtualKeys.VK_OEM_COMMA) name = ",";
                        else if (key == VirtualKeys.VK_OEM_MINUS) name = "-";
                        else if (key == VirtualKeys.VK_OEM_PERIOD) name = ".";
                        else if (key == VirtualKeys.VK_OEM_1) name = ";";
                        else if (key == VirtualKeys.VK_OEM_2) name = "?";
                        else if (key == VirtualKeys.VK_OEM_3) name = "~";
                        else if (key == VirtualKeys.VK_OEM_4) name = "[";
                        else if (key == VirtualKeys.VK_OEM_5) name = "\\";
                        else if (key == VirtualKeys.VK_OEM_6) name = "]";
                        else if (key == VirtualKeys.VK_OEM_7) name = "'";
                    }

                    keyNames[key] = name!;
                    return name!;
                }
            }

            var keyNum = (int)key;

            if (keyNum >= (int)VirtualKeys.VK_0 && keyNum <= (int)VirtualKeys.VK_9)
            {
                var ch = (char)(keyNum - (int)VirtualKeys.VK_0 + '0');
                return $"{ch}";
            }

            else if (keyNum >= (int)VirtualKeys.VK_A && keyNum <= (int)VirtualKeys.VK_Z)
            {
                var ch = (char)(keyNum - (int)VirtualKeys.VK_A + 'A');
                return $"{ch}";
            }

            else if (keyNum >= (int)VirtualKeys.VK_NUMPAD0 && keyNum <= (int)VirtualKeys.VK_NUMPAD9)
            {
                var ch = (char)(keyNum - (int)VirtualKeys.VK_NUMPAD0 + '0');
                return $"Num {ch}";
            }

            return "";
        }

        public static bool IsCompleted(HotKeyModifiers modifiers, VirtualKeys? key)
        {
            if (!key.HasValue) return false;

            var modifierTexts = MapKeyToString(modifiers);

            if (modifierTexts == null || modifierTexts.Length == 0) return false;

            var keyText = MapKeyToString(key.Value);

            if (string.IsNullOrEmpty(keyText)) return false;

            return true;
        }

        public static string MapKeyToString(HotKeyModifiers modifiers, VirtualKeys key, bool compact = false)
        {
            var modifierTexts = MapKeyToString(modifiers);
            var keyText = key != 0 ? MapKeyToString(key) : "";

            if ((modifierTexts != null && modifierTexts.Length > 0)
                || !string.IsNullOrEmpty(keyText))
            {
                var sb = new StringBuilder();

                if (modifierTexts != null && modifierTexts.Length > 0)
                {
                    foreach (var item in modifierTexts)
                    {
                        sb.Append(item);

                        if (compact) sb.Append("+");
                        else sb.Append(" + ");
                    }
                }

                if (!string.IsNullOrEmpty(keyText))
                {
                    sb.Append(keyText);
                }
                else if (sb.Length > 0)
                {
                    sb.Length--;
                }

                return sb.ToString();
            }

            return "";
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VirtualKeyModifiers GetCurrentVirtualKeyModifiersStates()
        {
            VirtualKeyModifiers modifiers = default;

            if ((Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control) & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down)
                modifiers |= VirtualKeyModifiers.Control;

            if ((Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.LeftWindows) & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down)
                modifiers |= VirtualKeyModifiers.Windows;

            if ((Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.RightWindows) & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down)
                modifiers |= VirtualKeyModifiers.Windows;

            if ((Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu) & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down)
                modifiers |= VirtualKeyModifiers.Menu;

            if ((Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift) & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down)
                modifiers |= VirtualKeyModifiers.Shift;

            return modifiers;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HotKeyModifiers GetCurrentModifiersStates() =>
            MapModifiers(GetCurrentVirtualKeyModifiersStates());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VirtualKeyModifiers MapModifiers(HotKeyModifiers modifiers)
        {
            VirtualKeyModifiers m = default;

            if ((modifiers & HotKeyModifiers.MOD_CONTROL) != 0) m |= VirtualKeyModifiers.Control;
            if ((modifiers & HotKeyModifiers.MOD_ALT) != 0) m |= VirtualKeyModifiers.Menu;
            if ((modifiers & HotKeyModifiers.MOD_SHIFT) != 0) m |= VirtualKeyModifiers.Shift;
            if ((modifiers & HotKeyModifiers.MOD_WIN) != 0) m |= VirtualKeyModifiers.Windows;

            return m;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HotKeyModifiers MapModifiers(VirtualKeyModifiers modifiers)
        {
            HotKeyModifiers m = default;

            if ((modifiers & VirtualKeyModifiers.Control) != 0) m |= HotKeyModifiers.MOD_CONTROL;
            if ((modifiers & VirtualKeyModifiers.Menu) != 0) m |= HotKeyModifiers.MOD_ALT;
            if ((modifiers & VirtualKeyModifiers.Shift) != 0) m |= HotKeyModifiers.MOD_SHIFT;
            if ((modifiers & VirtualKeyModifiers.Windows) != 0) m |= HotKeyModifiers.MOD_WIN;

            return m;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HotKeyModifiers MapModifiers(VirtualKeys key) => key switch
        {
            VirtualKeys.VK_CONTROL => HotKeyModifiers.MOD_CONTROL,
            VirtualKeys.VK_LCONTROL => HotKeyModifiers.MOD_CONTROL,
            VirtualKeys.VK_RCONTROL => HotKeyModifiers.MOD_CONTROL,

            VirtualKeys.VK_MENU => HotKeyModifiers.MOD_ALT,
            VirtualKeys.VK_LMENU => HotKeyModifiers.MOD_ALT,
            VirtualKeys.VK_RMENU => HotKeyModifiers.MOD_ALT,

            VirtualKeys.VK_LWIN => HotKeyModifiers.MOD_WIN,
            VirtualKeys.VK_RWIN => HotKeyModifiers.MOD_WIN,

            VirtualKeys.VK_SHIFT => HotKeyModifiers.MOD_SHIFT,
            VirtualKeys.VK_LSHIFT => HotKeyModifiers.MOD_SHIFT,
            VirtualKeys.VK_RSHIFT => HotKeyModifiers.MOD_SHIFT,

            _ => 0
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VirtualKeyModifiers MapVirtualKeyModifiers(VirtualKeys key) => MapModifiers(MapModifiers(key));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IReadOnlyList<VirtualKeys> MapModifiersToVirtualKey(HotKeyModifiers modifiers)
        {
            if (modifiers == 0 || modifiers == HotKeyModifiers.MOD_NOREPEAT) return Array.Empty<VirtualKeys>();

            lock (modifierKeys)
            {
                if (modifierKeys.TryGetValue(modifiers, out var keys)) return keys;

                var list = new List<VirtualKeys>();

                if ((modifiers & HotKeyModifiers.MOD_CONTROL) != 0) list.Add(VirtualKeys.VK_CONTROL);
                if ((modifiers & HotKeyModifiers.MOD_ALT) != 0) list.Add(VirtualKeys.VK_MENU);
                if ((modifiers & HotKeyModifiers.MOD_SHIFT) != 0) list.Add(VirtualKeys.VK_SHIFT);
                if ((modifiers & HotKeyModifiers.MOD_WIN) != 0) list.Add(VirtualKeys.VK_LWIN);

                if (list.Count > 0)
                {
                    keys = list.ToArray();
                }
                else
                {
                    keys = Array.Empty<VirtualKeys>();
                }
                modifierKeys[modifiers] = keys;
                return keys;
            }
        }
    }
}
