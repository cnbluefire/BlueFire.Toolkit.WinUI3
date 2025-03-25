using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace BlueFire.Toolkit.WindowBase
{
    internal partial class WindowMaxMinSizeFeature : WindowFeatureBase
    {
        private const uint WM_UPDATESIZE = PInvoke.WM_USER + 0x0100;
        private volatile bool queued;

        private int minWidth = 0;
        private int minHeight = 0;
        private int maxWidth = int.MaxValue;
        private int maxHeight = int.MaxValue;

        internal WindowMaxMinSizeFeature()
        {
            UpdateMaxSize();
        }

        public int MinWidth
        {
            get => minWidth;
            set
            {
                if (minWidth != value)
                {
                    ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(MinWidth));
                    minWidth = value;
                    UpdateSize();
                }
            }
        }

        public int MinHeight
        {
            get => minHeight;
            set
            {
                if (minHeight != value)
                {
                    ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(MinHeight));
                    minHeight = value;
                    UpdateSize();
                }
            }
        }

        public int MaxWidth
        {
            get => maxWidth;
            set
            {
                if (maxWidth != value)
                {
                    ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(MaxWidth));
                    maxWidth = value;
                    UpdateSize();
                }
            }
        }

        public int MaxHeight
        {
            get => maxHeight;
            set
            {
                if (maxHeight != value)
                {
                    ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(MaxHeight));
                    maxHeight = value;
                    UpdateSize();
                }
            }
        }

        private void UpdateSize()
        {
            if (WindowHandle == 0) return;

            Interlocked.Exchange(ref queued, true);
            PInvoke.PostMessage((HWND)WindowHandle, WM_UPDATESIZE, default, default);
        }

        private void UpdateMaxSize()
        {
            maxWidth = PInvoke.GetSystemMetricsForDpi(SYSTEM_METRICS_INDEX.SM_CXMAXTRACK, 96);
            maxHeight = PInvoke.GetSystemMetricsForDpi(SYSTEM_METRICS_INDEX.SM_CYMAXTRACK, 96);
        }

        public unsafe override void OnMessageReceived(WindowMessageReceivedEventArgs args)
        {
            if (args.MessageId == WM_UPDATESIZE)
            {
                RECT rect;

                if (Interlocked.Exchange(ref queued, false)
                    && PInvoke.GetWindowRect((HWND)args.WindowHandle, &rect))
                {
                    PInvoke.SetWindowPos(
                        (HWND)args.WindowHandle,
                        default,
                        0, 0,
                        rect.Width, rect.Height,
                        SET_WINDOW_POS_FLAGS.SWP_NOZORDER
                        | SET_WINDOW_POS_FLAGS.SWP_NOMOVE
                        | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
                }
            }
            else if (args.MessageId == PInvoke.WM_GETMINMAXINFO)
            {
                var pMinMaxInfo = (MINMAXINFO*)args.LParam;

                if (MinWidth > 0) pMinMaxInfo->ptMinTrackSize.X = MinWidth;
                if (MinHeight > 0) pMinMaxInfo->ptMinTrackSize.Y = MinHeight;

                if (MaxWidth > 0 && MaxWidth < int.MaxValue) pMinMaxInfo->ptMaxTrackSize.X = MinWidth;
                if (MaxHeight > 0 && MaxHeight < int.MaxValue) pMinMaxInfo->ptMaxTrackSize.Y = MaxHeight;
            }
            else if (args.MessageId == PInvoke.WM_DISPLAYCHANGE)
            {
                UpdateMaxSize();
            }
        }

        protected override void DisposeCore(nint windowHandle) { }
    }
}
