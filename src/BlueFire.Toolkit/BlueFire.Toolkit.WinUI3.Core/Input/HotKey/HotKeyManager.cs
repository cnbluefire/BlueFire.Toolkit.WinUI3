using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Win32.Foundation;
using PInvoke = Windows.Win32.PInvoke;

namespace BlueFire.Toolkit.WinUI3.Input
{
    public static class HotKeyManager
    {
        private static HotKeyListener? listener;
        private static List<HotKeyModel> models = new List<HotKeyModel>();
        private static List<WeakReference<Controls.HotKeyInputBox>> inputBoxes = new List<WeakReference<Controls.HotKeyInputBox>>();
        internal static object locker = new object();

        private static bool isEnabledInternal = true;
        private static bool isEnabled = true;

        internal static bool IsEnabledInternal
        {
            get => isEnabledInternal;
            set
            {
                lock (locker)
                {
                    if (isEnabledInternal != value)
                    {
                        isEnabledInternal = value;

                        UpdateModelsRegistration();
                    }
                }
            }
        }

        public static bool IsEnabled
        {
            get => isEnabled;
            set
            {
                lock (locker)
                {
                    if (isEnabled != value)
                    {
                        isEnabled = value;

                        UpdateModelsRegistration();
                    }
                }
            }
        }


        public static HotKeyModel? RegisterKey(string id, HotKeyModifiers modifiers, VirtualKeys key)
        {
            lock (locker)
            {
                if (models.Any(c => c.Id == id)) return null;

                var model = new HotKeyModel(id, modifiers, key);

                models.Add(model);

                if (model.StatusInternal != HotKeyModelStatus.Invalid)
                {
                    var listener = EnsureListener();

                    UpdateModelsRegistration();

                }

                return model;
            }
        }

        public static void Unregister(string id)
        {
            lock (locker)
            {
                var model = models.FirstOrDefault(c => c.Id == id);
                if (model != null)
                {
                    if (model.Registered)
                    {
                        model.Registered = false;
                        UpdateModelsRegistration();
                    }
                }
            }
        }

        internal static void UpdateModelsRegistration()
        {
            lock (locker)
            {
                var listener = HotKeyManager.listener;

                if (listener != null)
                {
                    listener.UnregisterAllKeys().GetAwaiter().GetResult();
                }

                if (!isEnabledInternal || !isEnabled) return;

                var states = new HashSet<(HotKeyModifiers, VirtualKeys)>();

                for (int i = 0; i < models.Count; i++)
                {
                    var model = models[i];

                    if (model.StatusInternal == HotKeyModelStatus.Enabled || model.StatusInternal == HotKeyModelStatus.RegisterFailed)
                    {
                        if (!states.Contains((model.ModifiersInternal, model.VirtualKeyInternal)))
                        {
                            if (listener == null)
                            {
                                listener = EnsureListener();
                            }

                            var state = listener.RegisterKey(model.ModifiersInternal, model.VirtualKeyInternal).GetAwaiter().GetResult();
                            model.RegistrationSuccessful = state;

                            if (state)
                            {
                                states.Add((model.ModifiersInternal, model.VirtualKeyInternal));
                            }
                        }
                        else
                        {
                            model.RegistrationSuccessful = false;
                        }
                    }
                }
            }
        }

        internal static void AddInputBox(Controls.HotKeyInputBox hotKeyInputBox)
        {
            lock (locker)
            {
                for (int i = inputBoxes.Count - 1; i >= 0; i--)
                {
                    if (inputBoxes[i].TryGetTarget(out var target))
                    {
                        if (target == hotKeyInputBox)
                        {
                            return;
                        }
                    }
                    else
                    {
                        inputBoxes.RemoveAt(i);
                    }
                }

                inputBoxes.Add(new WeakReference<Controls.HotKeyInputBox>(hotKeyInputBox));
                UpdateInputBoxFocusState();
            }
        }

        internal static void RemoveInputBox(Controls.HotKeyInputBox hotKeyInputBox)
        {
            lock (locker)
            {
                for (int i = inputBoxes.Count - 1; i >= 0; i--)
                {
                    if (inputBoxes[i].TryGetTarget(out var target))
                    {
                        if (target == hotKeyInputBox)
                        {
                            inputBoxes.RemoveAt(i);
                        }
                    }
                    else
                    {
                        inputBoxes.RemoveAt(i);
                    }
                }
                UpdateInputBoxFocusState();
            }
        }

        internal static void UpdateInputBoxFocusState()
        {
            lock (locker)
            {
                bool enabled = true;

                for (int i = inputBoxes.Count - 1; i >= 0; i--)
                {
                    if (inputBoxes[i].TryGetTarget(out var target))
                    {
                        try
                        {
                            if (target.IsLoaded
                                && target.FocusState != Microsoft.UI.Xaml.FocusState.Unfocused
                                && target.XamlRoot?.ContentIslandEnvironment != null
                                && WindowManager.Get(target.XamlRoot.ContentIslandEnvironment.AppWindowId)?.IsForegroundWindow == true)
                            {
                                enabled = false;
                                break;
                            }

                        }
                        catch { }
                    }
                    else
                    {
                        inputBoxes.RemoveAt(i);
                    }
                }

                IsEnabledInternal = enabled;
            }
        }

        private static void Listener_HotKeyInvoked(HotKeyListener sender, HotKeyListenerInvokedEventArgs args)
        {
            lock (locker)
            {
                HotKeyInvokedEventArgs? args2 = null;

                for (int i = 0; i < models.Count; i++)
                {
                    var model = models[i];
                    try
                    {
                        if (model.IsEnabledInternal
                            && model.ModifiersInternal == args.Modifier
                            && model.VirtualKeyInternal == args.Key)
                        {
                            if (args2 == null) args2 = new HotKeyInvokedEventArgs(model);

                            model.RaiseInvoked(args2);

                            break;
                        }
                    }
                    catch { }
                }

                if (args2 != null)
                {
                    HotKeyInvoked?.Invoke(args2);
                }
            }
        }

        public static event HotKeyInvokedEventHandler? HotKeyInvoked;

        private static HotKeyListener EnsureListener()
        {
            if (listener == null)
            {
                lock (models)
                {
                    if (listener == null)
                    {
                        listener = new HotKeyListener();
                        listener.HotKeyInvoked += Listener_HotKeyInvoked;
                    }
                }
            }

            return listener;
        }
    }

    public record HotKeyInvokedEventArgs(HotKeyModel Model);

    public delegate void HotKeyInvokedEventHandler(HotKeyInvokedEventArgs args);

}
