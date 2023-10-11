using BlueFire.Toolkit.WinUI3.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueFire.Toolkit.WinUI3.Controls
{
    public class HotKeyInputBox : Control
    {
        public HotKeyInputBox()
        {
            this.DefaultStyleKey = typeof(HotKeyInputBox);
            this.DefaultStyleResourceUri = new Uri("ms-appx:///BlueFire.Toolkit.WinUI3/Themes/Generic.xaml");
            this.IsEnabledChanged += HotKeyInputBox_IsEnabledChanged;
            this.Loaded += HotKeyInputBox_Loaded;
            this.Unloaded += HotKeyInputBox_Unloaded;
        }

        private TextBlock? PreviewTextBlock;
        private Button? ClearButton;

        private VirtualKeys newKey;
        private HotKeyModifiers newModifiers;

        private long modelModifierToken;
        private long modelKeyToken;
        private bool editing = false;
        private WindowManager? windowManager;

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (ClearButton != null)
            {
                ClearButton.Click -= ClearButton_Click;
            }

            PreviewTextBlock = GetTemplateChild(nameof(PreviewTextBlock)) as TextBlock;
            ClearButton = GetTemplateChild(nameof(ClearButton)) as Button;

            if (ClearButton != null)
            {
                ClearButton.Click += ClearButton_Click;
            }

            UpdatePreviewText();
        }

        public HotKeyModel HotKeyModel
        {
            get { return (HotKeyModel)GetValue(HotKeyModelProperty); }
            set { SetValue(HotKeyModelProperty, value); }
        }

        public static readonly DependencyProperty HotKeyModelProperty =
            DependencyProperty.Register("HotKeyModel", typeof(HotKeyModel), typeof(HotKeyInputBox), new PropertyMetadata(null, (s, a) =>
            {
                if (s is HotKeyInputBox sender && !Equals(a.NewValue, a.OldValue))
                {
                    sender.newKey = 0;
                    sender.newModifiers = 0;
                    sender.editing = false;

                    if (a.OldValue is HotKeyModel oldValue)
                    {
                        oldValue.UnregisterPropertyChangedCallback(HotKeyModel.VirtualKeyProperty, sender.modelKeyToken);
                        oldValue.UnregisterPropertyChangedCallback(HotKeyModel.ModifiersProperty, sender.modelModifierToken);
                        sender.modelKeyToken = 0;
                        sender.modelModifierToken = 0;
                    }

                    if (a.NewValue is HotKeyModel newValue)
                    {
                        sender.modelKeyToken = newValue.RegisterPropertyChangedCallback(HotKeyModel.VirtualKeyProperty, sender.ModelVirtualKeyChanged);
                        sender.modelModifierToken = newValue.RegisterPropertyChangedCallback(HotKeyModel.ModifiersProperty, sender.ModelModifiersChanged);

                    }

                    sender.UpdatePreviewText();
                }
            }));

        public string InvalidKeyDisplayText
        {
            get { return (string)GetValue(InvalidKeyDisplayTextProperty); }
            set { SetValue(InvalidKeyDisplayTextProperty, value); }
        }

        public static readonly DependencyProperty InvalidKeyDisplayTextProperty =
            DependencyProperty.Register("InvalidKeyDisplayText", typeof(string), typeof(HotKeyInputBox), new PropertyMetadata("空", (s, a) =>
            {
                if (s is HotKeyInputBox sender) sender.UpdatePreviewText();
            }));

        protected override void OnPointerPressed(PointerRoutedEventArgs e)
        {
            base.OnPointerPressed(e);

            if (CapturePointer(e.Pointer))
            {
                if (this.FocusState == FocusState.Unfocused)
                {
                    this.Focus(FocusState.Pointer);
                }
                else
                {
                    EnterEditState();
                }
                e.Handled = true;
            }
        }

        protected override void OnPointerReleased(PointerRoutedEventArgs e)
        {
            base.OnPointerReleased(e);

            ReleasePointerCapture(e.Pointer);

            if (editing)
            {
                this.Focus(FocusState.Pointer);
            }

            e.Handled = true;
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);

            EnterEditState();
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);

            ExitEditState(true);
        }

        protected override void OnPreviewKeyDown(KeyRoutedEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            if (e.Key == Windows.System.VirtualKey.Tab
                || e.Key == Windows.System.VirtualKey.Enter
                || e.Key == Windows.System.VirtualKey.Escape)
            {
                if (e.Key == Windows.System.VirtualKey.Tab)
                {
                    ExitEditState(true);
                    return;
                }
                else if (e.Key == Windows.System.VirtualKey.Escape)
                {
                    ExitEditState(false);
                    return;
                }

                ExitEditState(true);

                var focusElement = FocusManager.FindFirstFocusableElement(XamlRoot.Content);

                if (focusElement != null)
                {
                    _ = FocusManager.TryFocusAsync(focusElement, FocusState.Programmatic);
                }

                e.Handled = true;
                return;
            }

            if (editing)
            {
                var newKey = unchecked((VirtualKeys)(byte)e.Key);
                var isModifier = HotKeyHelper.MapModifiers(newKey) != 0;

                e.Handled = true;

                if (isModifier)
                {
                    newModifiers = HotKeyHelper.GetCurrentModifiersStates();
                }
                else
                {
                    this.newKey = newKey;
                }

                UpdatePreviewText();
            }
        }

        protected override void OnPreviewKeyUp(KeyRoutedEventArgs e)
        {
            base.OnPreviewKeyUp(e);

            if (e.Key == Windows.System.VirtualKey.Tab
                || e.Key == Windows.System.VirtualKey.Enter
                || e.Key == Windows.System.VirtualKey.Escape)
            {
                return;
            }

            if (HotKeyHelper.IsCompleted(this.newModifiers, this.newKey))
            {
                ExitEditState(true);
                EnterEditState();
                e.Handled = true;
                return;
            }

            if (editing)
            {
                var newKey = unchecked((VirtualKeys)(byte)e.Key);
                var isModifier = HotKeyHelper.MapModifiers(newKey) != 0;

                if (isModifier)
                {
                    newModifiers = HotKeyHelper.GetCurrentModifiersStates();
                }
                else if (newKey == this.newKey)
                {
                    this.newKey = 0;
                }

                UpdatePreviewText();
            }
        }


        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ExitEditState(false);

            var model = HotKeyModel;
            if (model != null)
            {
                model.Modifiers = newModifiers;
                model.VirtualKey = newKey;
            }

            UpdateCommonVisualState();

            UpdatePreviewText();
        }

        private void HotKeyInputBox_ActivatedStateChanged(object? sender, EventArgs e)
        {
            var manager = windowManager;

            if (manager != null && !manager.IsForegroundWindow)
            {
                ExitEditState(true);

                var xamlRoot = XamlRoot;

                if (xamlRoot?.Content != null)
                {
                    try
                    {
                        var focusElement = FocusManager.FindFirstFocusableElement(xamlRoot.Content);

                        if (focusElement != null)
                        {
                            _ = FocusManager.TryFocusAsync(focusElement, FocusState.Programmatic);
                        }
                    }
                    catch { }
                }
            }
        }

        private void UpdatePreviewText()
        {
            if (PreviewTextBlock == null) return;
            var model = HotKeyModel;

            var modifiers = model?.Modifiers ?? 0;
            var key = model?.VirtualKey ?? 0;

            if (model == null || FocusState == FocusState.Unfocused)
            {
                // 失去焦点时，组合键无效则显示空

                var text = HotKeyHelper.MapKeyToString(modifiers, key);
                if (HotKeyHelper.IsCompleted(modifiers, key))
                {
                    PreviewTextBlock.Text = text;
                }
                else
                {
                    PreviewTextBlock.Text = InvalidKeyDisplayText;
                }
            }
            else
            {
                // 获得焦点时，组合键无文本则显示空

                string text = "";
                if (newModifiers == 0 && newKey == 0)
                {
                    text = HotKeyHelper.MapKeyToString(modifiers, key);
                }
                else
                {
                    text = HotKeyHelper.MapKeyToString(newModifiers, newKey);
                }

                if (!string.IsNullOrEmpty(text))
                {
                    PreviewTextBlock.Text = text;
                }
                else
                {
                    PreviewTextBlock.Text = InvalidKeyDisplayText;
                }
            }
        }

        private void EnterEditState()
        {
            if (HotKeyModel == null) return;
            if (editing) return;

            editing = true;

            newKey = 0;
            newModifiers = 0;

            UpdateCommonVisualState();

            UpdatePreviewText();

            HotKeyManager.UpdateInputBoxFocusState();
        }

        private void ExitEditState(bool apply)
        {
            if (editing)
            {
                editing = false;

                var model = HotKeyModel;
                if (model != null && apply && HotKeyHelper.IsCompleted(newModifiers, newKey))
                {
                    model.Modifiers = newModifiers;
                    model.VirtualKey = newKey;
                }

                newKey = 0;
                newModifiers = 0;

                UpdateCommonVisualState();

                UpdatePreviewText();

                HotKeyManager.UpdateInputBoxFocusState();
            }
        }

        private void ModelVirtualKeyChanged(DependencyObject sender, DependencyProperty dp)
        {
            UpdatePreviewText();
        }

        private void ModelModifiersChanged(DependencyObject sender, DependencyProperty dp)
        {
            UpdatePreviewText();
        }

        #region UpdateVisualState

        private void HotKeyInputBox_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UpdateCommonVisualState();
        }

        private bool pointerOver;


        private void HotKeyInputBox_Loaded(object sender, RoutedEventArgs e)
        {
            HotKeyManager.AddInputBox(this);

            if (windowManager != null)
            {
                windowManager.ActivatedStateChanged -= HotKeyInputBox_ActivatedStateChanged;
                windowManager = null;
            }

            var manager = WindowManager.Get(XamlRoot.ContentIslandEnvironment.AppWindowId);

            if (manager != null)
            {
                manager.ActivatedStateChanged += HotKeyInputBox_ActivatedStateChanged;
                windowManager = manager;
            }
        }

        private void HotKeyInputBox_Unloaded(object sender, RoutedEventArgs e)
        {
            HotKeyManager.RemoveInputBox(this);
            pointerOver = false;
            UpdateCommonVisualState();

            if (windowManager != null)
            {
                windowManager.ActivatedStateChanged -= HotKeyInputBox_ActivatedStateChanged;
                windowManager = null;
            }
        }

        protected override void OnPointerEntered(PointerRoutedEventArgs e)
        {
            base.OnPointerEntered(e);
            pointerOver = true;

            UpdateCommonVisualState();
        }

        protected override void OnPointerExited(PointerRoutedEventArgs e)
        {
            base.OnPointerExited(e);
            pointerOver = false;

            UpdateCommonVisualState();
        }

        protected override void OnPointerCanceled(PointerRoutedEventArgs e)
        {
            base.OnPointerCanceled(e);
            pointerOver = false;

            UpdateCommonVisualState();
        }

        private void UpdateCommonVisualState()
        {
            bool windowActivated = true;

            if (windowManager != null) windowActivated = windowManager.IsForegroundWindow;

            if (!IsEnabled)
            {
                VisualStateManager.GoToState(this, "Disabled", true);
            }
            else if (FocusState != FocusState.Unfocused && editing && windowActivated)
            {
                VisualStateManager.GoToState(this, "Focused", true);
            }
            else if (pointerOver)
            {
                VisualStateManager.GoToState(this, "PointerOver", true);
            }
            else
            {
                VisualStateManager.GoToState(this, "Normal", true);
            }
        }

        #endregion UpdateVisualState
    }
}
