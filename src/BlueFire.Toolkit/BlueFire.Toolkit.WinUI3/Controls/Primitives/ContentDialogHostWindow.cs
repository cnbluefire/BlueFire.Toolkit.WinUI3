using BlueFire.Toolkit.WinUI3.Extensions;
using BlueFire.Toolkit.WinUI3.SystemBackdrops;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.Win32;

namespace BlueFire.Toolkit.WinUI3.Controls.Primitives
{
    internal class ContentDialogHostWindow : WindowEx
    {
        private Canvas canvas;
        private Rectangle titleBarRect;
        private bool hasShown;
        private bool contentReady;
        private TaskCompletionSource loadedTaskSource;
        private readonly ContentDialog contentDialog;

        internal ContentDialogHostWindow(ContentDialog contentDialog)
        {
            if (contentDialog == null) throw new ArgumentNullException(nameof(contentDialog));
            if (contentDialog.Parent != null) throw new InvalidOperationException(nameof(contentDialog.Parent));
            if (IsContentDialogOpened(contentDialog)) throw new InvalidOperationException("ContentDialog is already opened.");

            this.contentDialog = contentDialog;

            var configuration = new AcrylicBackdropConfiguration();
            configuration.SetTheme(contentDialog.ActualTheme switch
            {
                ElementTheme.Dark => SystemBackdropTheme.Dark,
                ElementTheme.Light => SystemBackdropTheme.Light,
                _ => SystemBackdropTheme.Default
            });

            var backdrop = new MaterialCardBackdrop
            {
                BorderThickness = 0,
                Margin = new(0),
                MaterialConfiguration = new AcrylicBackdropConfiguration(),
                Visible = false
            };

            this.SystemBackdrop = backdrop;

            loadedTaskSource = new TaskCompletionSource();

            this.Title = "ContentDialogHostWindow";
            this.Content = (canvas = new Canvas()
            {
                Opacity = 0,
                Children =
                {
                    contentDialog,
                    (titleBarRect = new Rectangle()
                    {
                        Height = 24,
                        Fill = new SolidColorBrush(Colors.Transparent)
                    })
                }
            });

            Canvas.SetZIndex(titleBarRect, 999);
            this.XamlWindow.ExtendsContentIntoTitleBar = true;

            canvas.Loaded += (s, a) =>
            {
                loadedTaskSource.TrySetResult();
            };

            contentDialog.SizeChanged += (s, a) =>
            {
                if (!contentReady) return;
                UpdateClientSize(false);
            };

            contentDialog.Opened += (s, a) =>
            {
                canvas.Opacity = 1;
                backdrop.Visible = true;
            };

            contentDialog.Closing += (s, a) =>
            {
                DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.High, () =>
                {
                    if (!a.Cancel)
                    {
                        backdrop.Visible = false;
                        canvas.Opacity = 0;

                        // 快速触发 Closed 事件
                        if (VisualTreeHelper.GetChildrenCount(contentDialog) > 0
                            && VisualTreeHelper.GetChild(contentDialog, 0) is FrameworkElement dialogLayoutRoot)
                        {
                            var visualStateGroup = VisualStateManager.GetVisualStateGroups(dialogLayoutRoot)?
                                .FirstOrDefault(c => c.Name == "DialogShowingStates");

                            if (visualStateGroup != null
                                && visualStateGroup.CurrentState?.Name != "DialogHidden")
                            {
                                VisualStateManager.GoToState(contentDialog, "DialogHidden", false);
                            }
                        }
                    }
                });
            };

            contentDialog.Closed += (s, a) =>
            {
                ContentDialogResult = a.Result;

                this.AppWindow.SetDialogResult(ContentDialogResult switch
                {
                    ContentDialogResult.Primary => true,
                    ContentDialogResult.Secondary => false,
                    _ => null
                });
                canvas.Children.Remove(contentDialog);
            };
        }

        internal ContentDialogResult ContentDialogResult { get; private set; } = ContentDialogResult.None;

        private static bool IsContentDialogOpened(ContentDialog contentDialog)
        {
            var container = GetContentDialogChildContainer(contentDialog);
            if (container != null)
            {
                var vsm = VisualStateManager.GetVisualStateGroups(container);
                return vsm.FirstOrDefault(c => c.Name == "DialogShowingStates")?
                    .CurrentState?.Name != "DialogHidden";
            }
            return false;
        }

        private static FrameworkElement? GetContentDialogChildContainer(ContentDialog contentDialog)
        {
            if (VisualTreeHelper.GetChildrenCount(contentDialog) > 0
                && VisualTreeHelper.GetChild(contentDialog, 0) is FrameworkElement container)
            {
                return container;
            }
            return null;
        }

        private Task WaitPresenterLoadedAsync() => loadedTaskSource.Task;

        private void UpdateClientSize(bool centerToOwner)
        {
            if (AppWindow == null) return;

            contentDialog.ApplyTemplate();
            contentDialog.Measure(new Windows.Foundation.Size(double.MaxValue, double.MaxValue));

            var scale = 1d;
            if (contentDialog.XamlRoot != null)
            {
                scale = contentDialog.XamlRoot.RasterizationScale;
            }
            else
            {
                scale = GetOwnerWindowDpi(this.Handle) / 96d;
            }

            var clientSize = contentDialog.DesiredSize;

            AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(
                (int)(scale * clientSize.Width),
                (int)(scale * clientSize.Height)));

            if (centerToOwner)
            {
                PInvoke.SendMessage(this.Handle, WindowDialogExtensions.WM_MOVECENTER, 0, 0);
            }

            titleBarRect.Width = clientSize.Width;
        }

        protected override void OnDpiChanged(WindowExDpiChangedEventArgs args)
        {
            base.OnDpiChanged(args);

            UpdateClientSize(false);
        }

        protected override async void OnWindowMessageReceived(WindowMessageReceivedEventArgs e)
        {
            base.OnWindowMessageReceived(e);

            if (!hasShown && e.MessageId == PInvoke.WM_SHOWWINDOW && e.WParam != 0)
            {
                hasShown = true;

                var presenter = (OverlappedPresenter)this.AppWindow.Presenter;
                presenter.SetBorderAndTitleBar(false, false);
                presenter.IsResizable = false;
                presenter.IsMinimizable = false;
                presenter.IsMaximizable = false;

                AppWindow?.Resize(new Windows.Graphics.SizeInt32(0, 0));

                await InitializeDialogAsync();
            }
            else if (e.MessageId == PInvoke.WM_SYSCOMMAND)
            {
                if (e.WParam == PInvoke.SC_MAXIMIZE
                    || e.WParam == PInvoke.SC_MINIMIZE
                    || e.WParam == PInvoke.SC_RESTORE)
                {
                    e.Handled = true;
                    e.LResult = 0;
                }
            }
        }

        private async Task InitializeDialogAsync()
        {
            await WaitPresenterLoadedAsync();
            var task = contentDialog.ShowAsync(ContentDialogPlacement.InPlace);

            await Task.Delay(TimeSpan.FromSeconds(0.25));

            UpdateClientSize(true);
            contentReady = true;

            if (task.Status == AsyncStatus.Started)
            {
                this.XamlWindow.SetTitleBar(titleBarRect);
            }
        }

        private static uint GetOwnerWindowDpi(Windows.Win32.Foundation.HWND hWnd)
        {
            var ownerWindow = (Windows.Win32.Foundation.HWND)PInvoke.GetWindowLongAuto(hWnd, Windows.Win32.UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX.GWL_HWNDPARENT);
            if (!ownerWindow.IsNull) return PInvoke.GetDpiForWindow(ownerWindow);

            var monitor = PInvoke.MonitorFromWindow(default, Windows.Win32.Graphics.Gdi.MONITOR_FROM_FLAGS.MONITOR_DEFAULTTOPRIMARY);
            if (PInvoke.GetDpiForMonitor(monitor, Windows.Win32.UI.HiDpi.MONITOR_DPI_TYPE.MDT_DEFAULT, out var dpiX, out var dpiY).Succeeded)
            {
                return dpiX;
            }

            return PInvoke.GetDpiForWindow(hWnd);
        }
    }
}
