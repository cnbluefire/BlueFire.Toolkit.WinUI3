using BlueFire.Toolkit.WinUI3.Compositions;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Color = Windows.UI.Color;

namespace BlueFire.Toolkit.WinUI3.SystemBackdrops
{
    public class MaterialCardBackdrop : TransparentBackdrop
    {
        private static MaterialCardBackdropConfiguration defaultConfiguration = new AcrylicBackdropConfiguration();

        private MaterialCardBackdropConfiguration? currentConfiguration;
        private CompositionBrushProvider? brushProvider;

        #region Dependency Properties

        /// <inheritdoc cref="BlueFire.Toolkit.WinUI3.SystemBackdrops.MaterialCardBackdropConfiguration.Parse(string?)"/>
        public MaterialCardBackdropConfiguration MaterialConfiguration
        {
            get { return (MaterialCardBackdropConfiguration)GetValue(MaterialConfigurationProperty); }
            set { SetValue(MaterialConfigurationProperty, value); }
        }

        public static readonly DependencyProperty MaterialConfigurationProperty =
            DependencyProperty.Register(
                "MaterialConfiguration",
                typeof(MaterialCardBackdropConfiguration),
                typeof(MaterialCardBackdrop),
                new PropertyMetadata(
                    null,
                    (s, a) => ((MaterialCardBackdrop)s).SetMaterialConfiguration((MaterialCardBackdropConfiguration?)a.NewValue)));

        private void SetMaterialConfiguration(MaterialCardBackdropConfiguration? configuration)
        {
            lock (this.ControllerEntries)
            {
                if (configuration != currentConfiguration)
                {
                    if (currentConfiguration != null)
                    {
                        currentConfiguration.PropertyChanged -= Configuration_PropertyChanged;
                    }

                    currentConfiguration = configuration;

                    if (currentConfiguration != null)
                    {
                        currentConfiguration.PropertyChanged += Configuration_PropertyChanged;
                    }

                    UpdateConfiguration();
                }
            }
        }

        public double CornerRadius
        {
            get { return (double)GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register("CornerRadius", typeof(double), typeof(MaterialCardBackdrop),
                new PropertyMetadata(
                    8d,
                    (s, a) => ((MaterialCardBackdrop)s).UpdateVisualProperties(a.Property)));


        public Thickness Margin
        {
            get { return (Thickness)GetValue(MarginProperty); }
            set { SetValue(MarginProperty, value); }
        }

        public static readonly DependencyProperty MarginProperty =
            DependencyProperty.Register("Margin", typeof(Thickness), typeof(MaterialCardBackdrop),
                new PropertyMetadata(
                    new Thickness(0),
                    (s, a) => ((MaterialCardBackdrop)s).UpdateVisualProperties(a.Property)));


        public double Opacity
        {
            get { return (double)GetValue(OpacityProperty); }
            set { SetValue(OpacityProperty, value); }
        }

        public static readonly DependencyProperty OpacityProperty =
            DependencyProperty.Register("Opacity", typeof(double), typeof(MaterialCardBackdrop),
                new PropertyMetadata(
                    1d,
                    (s, a) => ((MaterialCardBackdrop)s).UpdateVisualProperties(a.Property)));


        public bool Visible
        {
            get { return (bool)GetValue(VisibleProperty); }
            set { SetValue(VisibleProperty, value); }
        }

        public static readonly DependencyProperty VisibleProperty =
            DependencyProperty.Register("Visible", typeof(bool), typeof(MaterialCardBackdrop),
                new PropertyMetadata(
                    true,
                    (s, a) => ((MaterialCardBackdrop)s).UpdateVisualProperties(a.Property)));


        public Color BorderColor
        {
            get { return (Color)GetValue(BorderColorProperty); }
            set { SetValue(BorderColorProperty, value); }
        }

        public static readonly DependencyProperty BorderColorProperty =
            DependencyProperty.Register("BorderColor", typeof(Color), typeof(MaterialCardBackdrop),
                new PropertyMetadata(
                    Color.FromArgb(102, 117, 117, 117),
                    (s, a) => ((MaterialCardBackdrop)s).UpdateVisualProperties(a.Property)));


        public double BorderThickness
        {
            get { return (double)GetValue(BorderThicknessProperty); }
            set { SetValue(BorderThicknessProperty, value); }
        }

        public static readonly DependencyProperty BorderThicknessProperty =
            DependencyProperty.Register("BorderThickness", typeof(double), typeof(MaterialCardBackdrop),
                new PropertyMetadata(
                    1d,
                    (s, a) => ((MaterialCardBackdrop)s).UpdateVisualProperties(a.Property)));


        public Color ShadowColor
        {
            get { return (Color)GetValue(ShadowColorProperty); }
            set { SetValue(ShadowColorProperty, value); }
        }

        public static readonly DependencyProperty ShadowColorProperty =
            DependencyProperty.Register("ShadowColor", typeof(Color), typeof(MaterialCardBackdrop),
                new PropertyMetadata(
                    Color.FromArgb(255, 0, 0, 0),
                    (s, a) => ((MaterialCardBackdrop)s).UpdateVisualProperties(a.Property)));


        public double ShadowOpacity
        {
            get { return (double)GetValue(ShadowOpacityProperty); }
            set { SetValue(ShadowOpacityProperty, value); }
        }

        public static readonly DependencyProperty ShadowOpacityProperty =
            DependencyProperty.Register("ShadowOpacity", typeof(double), typeof(MaterialCardBackdrop),
                new PropertyMetadata(
                    0.8d,
                    (s, a) => ((MaterialCardBackdrop)s).UpdateVisualProperties(a.Property)));


        public double ShadowBlurRadius
        {
            get { return (double)GetValue(ShadowBlurRadiusProperty); }
            set { SetValue(ShadowBlurRadiusProperty, value); }
        }

        public static readonly DependencyProperty ShadowBlurRadiusProperty =
            DependencyProperty.Register("ShadowBlurRadius", typeof(double), typeof(MaterialCardBackdrop),
                new PropertyMetadata(
                    12d,
                    (s, a) => ((MaterialCardBackdrop)s).UpdateVisualProperties(a.Property)));


        public Point ShadowOffset
        {
            get { return (Point)GetValue(ShadowOffsetProperty); }
            set { SetValue(ShadowOffsetProperty, value); }
        }

        public static readonly DependencyProperty ShadowOffsetProperty =
            DependencyProperty.Register("ShadowOffset", typeof(Point), typeof(MaterialCardBackdrop),
                new PropertyMetadata(
                    new Point(0, 3),
                    (s, a) => ((MaterialCardBackdrop)s).UpdateVisualProperties(a.Property)));

        private void UpdateVisualProperties(DependencyProperty? dp, MaterialCardBackdropControllerEntry? entry)
        {
            lock (this.ControllerEntries)
            {
                if (entry?.VisualHelper != null)
                {
                    if (Change(dp, CornerRadiusProperty))
                        entry.VisualHelper.CornerRadius = CornerRadius;

                    if (Change(dp, MarginProperty))
                        entry.VisualHelper.Margin = Margin;

                    if (Change(dp, OpacityProperty))
                        entry.VisualHelper.Opacity = Opacity;

                    if (Change(dp, VisibleProperty))
                        entry.VisualHelper.Visible = Visible;

                    if (Change(dp, BorderColorProperty))
                        entry.VisualHelper.BorderColor = BorderColor;

                    if (Change(dp, BorderThicknessProperty))
                        entry.VisualHelper.BorderThickness = BorderThickness;

                    if (Change(dp, ShadowColorProperty))
                        entry.VisualHelper.ShadowColor = ShadowColor;

                    if (Change(dp, ShadowOpacityProperty))
                        entry.VisualHelper.ShadowOpacity = ShadowOpacity;

                    if (Change(dp, ShadowBlurRadiusProperty))
                        entry.VisualHelper.ShadowBlurRadius = ShadowBlurRadius;

                    if (Change(dp, ShadowOffsetProperty))
                        entry.VisualHelper.ShadowOffset = ShadowOffset.ToVector2();
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static bool Change(DependencyProperty? _dp, DependencyProperty _test) => _dp == null || _dp == _test;
        }

        private void UpdateVisualProperties(DependencyProperty? dp)
        {
            lock (this.ControllerEntries)
            {
                foreach (var entry in this.ControllerEntries.OfType<MaterialCardBackdropControllerEntry>())
                {
                    UpdateVisualProperties(dp, entry);
                }
            }

        }

        #endregion Dependency Properties

        private void Configuration_PropertyChanged(object? sender, string? e)
        {
            UpdateBrushProviderProperties(e);
        }

        private void UpdateConfiguration()
        {
            lock (this.ControllerEntries)
            {
                bool flag = false;

                if (currentConfiguration == null && brushProvider != null) flag = true;
                else if (currentConfiguration != null && brushProvider == null) flag = true;
                else if (currentConfiguration is AcrylicBackdropConfiguration && brushProvider is not AcrylicBrushProvider) flag = true;
                else if (currentConfiguration is MicaBackdropConfiguration && brushProvider is not MicaBrushProvider) flag = true;

                if (flag)
                {
                    if (brushProvider != null)
                    {
                        foreach (var item in this.ControllerEntries.OfType<MaterialCardBackdropControllerEntry>())
                        {
                            if (item.VisualHelper != null) item.VisualHelper.BrushProvider = null;
                        }
                        brushProvider.Dispose();
                        brushProvider = null;
                    }
                }

                if (currentConfiguration != null)
                {
                    brushProvider = currentConfiguration switch
                    {
                        AcrylicBackdropConfiguration => new AcrylicBrushProvider(),
                        MicaBackdropConfiguration => new MicaBrushProvider(),
                        _ => throw new NotSupportedException("Only support AcrylicBackdropConfiguration and MicaBackdropConfiguration.")
                    };

                    foreach (var item in this.ControllerEntries.OfType<MaterialCardBackdropControllerEntry>())
                    {
                        if (item.VisualHelper != null) item.VisualHelper.BrushProvider = brushProvider;
                    }
                }

                UpdateBrushProviderProperties(null);
            }
        }

        private void UpdateBrushProviderProperties(string? propName)
        {
            lock (this.ControllerEntries)
            {
                if (this.currentConfiguration != null && this.brushProvider != null)
                {
                    if (Change(propName, nameof(MaterialCardBackdropConfiguration.AlwaysUseFallback)))
                        this.brushProvider.UseFallback = this.currentConfiguration.AlwaysUseFallback;

                    if (Change(propName, nameof(MaterialCardBackdropConfiguration.FallbackColor)))
                        this.brushProvider.FallbackColor = this.currentConfiguration.FallbackColor;

                    {
                        if (this.currentConfiguration is AcrylicBackdropConfiguration configuration
                            && this.brushProvider is AcrylicBrushProvider brushProvider)
                        {
                            if (Change(propName, nameof(AcrylicBackdropConfiguration.TintColor)))
                                brushProvider.TintColor = configuration.TintColor;

                            if (Change(propName, nameof(AcrylicBackdropConfiguration.TintOpacity)))
                                brushProvider.TintOpacity = configuration.TintOpacity;

                            if (Change(propName, nameof(AcrylicBackdropConfiguration.TintLuminosityOpacity)))
                                brushProvider.TintLuminosityOpacity = configuration.TintLuminosityOpacity;

                            if (Change(propName, nameof(AcrylicBackdropConfiguration.UseHostBackdropBrush)))
                                brushProvider.UseHostBackdropBrush = configuration.UseHostBackdropBrush;

                            if (Change(propName, nameof(AcrylicBackdropConfiguration.BlurAmount)))
                                brushProvider.BlurAmount = configuration.BlurAmount;
                        }
                    }
                    {
                        if (this.currentConfiguration is MicaBackdropConfiguration configuration
                            && this.brushProvider is MicaBrushProvider brushProvider)
                        {
                            if (Change(propName, nameof(MicaBackdropConfiguration.TintColor)))
                                brushProvider.TintColor = configuration.TintColor;

                            if (Change(propName, nameof(MicaBackdropConfiguration.TintOpacity)))
                                brushProvider.TintOpacity = configuration.TintOpacity;

                            if (Change(propName, nameof(MicaBackdropConfiguration.LuminosityOpacity)))
                                brushProvider.LuminosityOpacity = configuration.LuminosityOpacity;
                        }
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static bool Change(string? _test, string? prop) => _test == null || _test == prop;
        }

        public IReadOnlyDictionary<NonClientRegionKind, Windows.Graphics.RectInt32[]> GetNonClientRegions(WindowId windowId, IReadOnlyList<NonClientRegionKind> kinds, TitleBarHeightOption titleBarHeightOption = TitleBarHeightOption.Standard)
        {
            const int SizeBorderPixels = 8;
            const int StandardTitleBarHeight = 32;
            const int TallTitleBarHeight = 48;

            var dict = new Dictionary<NonClientRegionKind, Windows.Graphics.RectInt32[]?>();

            if (kinds != null && kinds.Count > 0)
            {
                var hWnd = (Windows.Win32.Foundation.HWND)Win32Interop.GetWindowFromWindowId(windowId);

                if (Windows.Win32.PInvoke.IsWindow(hWnd))
                {
                    var style = (uint)Windows.Win32.PInvoke.GetWindowLongAuto(hWnd, Windows.Win32.UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX.GWL_STYLE);
                    if ((style & (uint)(Windows.Win32.UI.WindowsAndMessaging.WINDOW_STYLE.WS_CHILD)) == 0)
                    {
                        // top level window

                        if (Windows.Win32.PInvoke.GetWindowRect(hWnd, out var windowRect)
                            && Windows.Win32.PInvoke.GetClientRect(hWnd, out var clientRect))
                        {
                            var dpi = Windows.Win32.PInvoke.GetDpiForWindow(hWnd);

                            var margins = (
                                left: (int)(Margin.Left * dpi / 96d),
                                top: (int)(Margin.Top * dpi / 96d),
                                right: (int)(Margin.Right * dpi / 96d),
                                bottom: (int)(Margin.Bottom * dpi / 96d));

                            for (var i = 0; i < kinds.Count; i++)
                            {
                                var kind = kinds[i];
                                if (!dict.ContainsKey(kind))
                                {
                                    switch (kind)
                                    {
                                        case NonClientRegionKind.Close:
                                        case NonClientRegionKind.Icon:
                                        case NonClientRegionKind.Maximize:
                                        case NonClientRegionKind.Minimize:
                                        case NonClientRegionKind.Passthrough:
                                        default:
                                            dict[kind] = null;
                                            break;

                                        case NonClientRegionKind.LeftBorder:
                                            {
                                                dict[kind] = new[] { new Windows.Graphics.RectInt32(
                                                    clientRect.left + margins.left - SizeBorderPixels,
                                                    clientRect.top,
                                                    SizeBorderPixels,
                                                    clientRect.Height) };
                                            }
                                            break;

                                        case NonClientRegionKind.TopBorder:
                                            {
                                                dict[kind] = new[] { new Windows.Graphics.RectInt32(
                                                    clientRect.left,
                                                    clientRect.top + margins.top - SizeBorderPixels,
                                                    clientRect.Width,
                                                    SizeBorderPixels) };
                                            }
                                            break;

                                        case NonClientRegionKind.RightBorder:
                                            {
                                                dict[kind] = new[] { new Windows.Graphics.RectInt32(
                                                    clientRect.right - margins.right - SizeBorderPixels,
                                                    clientRect.top,
                                                    SizeBorderPixels,
                                                    clientRect.Height) };
                                            }
                                            break;

                                        case NonClientRegionKind.BottomBorder:
                                            {
                                                dict[kind] = new[] { new Windows.Graphics.RectInt32(
                                                    clientRect.left,
                                                    clientRect.bottom - margins.top - SizeBorderPixels,
                                                    clientRect.Width,
                                                    SizeBorderPixels) };
                                            }
                                            break;

                                        case NonClientRegionKind.Caption:
                                            {
                                                switch (titleBarHeightOption)
                                                {
                                                    case TitleBarHeightOption.Collapsed:
                                                        dict[kind] = Array.Empty<Windows.Graphics.RectInt32>();
                                                        break;

                                                    case TitleBarHeightOption.Tall:
                                                        dict[kind] = new[] { new Windows.Graphics.RectInt32(
                                                            clientRect.left + margins.left,
                                                            clientRect.top + margins.top,
                                                            Math.Max(clientRect.Width - margins.left - margins.right, 0),
                                                            TallTitleBarHeight) };
                                                        break;

                                                    case TitleBarHeightOption.Standard:
                                                    default:
                                                        dict[kind] = new[] { new Windows.Graphics.RectInt32(
                                                            clientRect.left + margins.left,
                                                            clientRect.top + margins.top,
                                                            Math.Max(clientRect.Width - margins.left - margins.right, 0),
                                                            StandardTitleBarHeight) };
                                                        break;
                                                }
                                            }
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            foreach (var key in dict.Keys.ToArray())
            {
                if (dict[key] == null) dict.Remove(key);
            }

#pragma warning disable IDE0004
            return (IReadOnlyDictionary<NonClientRegionKind, Windows.Graphics.RectInt32[]>)dict;
#pragma warning restore IDE0004
        }

        internal override TransparentBackdropControllerEntry CreateControllerEntry(ICompositionSupportsSystemBackdrop connectedTarget, XamlRoot xamlRoot)
        {
            var entry = new MaterialCardBackdropControllerEntry(connectedTarget, xamlRoot.ContentIslandEnvironment.AppWindowId);

            if (entry.VisualHelper != null)
            {
                entry.VisualHelper.BrushProvider = brushProvider;
            }

            UpdateVisualProperties(null, entry);

            return entry;
        }

        private class MaterialCardBackdropControllerEntry : TransparentBackdropControllerEntry
        {
            private bool attached;
            private MaterialCardVisualHelper? visualHelper;
            private WindowManager? windowManager;

            internal MaterialCardBackdropControllerEntry(ICompositionSupportsSystemBackdrop connectedTarget, WindowId windowId) : base(connectedTarget, windowId)
            {
            }

            internal MaterialCardVisualHelper? VisualHelper => visualHelper;

            protected override unsafe void OnAttached(ICompositionSupportsSystemBackdrop connectedTarget, WindowId windowId)
            {
                base.OnAttached(connectedTarget, windowId);
                attached = true;

                var hWnd = (Windows.Win32.Foundation.HWND)Win32Interop.GetWindowFromWindowId(windowId);
                var exStyle = (uint)Windows.Win32.PInvoke.GetWindowLongAuto(hWnd, Windows.Win32.UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
                exStyle |= (uint)Windows.Win32.UI.WindowsAndMessaging.WINDOW_EX_STYLE.WS_EX_LAYERED;
                Windows.Win32.PInvoke.SetWindowLongAuto(hWnd, Windows.Win32.UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (nint)exStyle);

                Windows.Win32.PInvoke.SetLayeredWindowAttributes(hWnd, default, 255, Windows.Win32.UI.WindowsAndMessaging.LAYERED_WINDOW_ATTRIBUTES_FLAGS.LWA_ALPHA);

                visualHelper = new MaterialCardVisualHelper();
                windowManager = WindowManager.Get(windowId)!;

                windowManager.BackdropVisual.Children.InsertAtTop(visualHelper.Visual);
                visualHelper.RasterizationScale = windowManager.WindowDpi / 96d;

                if (Windows.Win32.PInvoke.GetClientRect((Windows.Win32.Foundation.HWND)Win32Interop.GetWindowFromWindowId(WindowId), out var rect))
                {
                    visualHelper.HostSizeInPixels = new Windows.Graphics.SizeInt32(rect.Width, rect.Height);
                }

                if (AcrylicBrushProvider.IsDwmHostBackdropBrushSupported)
                {
                    int value = 1;
                    Windows.Win32.PInvoke.DwmSetWindowAttribute(hWnd, Windows.Win32.Graphics.Dwm.DWMWINDOWATTRIBUTE.DWMWA_USE_HOSTBACKDROPBRUSH, &value, sizeof(int));
                }
            }

            protected override void OnDetached(ICompositionSupportsSystemBackdrop connectedTarget, WindowId windowId)
            {
                base.OnDetached(connectedTarget, windowId);
                attached = false;

                if (visualHelper != null)
                {
                    if (windowManager?.AppWindow != null)
                    {
                        windowManager.BackdropVisual.Children.Remove(visualHelper.Visual);
                    }
                }

                if (!CloseRequested)
                {
                    var hWnd = (Windows.Win32.Foundation.HWND)Win32Interop.GetWindowFromWindowId(windowId);
                    var exStyle = (uint)Windows.Win32.PInvoke.GetWindowLongAuto(hWnd, Windows.Win32.UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
                    exStyle &= ~(uint)Windows.Win32.UI.WindowsAndMessaging.WINDOW_EX_STYLE.WS_EX_LAYERED;
                    Windows.Win32.PInvoke.SetWindowLongAuto(hWnd, Windows.Win32.UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (nint)exStyle);
                }
            }

            internal override unsafe void WndProc(WindowManager sender, WindowMessageReceivedEventArgs e)
            {
                base.WndProc(sender, e);

                if (attached)
                {
                    if (e.MessageId == Windows.Win32.PInvoke.WM_STYLECHANGING
                        && e.WParam == unchecked((uint)Windows.Win32.UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE))
                    {
                        var styleStruct = (Windows.Win32.UI.WindowsAndMessaging.STYLESTRUCT*)e.LParam;

                        if ((styleStruct->styleNew & (uint)Windows.Win32.UI.WindowsAndMessaging.WINDOW_EX_STYLE.WS_EX_LAYERED) == 0)
                        {
                            styleStruct->styleNew |= (uint)Windows.Win32.UI.WindowsAndMessaging.WINDOW_EX_STYLE.WS_EX_LAYERED;
                            Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.High, () =>
                            {
                                if (attached)
                                {
                                    var hWnd = (Windows.Win32.Foundation.HWND)Win32Interop.GetWindowFromWindowId(WindowId);

                                    Windows.Win32.PInvoke.SetLayeredWindowAttributes(hWnd, default, 255, Windows.Win32.UI.WindowsAndMessaging.LAYERED_WINDOW_ATTRIBUTES_FLAGS.LWA_ALPHA);
                                }
                            });
                        }
                    }

                    if (visualHelper != null)
                    {
                        if (e.MessageId == Windows.Win32.PInvoke.WM_SIZE)
                        {
                            if (Windows.Win32.PInvoke.GetClientRect((Windows.Win32.Foundation.HWND)Win32Interop.GetWindowFromWindowId(WindowId), out var rect))
                            {
                                visualHelper.HostSizeInPixels = new Windows.Graphics.SizeInt32(rect.Width, rect.Height);
                            }
                        }
                        else if (e.MessageId == Windows.Win32.PInvoke.WM_DPICHANGED)
                        {
                            var dpi = Windows.Win32.PInvoke.HIWORD((uint)e.WParam);
                            visualHelper.RasterizationScale = dpi / 96d;
                        }
                    }
                }
            }
        }
    }
}
