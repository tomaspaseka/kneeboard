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
            [nameof(ZoomablePageView.PageImage)] = MapPageImage,
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
        _image.Width = e.NewSize.Width;
        _image.Height = e.NewSize.Height;
        PlatformView.ChangeView(0, 0, 1.0f, disableAnimation: true);
    }

    private void OnTapped(object sender, TappedRoutedEventArgs e)
    {
        _pendingTapPos = e.GetPosition(PlatformView);
        if (_tapTimer is not null)
        {
            _tapTimer.Stop();
            _tapTimer.Tick -= OnTapTimerTick;
            _tapTimer = null;
        }
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
        PlatformView.ChangeView(0, 0, 1.0f, disableAnimation: true);
    }

    private static void MapPageImage(ZoomablePageViewHandler handler, ZoomablePageView view)
    {
        var bytes = view.PageImage;
        if (bytes.IsEmpty)
        {
            handler._image.Source = null;
            return;
        }

        // Deliberately synchronous: SetSource (not SetSourceAsync) keeps this mapper ordering-safe,
        // so a fast page turn can't land an earlier decode over a later page. The stream is not
        // disposed here — the decoder reads from it after this returns, and the adapter keeps it
        // alive for as long as the BitmapImage needs it.
        var bitmap = new BitmapImage();
        bitmap.SetSource(new MemoryStream(bytes.ToArray()).AsRandomAccessStream());

        handler._image.Source = bitmap;
        handler.PlatformView.ChangeView(0, 0, 1.0f, disableAnimation: true);
    }
}
