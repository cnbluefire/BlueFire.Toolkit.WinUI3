using BlueFire.Toolkit.WinUI3.Compositions;
using BlueFire.Toolkit.WinUI3.WindowBase;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinCompositionColorBrush = Windows.UI.Composition.CompositionColorBrush;
using Windows.Win32.Foundation;
using Windows.Win32;
using Windows.Win32.Graphics.Gdi;
using WinRT;
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Xaml.Markup;
using BlueFire.Toolkit.WinUI3.Extensions;

namespace BlueFire.Toolkit.WinUI3.SystemBackdrops
{
    public class TransparentBackdrop : WindowBackdropBase
    {
        private WindowManager? windowManager;
        private WinCompositionColorBrush? colorBrush;
        private COLORREF defaultWindowBackground;
        private DeleteObjectSafeHandle? backgroundBrush;

        protected override void OnTargetConnected(WindowId windowId, ICompositionSupportsSystemBackdrop connectedTarget, XamlRoot xamlRoot)
        {
            base.OnTargetConnected(windowId, connectedTarget, xamlRoot);

            if (colorBrush == null)
            {
                colorBrush = WindowsCompositionHelper.Compositor.CreateColorBrush(BackgroundColor);
            }

            connectedTarget.SystemBackdrop = colorBrush;

            windowManager = WindowManager.Get(windowId);

            if (windowManager != null)
            {
                defaultWindowBackground = new COLORREF(PInvoke.GetSysColor(SYS_COLOR_INDEX.COLOR_WINDOW));
                defaultWindowBackground = new COLORREF(0x00000000);

                windowManager.GetMonitorInternal().WindowMessageBeforeReceived -= OnWindowMessageBeforeReceived;
                windowManager.GetMonitorInternal().WindowMessageAfterReceived -= OnWindowMessageAfterReceived;

                windowManager.GetMonitorInternal().WindowMessageBeforeReceived += OnWindowMessageBeforeReceived;
                windowManager.GetMonitorInternal().WindowMessageAfterReceived += OnWindowMessageAfterReceived;

                UpdateTransparentAttributes(true);
            }
        }

        protected override void OnTargetDisconnected(ICompositionSupportsSystemBackdrop disconnectedTarget)
        {
            base.OnTargetDisconnected(disconnectedTarget);

            disconnectedTarget.SystemBackdrop = null;

            if (windowManager != null)
            {
                windowManager.GetMonitorInternal().WindowMessageBeforeReceived -= OnWindowMessageBeforeReceived;
                windowManager.GetMonitorInternal().WindowMessageAfterReceived -= OnWindowMessageAfterReceived;

                UpdateTransparentAttributes(false);

                windowManager = null;
            }
        }

        private bool TryGetWindowId(ICompositionSupportsSystemBackdrop target, XamlRoot xamlRoot, out WindowId windowId)
        {
            windowId = xamlRoot.GetContentWindowId();
            return windowId.Value != 0;
        }

        private unsafe void OnWindowMessageBeforeReceived(WindowManager sender, WindowMessageReceivedEventArgs e)
        {
            if (e.MessageId == PInvoke.WM_DWMCOMPOSITIONCHANGED)
            {
                UpdateTransparentAttributes(true);
            }
            else if (e.MessageId == PInvoke.WM_ERASEBKGND)
            {
                if (PInvoke.GetClientRect(new HWND((nint)e.WindowId.Value), out var rect))
                {
                    if (backgroundBrush == null)
                    {
                        backgroundBrush = PInvoke.CreateSolidBrush_SafeHandle(new COLORREF(0x00000000));
                    }

                    PInvoke.FillRect(new HDC((nint)e.WParam), rect, backgroundBrush);

                    e.LResult = 1;
                    e.Handled = true;
                }
            }
        }

        private unsafe void OnWindowMessageAfterReceived(WindowManager sender, WindowMessageReceivedEventArgs e)
        {
            if (e.MessageId == PInvoke.WM_STYLECHANGING)
            {
                if (e.WParam == unchecked((nuint)Windows.Win32.UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE))
                {
                    if ((((Windows.Win32.UI.WindowsAndMessaging.STYLESTRUCT*)e.LParam)->styleNew & (uint)(Windows.Win32.UI.WindowsAndMessaging.WINDOW_EX_STYLE.WS_EX_LAYERED)) == 0)
                    {
                        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.High, () =>
                        {
                            UpdateTransparentAttributes(true);
                        });
                    }
                }
            }
        }

        private unsafe void UpdateTransparentAttributes(bool enabled)
        {
            if (windowManager != null)
            {
                PInvoke.DwmExtendFrameIntoClientArea(windowManager.HWND, new Windows.Win32.UI.Controls.MARGINS());

                using var hrgn = PInvoke.CreateRectRgn_SafeHandle(-2, -2, -1, -1);
                PInvoke.DwmEnableBlurBehindWindow(windowManager.HWND, new Windows.Win32.Graphics.Dwm.DWM_BLURBEHIND()
                {
                    dwFlags = enabled ? (PInvoke.DWM_BB_ENABLE | PInvoke.DWM_BB_BLURREGION) : 0,
                    fEnable = enabled,
                    hRgnBlur = new HRGN(hrgn.DangerousGetHandle())
                });

                var exStyle = PInvoke.GetWindowLongAuto(windowManager.HWND, Windows.Win32.UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
                if (enabled)
                {
                    exStyle |= (nint)(Windows.Win32.UI.WindowsAndMessaging.WINDOW_EX_STYLE.WS_EX_LAYERED);

                    PInvoke.SetLayeredWindowAttributes(windowManager.HWND, defaultWindowBackground, 255, Windows.Win32.UI.WindowsAndMessaging.LAYERED_WINDOW_ATTRIBUTES_FLAGS.LWA_ALPHA);

                    if ((defaultWindowBackground.Value & 0x00FFFFFF) != 0)
                    {
                        PInvoke.SetLayeredWindowAttributes(windowManager.HWND, defaultWindowBackground, 255, Windows.Win32.UI.WindowsAndMessaging.LAYERED_WINDOW_ATTRIBUTES_FLAGS.LWA_COLORKEY);
                    }
                }
                else
                {
                    exStyle &= ~(nint)(Windows.Win32.UI.WindowsAndMessaging.WINDOW_EX_STYLE.WS_EX_LAYERED);
                }

                PInvoke.InvalidateRect(windowManager.HWND, bErase: true);
            }
        }

        public Windows.UI.Color BackgroundColor
        {
            get { return (Windows.UI.Color)GetValue(BackgroundColorProperty); }
            set { SetValue(BackgroundColorProperty, value); }
        }

        public static readonly DependencyProperty BackgroundColorProperty =
            DependencyProperty.Register("BackgroundColor", typeof(Windows.UI.Color), typeof(TransparentBackdrop), new PropertyMetadata(Windows.UI.Color.FromArgb(0, 255, 255, 255), (s, a) =>
            {
                if (s is TransparentBackdrop sender && !Equals(a.NewValue, a.OldValue))
                {
                    if (sender.colorBrush != null)
                    {
                        sender.colorBrush.Color = (Windows.UI.Color)a.NewValue;
                    }
                }
            }));
    }
}
