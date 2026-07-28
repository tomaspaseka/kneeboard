using Kneeboard.Controls;
using Kneeboard.ViewModels;
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
            // Page before framing, matching the order the screen announces them in: a framing's centre
            // is measured against the page it frames.
            [nameof(ZoomablePageView.PageImage)] = MapPageImage,
            [nameof(ZoomablePageView.Framing)] = MapFraming,
        };

    private Microsoft.UI.Xaml.Controls.Image _image = null!;
    private DispatcherTimer? _tapTimer;
    private global::Windows.Foundation.Point _pendingTapPos;

    // Set while a view change this handler asked for is still on its way back through OnViewChanged.
    private bool _applying;

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
            MinZoomFactor = (float)Framing.FitFactor,
            MaxZoomFactor = (float)Framing.MaxFactor,
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
        platformView.ViewChanged += OnViewChanged;
        _image.SizeChanged += OnImageSizeChanged;
    }

    protected override void DisconnectHandler(ScrollViewer platformView)
    {
        platformView.SizeChanged -= OnSizeChanged;
        platformView.Tapped -= OnTapped;
        platformView.DoubleTapped -= OnDoubleTapped;
        platformView.ViewChanged -= OnViewChanged;
        _image.SizeChanged -= OnImageSizeChanged;
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

        // Re-aimed rather than reset: the framing is page-relative, so a resized window still means the
        // same patch of the page and only the offsets that reach it have changed. Here as well as in
        // OnImageSizeChanged because a viewport can change without the page's fitted size changing at
        // all — a window widened around a page already capped by its height — and then no image size
        // change follows to do it. When one does follow, it re-aims against the new measurements.
        ApplyFraming();
    }

    /// <summary>
    /// The page has been laid out at a new fitted size — a fresh page arriving, or the window resized
    /// around it. Until this fires, the measurements a framing is converted against describe the page
    /// the pilot has just left, so this is where a section switch actually lands its framing.
    /// </summary>
    private void OnImageSizeChanged(object sender, SizeChangedEventArgs e) => ApplyFraming();

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

        // Announced rather than just done: fitting the page back to the screen is a framing like any
        // other, and it is what the pilot will find when they come back to this section.
        if (VirtualView is not null)
            VirtualView.Framing = Framing.Fit;

        // The assignment above runs the mapper, which applies it — so this only does anything when the
        // section was already recorded as fitted while the view had drifted off it. Double-tapping a
        // page that looks zoomed has to fit it, whatever the screen already believed.
        ApplyFraming();
    }

    /// <summary>
    /// Reports the pilot's own zoom and pan back to the screen. Only settled views count — WinUI
    /// reports a running commentary of intermediate ones through the same event.
    /// </summary>
    private void OnViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        if (e.IsIntermediate || VirtualView is null || !CanMeasurePage)
            return;

        // This handler's own instruction arriving back. Swallowed rather than compared: a framing applied
        // before the page it frames has finished laying out reads back against measurements that are
        // half old page and half new, and reporting that would overwrite the section's framing with an
        // artefact of the page it just replaced. OnImageSizeChanged applies it again properly.
        if (_applying)
        {
            _applying = false;
            return;
        }

        var framing = FramingOnScreen(VirtualView.Framing);
        if (framing.Matches(VirtualView.Framing))
            return;

        VirtualView.Framing = framing;
    }

    private static void MapFraming(ZoomablePageViewHandler handler, ZoomablePageView view) =>
        handler.ApplyFraming();

    /// <summary>
    /// Points the ScrollViewer at the framing the screen is asking for, converting the page-relative
    /// centre into scroll offsets. Silent when the page has not been laid out yet — there is nothing
    /// to measure against, and <see cref="OnImageSizeChanged"/> brings us back when there is.
    /// </summary>
    private void ApplyFraming()
    {
        if (VirtualView is null || !CanMeasurePage)
            return;

        var framing = VirtualView.Framing;
        if (FramingOnScreen(framing).Matches(framing))
            return;

        var (extentWidth, extentHeight) = ExtentAt(framing.Factor);

        // Offsets that put the framing's centre in the middle of the viewport. Overshoot at the edges
        // of the page is left to ChangeView, which clamps into range.
        //
        // The flag rides on the return value: true means the view will move and report the move back,
        // and that report is this instruction rather than the pilot's hands. False means nothing was
        // scheduled, so there will be no report to swallow and swallowing one would cost a real gesture.
        _applying = PlatformView.ChangeView(
            framing.CentreX * extentWidth - PlatformView.ViewportWidth / 2,
            framing.CentreY * extentHeight - PlatformView.ViewportHeight / 2,
            (float)framing.Factor,
            disableAnimation: true);
    }

    /// <summary>
    /// How the page is framed at this moment, in the page's own terms. An axis with nothing left to
    /// scroll keeps the centre from <paramref name="given"/> rather than reporting the middle: the page
    /// is centred by the ScrollViewer's own alignment there, so there is no choice of the pilot's to read
    /// back, and answering "the middle" would throw away the one they made when the window was narrower.
    /// </summary>
    private Framing FramingOnScreen(Framing given)
    {
        var zoom = PlatformView.ZoomFactor;
        var (extentWidth, extentHeight) = ExtentAt(zoom);

        return new Framing(
            zoom,
            CentreOf(PlatformView.HorizontalOffset, PlatformView.ViewportWidth, extentWidth, given.CentreX),
            CentreOf(PlatformView.VerticalOffset, PlatformView.ViewportHeight, extentHeight, given.CentreY));
    }

    /// <summary>
    /// How much room the page takes at <paramref name="factor"/>, worked out from its fitted size rather
    /// than read off the ScrollViewer — whose own extent still describes the zoom being left behind
    /// whenever a framing is being applied.
    /// </summary>
    private (double Width, double Height) ExtentAt(double factor) =>
        (_image.ActualWidth * factor, _image.ActualHeight * factor);

    /// <summary>Which point of the page, along one axis, the middle of the viewport is over.</summary>
    private static double CentreOf(double offset, double viewport, double extent, double whenUnscrollable)
    {
        // Measuring a viewport against an extent smaller than itself would report a centre off the edge
        // of the page — and there is nothing to measure anyway when none of the page is out of sight.
        if (extent <= viewport)
            return whenUnscrollable;

        return (offset + viewport / 2) / extent;
    }

    /// <summary>
    /// Whether the page has been laid out. Nothing can be converted between a framing and scroll
    /// offsets before it has, and a zero width would divide by it.
    /// </summary>
    private bool CanMeasurePage => _image.ActualWidth > 0 && _image.ActualHeight > 0;

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

        // No framing applied here on purpose. The new page has not been laid out yet, so the only
        // measurements available describe the page being replaced; OnImageSizeChanged applies the
        // framing once the page it belongs to is actually on screen.
    }
}
