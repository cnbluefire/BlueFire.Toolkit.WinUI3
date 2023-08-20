using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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


        public static HotKeyModel? RegisterKey(HotKeyModifiers modifiers, VirtualKeys key)
        {
            lock (locker)
            {
                var model = new HotKeyModel(modifiers, key);

                if (model.StatusInternal != HotKeyModelStatus.Invalid)
                {
                    models.Add(model);

                    var listener = EnsureListener();

                    UpdateModelsRegistration();

                    return model;
                }

                return null;
            }
        }

        public static void RegisterKey(HotKeyModel model)
        {
            lock (locker)
            {
                if (model.StatusInternal != HotKeyModelStatus.NotRegistered)
                {
                    throw new ArgumentException("Already registered", nameof(model));
                }

                model.Registered = true;
                models.Add(model);

                if (model.StatusInternal == HotKeyModelStatus.RegisterFailed)
                {
                    UpdateModelsRegistration();
                }
            }
        }

        public static void Unregister(HotKeyModel model)
        {
            lock (locker)
            {
                if (model.Registered && models.Remove(model))
                {
                    model.Registered = false;
                    UpdateModelsRegistration();
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

                if (!isEnabledInternal) return;

                var states = new Dictionary<(HotKeyModifiers, VirtualKeys), bool>();

                for (int i = 0; i < models.Count; i++)
                {
                    var model = models[i];

                    if (model.StatusInternal == HotKeyModelStatus.Enabled || model.StatusInternal == HotKeyModelStatus.RegisterFailed)
                    {
                        if (states.TryGetValue((model.ModifiersInternal, model.VirtualKeyInternal), out var state))
                        {
                            model.RegistrationSuccessful = state;
                        }
                        else
                        {
                            if (listener == null)
                            {
                                listener = EnsureListener();
                            }

                            state = listener.RegisterKey(model.ModifiersInternal, model.VirtualKeyInternal).GetAwaiter().GetResult();
                            model.RegistrationSuccessful = state;
                            states[(model.ModifiersInternal, model.VirtualKeyInternal)] = state;
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

        private static void Listener_HotKeyInvoked(HotKeyListener sender, HotKeyInvokedEventArgs args)
        {
            lock (locker)
            {
                for (int i = 0; i < models.Count; i++)
                {
                    var model = models[i];
                    try
                    {
                        if (model.ModifiersInternal == args.Modifier
                            && model.VirtualKeyInternal == args.Key)
                        {
                            model.RaiseInvoked(args);
                        }
                    }
                    catch { }
                }
            }
        }

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
}
