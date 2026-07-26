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
        // Centred, not stretched: a uniformly-stretched Image arranges to its fitted size rather
        // than its slot, and the default Stretch alignment drops that fitted content at 0,0 — which
        // pins a portrait page to the left of a landscape screen.
        _image = new Microsoft.UI.Xaml.Controls.Image
        {
            Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform,
            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
            VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center,
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
            // Paired with the image's own alignment above: this centres the page within the
            // viewport, that centres it within whatever slot the presenter hands it. Which of the
            // two has the spare room depends on the presenter, and only one of them needs it.
            HorizontalContentAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
            VerticalContentAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center,
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
        // Maximums, not exact sizes: the ScrollViewer measures its content unbounded, so without a
        // cap the page would lay out at its full bitmap size instead of fitting the screen. Capping
        // rather than fixing leaves the page free to arrange smaller than the screen and centre.
        _image.MaxWidth = e.NewSize.Width;
        _image.MaxHeight = e.NewSize.Height;
        PlatformView.ChangeView(0, 0, 1.0f, disableAnimation: true);
    }

    private void OnTapped(object sender, TappedRoutedEventArgs e)
    {
        // Relative to the page, not the screen: WinUI then does the zoom and scroll arithmetic for
        // us, over the same transform that makes a control inside a zoomed ScrollViewer clickable in
        // the right place. Taps on the empty space around the page arrive as negative or overlarge.
        _pendingTapPos = e.GetPosition(_image);
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

        // Both the tap and the width are in the page's own coordinates, so the zoom factor cancels
        // out and the zones stay a fifth of the page at any magnification.
        switch (PageNavigationZones.Resolve(_pendingTapPos.X, _image.ActualWidth))
        {
            case PageNavigationZone.Previous:
                VirtualView?.PreviousPageCommand?.Execute(null);
                break;
            case PageNavigationZone.Next:
                VirtualView?.NextPageCommand?.Execute(null);
                break;
        }
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
