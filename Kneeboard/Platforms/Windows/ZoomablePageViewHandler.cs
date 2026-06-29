using Kneeboard.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Kneeboard.Platforms.Windows;

public class ZoomablePageViewHandler : ViewHandler<ZoomablePageView, ScrollViewer>
{
    public static PropertyMapper<ZoomablePageView, ZoomablePageViewHandler> Mapper =
        new(ViewMapper)
        {
            [nameof(ZoomablePageView.ImageSource)] = MapImageSource,
        };

    private Microsoft.UI.Xaml.Controls.Image _image = null!;
    private DispatcherTimer? _tapTimer;
    private global::Windows.Foundation.Point _pendingTapPos;

    public ZoomablePageViewHandler() : base(Mapper) { }

    protected override ScrollViewer CreatePlatformView()
    {
        _image = new Microsoft.UI.Xaml.Controls.Image
        {
            Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform,
        };

        var scrollViewer = new ScrollViewer
        {
            ZoomMode = ZoomMode.Enabled,
            HorizontalScrollMode = Microsoft.UI.Xaml.Controls.ScrollMode.Enabled,
            VerticalScrollMode = Microsoft.UI.Xaml.Controls.ScrollMode.Enabled,
            HorizontalScrollBarVisibility = Microsoft.UI.Xaml.Controls.ScrollBarVisibility.Hidden,
            VerticalScrollBarVisibility = Microsoft.UI.Xaml.Controls.ScrollBarVisibility.Hidden,
            MinZoomFactor = 1.0f,
            MaxZoomFactor = 3.0f,
            Content = _image,
        };

        return scrollViewer;
    }

    protected override void ConnectHandler(ScrollViewer platformView)
    {
        base.ConnectHandler(platformView);
        platformView.SizeChanged += OnSizeChanged;
        platformView.Tapped += OnTapped;
        platformView.DoubleTapped += OnDoubleTapped;
    }

    protected override void DisconnectHandler(ScrollViewer platformView)
    {
        platformView.SizeChanged -= OnSizeChanged;
        platformView.Tapped -= OnTapped;
        platformView.DoubleTapped -= OnDoubleTapped;
        _tapTimer?.Stop();
        _tapTimer = null;
        base.DisconnectHandler(platformView);
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        var sv = PlatformView;
        _image.Width = sv.ViewportWidth;
        _image.Height = sv.ViewportHeight;
        sv.ChangeView(0, 0, 1.0f, disableAnimation: true);
    }

    private void OnTapped(object sender, TappedRoutedEventArgs e)
    {
        _pendingTapPos = e.GetPosition(PlatformView);
        _tapTimer?.Stop();
        _tapTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _tapTimer.Tick += OnTapTimerTick;
        _tapTimer.Start();
    }

    private void OnTapTimerTick(object? sender, object e)
    {
        _tapTimer?.Stop();
        _tapTimer = null;

        var width = PlatformView.ActualWidth;
        if (_pendingTapPos.X < width * 0.2)
            VirtualView?.PreviousPageCommand?.Execute(null);
        else if (_pendingTapPos.X > width * 0.8)
            VirtualView?.NextPageCommand?.Execute(null);
    }

    private void OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        _tapTimer?.Stop();
        _tapTimer = null;
        PlatformView.ChangeView(0, 0, 1.0f, disableAnimation: false);
    }

    private static void MapImageSource(ZoomablePageViewHandler handler, ZoomablePageView view)
    {
        var path = view.ImageSource;
        if (string.IsNullOrEmpty(path))
        {
            handler._image.Source = null;
        }
        else
        {
            handler._image.Source = new BitmapImage(new Uri(path));
            handler.PlatformView.ChangeView(0, 0, 1.0f, disableAnimation: true);
        }
    }
}
