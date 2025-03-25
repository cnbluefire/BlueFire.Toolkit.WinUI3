using BlueFire.Toolkit.Dispatching;
using BlueFire.Toolkit.WindowBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace BlueFire.Toolkit.Extensions
{
    internal static class SystemEvents
    {
        private const uint WM_REFLECT = PInvoke.WM_USER + 0x1C00;

        private static Thread? thread;
        private static NativeWindow? nativeWindow;
        private static Lock locker = new Lock();

        private static object s_onUserPreferenceChangedEvent = new object();
        private static object s_onDisplaySettingsChangedEvent = new object();

        private static Dictionary<object, List<Delegate>>? s_handlers;

        public static event EventHandler<UserPreferenceChangedEventArgs>? UserPreferenceChanged
        {
            add
            {
                EnsureMessageWindow();
                AddEventHandler(s_onUserPreferenceChangedEvent, value);
            }
            remove
            {
                RemoveEventHandler(s_onUserPreferenceChangedEvent, value);
            }
        }

        public static event EventHandler? DisplaySettingsChanged
        {
            add
            {
                EnsureMessageWindow();
                AddEventHandler(s_onDisplaySettingsChangedEvent, value);
            }
            remove
            {
                RemoveEventHandler(s_onDisplaySettingsChangedEvent, value);
            }
        }

        private static void EnsureMessageWindow()
        {
            if (thread == null)
            {
                lock (locker)
                {
                    if (thread == null)
                    {
                        thread = new Thread(new ThreadStart(ThreadMain));
                        thread.IsBackground = true;
                        thread.Start();
                    }
                }
            }

            static unsafe void ThreadMain()
            {
                nativeWindow = new NativeWindow(0, "BlueFire.Toolkit.Screen.SystemEvents");
                nativeWindow.MessageReceived += OnMessageReceived;

                MSG msg;
                while (PInvoke.GetMessage(&msg, HWND.Null, 0, 0))
                {
                    PInvoke.TranslateMessage(in msg);
                    PInvoke.DispatchMessage(in msg);
                }

                nativeWindow.Dispose();
            }
        }

        private static void OnMessageReceived(object sender, WindowMessageReceivedEventArgs e)
        {
            switch (e.MessageId)
            {
                case PInvoke.WM_DISPLAYCHANGE:
                case PInvoke.WM_SYSCOLORCHANGE:
                    {
                        PInvoke.PostMessage((HWND)e.WindowHandle, WM_REFLECT + e.MessageId, (WPARAM)e.WParam, (LPARAM)e.LParam);
                    }
                    break;

                case PInvoke.WM_SETTINGCHANGE:
                    {
                        var newStringPtr = e.LParam;

                        if (e.LParam != 0)
                        {
                            var newString = Marshal.PtrToStringUni(e.LParam);
                            if (newString != null)
                            {
                                newStringPtr = Marshal.StringToHGlobalUni(newString);
                            }
                        }

                        PInvoke.PostMessage((HWND)e.WindowHandle, WM_REFLECT + e.MessageId, (WPARAM)e.WParam, (LPARAM)newStringPtr);
                    }
                    break;

                case WM_REFLECT + PInvoke.WM_DISPLAYCHANGE:
                    {
                        RaiseEvent(s_onDisplaySettingsChangedEvent, EventArgs.Empty);
                    }
                    break;

                case WM_REFLECT + PInvoke.WM_SYSCOLORCHANGE:
                case WM_REFLECT + PInvoke.WM_SETTINGCHANGE:
                    {
                        bool shouldFreeLParam = false;
                        try
                        {
                            var category = GetUserPreferenceCategory(e.MessageId, e.WParam, e.LParam, out var action, out shouldFreeLParam);
                            RaiseEvent(s_onUserPreferenceChangedEvent, new UserPreferenceChangedEventArgs(category, (uint)action));
                        }
                        finally
                        {
                            if (shouldFreeLParam && e.LParam != 0)
                            {
                                try
                                {
                                    Marshal.FreeHGlobal(e.LParam);
                                }
                                catch { }
                            }
                        }
                    }
                    break;
            }
        }

        private static void AddEventHandler(object key, Delegate? value)
        {
            if (value is null) return;

            lock (locker)
            {
                if (s_handlers == null)
                {
                    s_handlers = new Dictionary<object, List<Delegate>>();
                    EnsureMessageWindow();
                }

                if (!s_handlers.TryGetValue(key, out var events))
                {
                    events = new List<Delegate>();
                    s_handlers[key] = events;
                }

                events.Add(value);
            }
        }

        private static void RemoveEventHandler(object key, Delegate? value)
        {
            if (value is null) return;

            lock (locker)
            {
                if (s_handlers != null && s_handlers.TryGetValue(key, out var events))
                {
                    events.Remove(value);
                }
            }
        }

        private static void RaiseEvent(object key, EventArgs args)
        {
            Delegate?[]? handlers = null;

            lock (locker)
            {
                if (s_handlers != null && s_handlers.TryGetValue(key, out var events))
                {
                    handlers = events?.ToArray();
                }
            }

            if (handlers != null)
            {
                for (int i = 0; i < handlers.Length; i++)
                {
                    try
                    {
                        {
                            if (handlers[i] is EventHandler handler)
                            {
                                handler?.Invoke(null, args);
                            }
                        }
                        {
                            if (handlers[i] is EventHandler<UserPreferenceChangedEventArgs> handler)
                            {
                                handler?.Invoke(null, (UserPreferenceChangedEventArgs)args);
                            }
                        }
                        handlers[i] = null;
                    }
                    catch { }
                }

                lock (locker)
                {
                    List<Delegate>? events = null;

                    for (int i = 0; i < handlers.Length; i++)
                    {
                        if (handlers[i] != null)
                        {
                            if (events == null)
                            {
                                if (!s_handlers!.TryGetValue(key, out events))
                                {
                                    return;
                                }
                            }

                            events.Remove(handlers[i]!);
                        }
                    }
                }
            }
        }

        public enum UserPreferenceCategory
        {
            Accessibility = 1,
            Color = 2,
            Desktop = 3,
            General = 4,
            Icon = 5,
            Keyboard = 6,
            Menu = 7,
            Mouse = 8,
            Policy = 9,
            Power = 10,
            Screensaver = 11,
            Window = 12,
            Locale = 13,
            VisualStyle = 14,
        }

        private static UserPreferenceCategory GetUserPreferenceCategory(uint msg, nuint wParam, nint lParam, out SYSTEM_PARAMETERS_INFO_ACTION systemParametersInfoAction, out bool shouldFreeLParam)
        {
            systemParametersInfoAction = 0;
            shouldFreeLParam = false;
            UserPreferenceCategory pref = UserPreferenceCategory.General;

            if (msg == PInvoke.WM_SETTINGCHANGE)
            {
                string? str = null;

                if (lParam != 0)
                {
                    shouldFreeLParam = true;
                    str = Marshal.PtrToStringUni(lParam);
                }

                if ("Policy".Equals(str))
                {
                    pref = UserPreferenceCategory.Policy;
                }
                else if ("intl".Equals(str))
                {
                    pref = UserPreferenceCategory.Locale;
                }
                else
                {
                    systemParametersInfoAction = (SYSTEM_PARAMETERS_INFO_ACTION)(uint)wParam;
                    switch (systemParametersInfoAction)
                    {
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETACCESSTIMEOUT:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETFILTERKEYS:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETHIGHCONTRAST:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETMOUSEKEYS:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETSCREENREADER:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETSERIALKEYS:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETSHOWSOUNDS:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETSOUNDSENTRY:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETSTICKYKEYS:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETTOGGLEKEYS:
                            pref = UserPreferenceCategory.Accessibility;
                            break;

                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETDESKWALLPAPER:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETFONTSMOOTHING:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETCURSORS:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETDESKPATTERN:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETGRIDGRANULARITY:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETWORKAREA:
                            pref = UserPreferenceCategory.Desktop;
                            break;

                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_ICONHORIZONTALSPACING:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_ICONVERTICALSPACING:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETICONMETRICS:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETICONS:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETICONTITLELOGFONT:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETICONTITLEWRAP:
                            pref = UserPreferenceCategory.Icon;
                            break;

                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETDOUBLECLICKTIME:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETDOUBLECLKHEIGHT:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETDOUBLECLKWIDTH:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETMOUSE:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETMOUSEBUTTONSWAP:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETMOUSEHOVERHEIGHT:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETMOUSEHOVERTIME:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETMOUSESPEED:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETMOUSETRAILS:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETSNAPTODEFBUTTON:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETWHEELSCROLLLINES:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETCURSORSHADOW:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETHOTTRACKING:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETTOOLTIPANIMATION:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETTOOLTIPFADE:
                            pref = UserPreferenceCategory.Mouse;
                            break;

                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETKEYBOARDDELAY:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETKEYBOARDPREF:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETKEYBOARDSPEED:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETLANGTOGGLE:
                            pref = UserPreferenceCategory.Keyboard;
                            break;

                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETMENUDROPALIGNMENT:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETMENUFADE:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETMENUSHOWDELAY:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETMENUANIMATION:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETSELECTIONFADE:
                            pref = UserPreferenceCategory.Menu;
                            break;

                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETLOWPOWERACTIVE:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETLOWPOWERTIMEOUT:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETPOWEROFFACTIVE:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETPOWEROFFTIMEOUT:
                            pref = UserPreferenceCategory.Power;
                            break;

                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETSCREENSAVEACTIVE:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETSCREENSAVERRUNNING:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETSCREENSAVETIMEOUT:
                            pref = UserPreferenceCategory.Screensaver;
                            break;

                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETKEYBOARDCUES:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETCOMBOBOXANIMATION:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETLISTBOXSMOOTHSCROLLING:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETGRADIENTCAPTIONS:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETUIEFFECTS:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETACTIVEWINDOWTRACKING:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETACTIVEWNDTRKZORDER:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETACTIVEWNDTRKTIMEOUT:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETANIMATION:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETBORDER:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETCARETWIDTH:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETDRAGFULLWINDOWS:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETDRAGHEIGHT:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETDRAGWIDTH:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETFOREGROUNDFLASHCOUNT:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETFOREGROUNDLOCKTIMEOUT:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETMINIMIZEDMETRICS:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETNONCLIENTMETRICS:
                        case SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETSHOWIMEUI:
                            pref = UserPreferenceCategory.Window;
                            break;
                    }
                }
            }
            else if (msg == PInvoke.WM_SYSCOLORCHANGE)
            {
                pref = UserPreferenceCategory.Color;
            }

            return pref;
        }

        public class UserPreferenceChangedEventArgs : EventArgs
        {
            internal UserPreferenceChangedEventArgs(UserPreferenceCategory category, uint systemParametersInfoAction)
            {
                Category = category;
                SystemParametersInfoAction = systemParametersInfoAction;
            }

            public UserPreferenceCategory Category { get; }

            public uint SystemParametersInfoAction { get; }

            internal SYSTEM_PARAMETERS_INFO_ACTION GetSystemParametersInfoAction() => (SYSTEM_PARAMETERS_INFO_ACTION)SystemParametersInfoAction;
        }
    }
}
