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
        private bool registered;
        private bool internalSet;
        private bool registrationSuccessful;

        public HotKeyModel()
        {
            registered = false;

            IsEnabled = false;

            UpdateStatus();
        }

        internal HotKeyModel(HotKeyModifiers modifiers, VirtualKeys virtualKey)
        {
            registered = true;

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

        internal HotKeyModifiers ModifiersInternal { get; private set; }

        internal VirtualKeys VirtualKeyInternal { get; private set; }

        internal bool IsEnabledInternal { get; private set; }

        internal HotKeyModelStatus StatusInternal { get; private set; }

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

        public HotKeyModifiers Modifiers
        {
            get { return (HotKeyModifiers)GetValue(ModifiersProperty); }
            set { SetValue(ModifiersProperty, value); }
        }

        public static readonly DependencyProperty ModifiersProperty =
            DependencyProperty.Register("Modifiers", typeof(HotKeyModifiers), typeof(HotKeyModel), new PropertyMetadata(HotKeyModifiers.MOD_NONE, (s, a) =>
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
            get { return (VirtualKeys)GetValue(VirtualKeyProperty); }
            set { SetValue(VirtualKeyProperty, value); }
        }

        public static readonly DependencyProperty VirtualKeyProperty =
            DependencyProperty.Register("VirtualKey", typeof(VirtualKeys), typeof(HotKeyModel), new PropertyMetadata((VirtualKeys)0, (s, a) =>
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
            get { return (bool)GetValue(IsEnabledProperty); }
            set { SetValue(IsEnabledProperty, value); }
        }

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.Register("IsEnabled", typeof(bool), typeof(HotKeyModel), new PropertyMetadata(true, (s, a) =>
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



        public HotKeyModelStatus Status
        {
            get { return (HotKeyModelStatus)GetValue(StatusProperty); }
            private set { SetValue(StatusProperty, value); }
        }

        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register("Status", typeof(HotKeyModelStatus), typeof(HotKeyModel), new PropertyMetadata(HotKeyModelStatus.Invalid, (s, a) =>
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
        Enabled,
        Disabled,
        Invalid,
        NotRegistered,
        RegisterFailed,
    }
}
