using BlueFire.Toolkit.WinUI3.Extensions;
using BlueFire.Toolkit.WinUI3.SystemBackdrops;
using Microsoft.UI;
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

namespace BlueFire.Toolkit.WinUI3.Core.Controls.Primitives
{
    internal class ContentDialogHostWindow : WindowEx
    {
        private Canvas canvas;
        private Rectangle titleBarRect;
        private bool hasShown;
        private bool contentReady;
        private TaskCompletionSource loadedTaskSource;
        private readonly ContentDialog contentDialog;
        private readonly nint owner;

        internal ContentDialogHostWindow(ContentDialog contentDialog, nint owner)
        {
            if (contentDialog == null) throw new ArgumentNullException(nameof(contentDialog));
            if (contentDialog.Parent != null) throw new InvalidOperationException(nameof(contentDialog.Parent));
            if (IsContentDialogOpened(contentDialog)) throw new InvalidOperationException("ContentDialog is already opened.");

            this.contentDialog = contentDialog;
            this.owner = owner;

            var backdrop = new ColorBackdrop();
            this.SystemBackdrop = backdrop;

            if (Application.Current.Resources.TryGetValue("ContentDialogBackground", out var _ContentDialogBackground)
                && _ContentDialogBackground is SolidColorBrush ContentDialogBackground)
            {
                var color = ContentDialogBackground.Color;
                backdrop.BackgroundColor = Color.FromArgb((byte)(color.A * ContentDialogBackground.Opacity), color.R, color.G, color.B);
            }

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
                DispatcherQueue.TryEnqueue(() =>
                {
                    backdrop.BackgroundColor = Color.FromArgb(0, 255, 255, 255);
                });
            };

            contentDialog.Closing += (s, a) =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (!a.Cancel)
                    {
                        AppWindow?.Hide();
                        PInvoke.SetWindowPos(
                            (Windows.Win32.Foundation.HWND)this.owner,
                            new Windows.Win32.Foundation.HWND(IntPtr.Zero),
                            0, 0, 0, 0,
                            Windows.Win32.UI.WindowsAndMessaging.SET_WINDOW_POS_FLAGS.SWP_NOMOVE
                            | Windows.Win32.UI.WindowsAndMessaging.SET_WINDOW_POS_FLAGS.SWP_NOSIZE);
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
                scale = PInvoke.GetDpiForWindow((Windows.Win32.Foundation.HWND)owner) / 96d;
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

            this.XamlWindow.SetTitleBar(titleBarRect);
        }
    }
}
