using BlueFire.Toolkit.WinUI3.Compositions;
using Microsoft.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32.Foundation;
using WinComposition = Windows.UI.Composition;
using PInvoke = Windows.Win32.PInvoke;

namespace BlueFire.Toolkit.WinUI3.WindowBase
{
    internal class WindowCompositionResources : IDisposable
    {
        private readonly WindowManager windowManager;

        private bool disposeValue;
        private WinComposition.Desktop.DesktopWindowTarget? desktopWindowTarget;
        private WinComposition.ContainerVisual? rootVisual;
        private WinComposition.SpriteVisual? windowContentVisual;
        private WinComposition.SpriteVisual? backdropVisual;

        internal WindowCompositionResources(WindowManager windowManager)
        {
            this.windowManager = windowManager;

            if (!PInvoke.IsWindow(windowManager.HWND)) throw new ArgumentException(nameof(windowManager.HWND));

            var style = unchecked((uint)PInvoke.GetWindowLongAuto(windowManager.HWND, Windows.Win32.UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX.GWL_STYLE));
            if (!CreateDesktopWindowTarget())
            {
                var monitor = windowManager.GetMonitorInternal();
                if (monitor != null)
                {
                    monitor.WindowMessageBeforeReceived += WindowCompositionResources_WindowMessageBeforeReceived;
                }
            }
        }

        internal WinComposition.SpriteVisual WindowContentVisual => EnsureWindowContentVisual();

        internal WinComposition.SpriteVisual BackdropVisual => EnsureBackdropVisual();

        private async void WindowCompositionResources_WindowMessageBeforeReceived(WindowManager sender, WindowMessageReceivedEventArgs e)
        {
            if (e.MessageId == PInvoke.WM_SHOWWINDOW)
            {
                var monitor = windowManager.GetMonitorInternal();
                if (monitor != null)
                {
                    monitor.WindowMessageBeforeReceived -= WindowCompositionResources_WindowMessageBeforeReceived;
                }

                await Task.Yield();
                CreateDesktopWindowTarget();
            }
        }

        public bool CreateDesktopWindowTarget()
        {
            if (PInvoke.IsWindow(windowManager.HWND))
            {
                var style = unchecked((uint)PInvoke.GetWindowLongAuto(windowManager.HWND, Windows.Win32.UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX.GWL_STYLE));
                if ((style & (uint)Windows.Win32.UI.WindowsAndMessaging.WINDOW_STYLE.WS_VISIBLE) != 0)
                {
                    EnsureDesktopWindowTarget();
                    return true;
                }
            }

            return false;
        }

        private WinComposition.Desktop.DesktopWindowTarget EnsureDesktopWindowTarget()
        {
            ThrowIfDisposed();

            if (desktopWindowTarget == null)
            {
                desktopWindowTarget = WindowsCompositionHelper.CreateDesktopWindowTarget(windowManager.WindowId, false);

                desktopWindowTarget.Root = EnsureRootVisual();
            }

            return desktopWindowTarget;
        }

        private WinComposition.ContainerVisual EnsureRootVisual()
        {
            ThrowIfDisposed();

            if (rootVisual == null)
            {
                rootVisual = WindowsCompositionHelper.Compositor.CreateContainerVisual();
                rootVisual.RelativeSizeAdjustment = Vector2.One;

                if (windowContentVisual != null)
                {
                    rootVisual.Children.InsertAtTop(windowContentVisual);
                }

                if (backdropVisual != null)
                {
                    rootVisual.Children.InsertAtBottom(backdropVisual);
                }
            }

            return rootVisual;
        }

        private WinComposition.SpriteVisual EnsureWindowContentVisual()
        {
            ThrowIfDisposed();

            if (windowContentVisual == null)
            {
                windowContentVisual = WindowsCompositionHelper.Compositor.CreateSpriteVisual();
                windowContentVisual.RelativeSizeAdjustment = Vector2.One;

                if (rootVisual != null)
                {
                    rootVisual.Children.InsertAtTop(windowContentVisual);
                }
            }

            return windowContentVisual;
        }

        private WinComposition.SpriteVisual EnsureBackdropVisual()
        {
            ThrowIfDisposed();

            if (backdropVisual == null)
            {
                backdropVisual = WindowsCompositionHelper.Compositor.CreateSpriteVisual();
                backdropVisual.RelativeSizeAdjustment = Vector2.One;

                if (rootVisual != null)
                {
                    rootVisual.Children.InsertAtBottom(backdropVisual);
                }
            }

            return backdropVisual;
        }
        private void ThrowIfDisposed()
        {
            if (disposeValue)
            {
                throw new ObjectDisposedException(nameof(WindowCompositionResources));
            }
        }

        public void Dispose()
        {
            if (!disposeValue)
            {
                disposeValue = true;

                if (desktopWindowTarget != null)
                {
                    desktopWindowTarget.Root = null;
                    desktopWindowTarget.Dispose();
                    desktopWindowTarget = null;
                }

                if (rootVisual != null)
                {
                    rootVisual.Children.RemoveAll();
                    rootVisual.Dispose();
                    rootVisual = null;
                }

                if (windowContentVisual != null)
                {
                    windowContentVisual.Children.RemoveAll();
                    windowContentVisual.Dispose();
                    windowContentVisual = null;
                }

                if (backdropVisual != null)
                {
                    backdropVisual.Children.RemoveAll();
                    backdropVisual.Dispose();
                    backdropVisual = null;
                }
            }
        }
    }
}
