# BlueFire.Toolkit.WinUI3
WinUI3的帮助库，目前还处于开发的早期阶段。

## WindowEx
### BlueFire.Toolkit.WinUI3.WindowBase.WindowEx
Window的封装
```xml
<?xml version="1.0" encoding="utf-8"?>
<windowBase:WindowEx
    x:Class="BlueFire.Toolkit.Sample.WinUI3.MainWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:local="using:BlueFire.Toolkit.Sample.WinUI3"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    xmlns:windowBase="using:BlueFire.Toolkit.WinUI3.WindowBase"
    mc:Ignorable="d"    
    Title="Custom Title"
    Width="500" MinWidth="400" MaxWidth="800"
    Height="350" MinHeight="350" MaxHeight="500">

    <TextBlock>Test</TextBlock>

</windowBase:WindowEx>
```

## WindowManager
### BlueFire.Toolkit.WinUI3.WindowBase.WindowManager
窗口消息帮助类
```cs
var manager = WindowManager.Get(AppWindow.Id);
manager.WindowMessageReceived += WindowManager_WindowMessageReceived;
```

```cs
private void WindowManager_WindowMessageReceived(WindowManager sender, WindowMessageReceivedEventArgs e)
{
    if (e.MessageId == PInvoke.WM_DPICHANGED)
    {
        ...
    }
}
```

## CompositionSurfaceLoader
### BlueFire.Toolkit.WinUI3.Media.CompositionSurfaceLoader
从图片创建 Windows.UI.Composition.ICompositionSurface 对象
```cs
public class MainWindow : WindowEx
{
    private CompositionSurfaceLoader surfaceLoader;

    public MainWindow()
    {
        this.InitializeComponent();

        surfaceLoader = CompositionSurfaceLoader.StartLoadFromUri(new Uri("https://www.microsoft.com/favicon.ico?v2"));

        var rootVisual = WindowsCompositionHelper.Compositor.CreateSpriteVisual();
        rootVisual.RelativeSizeAdjustment = System.Numerics.Vector2.One;

        var brush = WindowsCompositionHelper.Compositor.CreateSurfaceBrush(surfaceLoader.Surface);

        brush.Stretch = Windows.UI.Composition.CompositionStretch.Uniform;
        brush.HorizontalAlignmentRatio = 0.5f;
        brush.VerticalAlignmentRatio = 0.5f;

        rootVisual.Brush = brush;

        this.RootVisual = rootVisual;
    }
}

```

## WindowBackdropBase
### BlueFire.Toolkit.WinUI3.SystemBackdrops.WindowBackdropBase
为SystemBackdrop提供WindowId
```cs
public class MyBackdrop : WindowBackdropBase
{
    protected override void OnTargetConnected(WindowId windowId, ICompositionSupportsSystemBackdrop connectedTarget, XamlRoot xamlRoot)
    {
        base.OnTargetConnected(windowId, connectedTarget, xamlRoot);

        var manager = WindowManager.Get(windowId);
        
        ...
    }

}
```