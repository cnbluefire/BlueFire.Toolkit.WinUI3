using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace BlueFire.Toolkit.WinUI3.Input
{
    public class HotKeyModel : DependencyObject
    {
        private const HotKeyModifiers DefaultModifiers = HotKeyModifiers.MOD_NONE;
        private const VirtualKeys DefaultVirtualKeys = (VirtualKeys)0;
        private const bool DefaultIsEnabled = true;
        private const HotKeyModelStatus DefaultStatus = HotKeyModelStatus.Invalid;

        private bool registered;
        private bool internalSet;
        private bool registrationSuccessful;

        internal HotKeyModel(string id, HotKeyModifiers modifiers, VirtualKeys virtualKey)
        {
            registered = true;
            Id = id;

            Modifiers = modifiers;
            VirtualKey = virtualKey;

            UpdateStatus();
        }

        internal bool Registered
        {
            get => registered;
            set
            {
                if (registered != value)
                {
                    registered = value;
                    UpdateStatus();
                }
            }
        }

        internal HotKeyModifiers ModifiersInternal { get; private set; } = DefaultModifiers;

        internal VirtualKeys VirtualKeyInternal { get; private set; } = DefaultVirtualKeys;

        internal bool IsEnabledInternal { get; private set; } = DefaultIsEnabled;

        internal HotKeyModelStatus StatusInternal { get; private set; } = DefaultStatus;

        internal bool RegistrationSuccessful
        {
            get => registrationSuccessful;
            set
            {
                if (registrationSuccessful != value)
                {
                    registrationSuccessful = value;
                    UpdateStatus();
                }
            }
        }

        /// <summary>
        /// Hotkey unique id.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Hotkey label. Can be used in the UI.
        /// </summary>
        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register("Label", typeof(string), typeof(HotKeyModel), new PropertyMetadata("", (s, a) =>
            {
                if (a.NewValue is null) throw new ArgumentException(null, nameof(Label));
            }));

        public HotKeyModifiers Modifiers
        {
            get { return DispatcherQueue.HasThreadAccess ? (HotKeyModifiers)GetValue(ModifiersProperty) : ModifiersInternal; }
            set { SetValue(ModifiersProperty, value); }
        }

        public static readonly DependencyProperty ModifiersProperty =
            DependencyProperty.Register("Modifiers", typeof(HotKeyModifiers), typeof(HotKeyModel), new PropertyMetadata(DefaultModifiers, (s, a) =>
            {
                lock (HotKeyManager.locker)
                {
                    if (s is HotKeyModel sender && !Equals(a.NewValue, a.OldValue))
                    {
                        sender.ModifiersInternal = (HotKeyModifiers)a.NewValue;
                        sender.registrationSuccessful = false;
                        sender.UpdateStatus();
                        sender.UpdateModelsRegistration();
                    }
                }
            }));



        public VirtualKeys VirtualKey
        {
            get { return DispatcherQueue.HasThreadAccess ? (VirtualKeys)GetValue(VirtualKeyProperty) : VirtualKeyInternal; }
            set { SetValue(VirtualKeyProperty, value); }
        }

        public static readonly DependencyProperty VirtualKeyProperty =
            DependencyProperty.Register("VirtualKey", typeof(VirtualKeys), typeof(HotKeyModel), new PropertyMetadata(DefaultVirtualKeys, (s, a) =>
            {
                lock (HotKeyManager.locker)
                {
                    if (s is HotKeyModel sender && !Equals(a.NewValue, a.OldValue))
                    {
                        sender.VirtualKeyInternal = (VirtualKeys)a.NewValue;
                        sender.registrationSuccessful = false;
                        sender.UpdateStatus();
                        sender.UpdateModelsRegistration();
                    }
                }
            }));



        public bool IsEnabled
        {
            get { return DispatcherQueue.HasThreadAccess ? (bool)GetValue(IsEnabledProperty) : IsEnabledInternal; }
            set { SetValue(IsEnabledProperty, value); }
        }

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.Register("IsEnabled", typeof(bool), typeof(HotKeyModel), new PropertyMetadata(DefaultIsEnabled, (s, a) =>
            {
                lock (HotKeyManager.locker)
                {
                    if (s is HotKeyModel sender && !Equals(a.NewValue, a.OldValue))
                    {
                        sender.IsEnabledInternal = (bool)a.NewValue;
                        sender.registrationSuccessful = false;
                        sender.UpdateStatus();
                        sender.UpdateModelsRegistration();
                    }
                }
            }));


        /// <summary>
        /// The registration status of the hotkey.
        /// </summary>
        public HotKeyModelStatus Status
        {
            get { return DispatcherQueue.HasThreadAccess ? (HotKeyModelStatus)GetValue(StatusProperty) : StatusInternal; }
            private set { SetValue(StatusProperty, value); }
        }

        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register("Status", typeof(HotKeyModelStatus), typeof(HotKeyModel), new PropertyMetadata(DefaultStatus, (s, a) =>
            {
                lock (HotKeyManager.locker)
                {
                    if (s is HotKeyModel sender && !Equals(a.NewValue, a.OldValue))
                    {
                        if (!sender.internalSet) throw new NotSupportedException(nameof(Status));
                        sender.StatusInternal = (HotKeyModelStatus)a.NewValue;
                    }
                }
            }));

        /// <summary>
        /// Hotkey invoked event.
        /// </summary>
        public event TypedEventHandler<HotKeyModel, HotKeyInvokedEventArgs>? Invoked;

        internal void RaiseInvoked(HotKeyInvokedEventArgs args)
        {
            Invoked?.Invoke(this, args);
        }

        private void UpdateStatus()
        {
            lock (HotKeyManager.locker)
            {
                internalSet = true;

                try
                {
                    if (!registered)
                    {
                        Status = HotKeyModelStatus.NotRegistered;
                    }
                    else
                    {
                        if (IsEnabled)
                        {
                            if (HotKeyHelper.IsCompleted(Modifiers, VirtualKey))
                            {
                                if (RegistrationSuccessful)
                                {
                                    Status = HotKeyModelStatus.Enabled;
                                }
                                else
                                {
                                    Status = HotKeyModelStatus.RegisterFailed;
                                }
                            }
                            else
                            {
                                Status = HotKeyModelStatus.Invalid;
                            }
                        }
                        else
                        {
                            Status = HotKeyModelStatus.Disabled;
                        }
                    }
                }
                finally
                {
                    internalSet = false;
                }
            }
        }

        private void UpdateModelsRegistration()
        {
            if (StatusInternal == HotKeyModelStatus.RegisterFailed)
            {
                HotKeyManager.UpdateModelsRegistration();
            }
        }

        public override string ToString()
        {
            return HotKeyHelper.MapKeyToString(ModifiersInternal, VirtualKeyInternal);
        }

        public string ToString(bool compact)
        {
            return HotKeyHelper.MapKeyToString(ModifiersInternal, VirtualKeyInternal, compact);
        }
    }

    public enum HotKeyModelStatus
    {
        /// <summary>
        /// Hotkey is enabled.
        /// </summary>
        Enabled,

        /// <summary>
        /// Hotkey is disabled.
        /// </summary>
        Disabled,

        /// <summary>
        /// Hotkey is invalid.
        /// </summary>
        Invalid,

        /// <summary>
        /// Hotkey has not been registered.
        /// </summary>
        NotRegistered,

        /// <summary>
        /// Hotkey registration failed.
        /// </summary>
        RegisterFailed,
    }
}
