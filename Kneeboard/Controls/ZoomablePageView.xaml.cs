using System.Windows.Input;

namespace Kneeboard.Controls;

public partial class ZoomablePageView : ContentView
{
    private const double MaxScale = 3.0;
    private const double SwipeThreshold = 80.0;
    private const double VerticalSwipeLimit = 50.0;
    private const uint AnimationDuration = 150;

    // Current transform state of _image
    private double _scale = 1.0;
    private double _translateX;
    private double _translateY;

    // Pinch snapshot
    private double _startScale;
    private double _startTranslateX;
    private double _startTranslateY;

    // Pan tracking
    private double _panStartScale;
    private double _startPanTranslateX;
    private double _startPanTranslateY;
    private double _accX;
    private double _accY;
    private double _prevTotalX;
    private double _prevTotalY;

    // ── Bindable properties ────────────────────────────────────────────────────

    public static readonly BindableProperty SourceProperty =
        BindableProperty.Create(nameof(Source), typeof(string), typeof(ZoomablePageView),
            defaultValue: string.Empty,
            propertyChanged: static (b, _, newVal) => ((ZoomablePageView)b).OnSourceChanged((string)newVal));

    public static readonly BindableProperty NavigateNextCommandProperty =
        BindableProperty.Create(nameof(NavigateNextCommand), typeof(ICommand), typeof(ZoomablePageView));

    public static readonly BindableProperty NavigatePreviousCommandProperty =
        BindableProperty.Create(nameof(NavigatePreviousCommand), typeof(ICommand), typeof(ZoomablePageView));

    public string Source
    {
        get => (string)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public ICommand? NavigateNextCommand
    {
        get => (ICommand?)GetValue(NavigateNextCommandProperty);
        set => SetValue(NavigateNextCommandProperty, value);
    }

    public ICommand? NavigatePreviousCommand
    {
        get => (ICommand?)GetValue(NavigatePreviousCommandProperty);
        set => SetValue(NavigatePreviousCommandProperty, value);
    }

    // ── Constructor ────────────────────────────────────────────────────────────

    public ZoomablePageView()
    {
        InitializeComponent();

        var pinch = new PinchGestureRecognizer();
        pinch.PinchUpdated += OnPinchUpdated;

        var pan = new PanGestureRecognizer();
        pan.PanUpdated += OnPanUpdated;

        var doubleTap = new TapGestureRecognizer { NumberOfTapsRequired = 2 };
        doubleTap.Tapped += OnDoubleTapped;

        _image.GestureRecognizers.Add(pinch);
        _image.GestureRecognizers.Add(pan);
        _image.GestureRecognizers.Add(doubleTap);
    }

    // ── Source change ──────────────────────────────────────────────────────────

    private void OnSourceChanged(string newSource)
    {
        _image.CancelAnimations();
        _image.Source = newSource;
        _scale = 1.0;
        _translateX = 0.0;
        _translateY = 0.0;
        _image.Scale = 1.0;
        _image.TranslationX = 0.0;
        _image.TranslationY = 0.0;
    }

    // ── Pinch ──────────────────────────────────────────────────────────────────

    private void OnPinchUpdated(object? sender, PinchGestureUpdatedEventArgs e)
    {
        switch (e.Status)
        {
            case GestureStatus.Started:
                _startScale = _scale;
                _startTranslateX = _translateX;
                _startTranslateY = _translateY;
                break;

            case GestureStatus.Running:
                var newScale = Math.Clamp(_startScale * e.Scale, 1.0, MaxScale);
                var cx = e.ScaleOrigin.X * Width;
                var cy = e.ScaleOrigin.Y * Height;
                var rawTx = _startTranslateX + (cx - Width  / 2.0) * (_startScale - newScale);
                var rawTy = _startTranslateY + (cy - Height / 2.0) * (_startScale - newScale);
                var maxTx = (newScale - 1.0) * Width / 2.0;
                var maxTy = (newScale - 1.0) * Height / 2.0;
                _scale = newScale;
                _translateX = Math.Clamp(rawTx, -maxTx, maxTx);
                _translateY = Math.Clamp(rawTy, -maxTy, maxTy);
                _image.Scale = _scale;
                _image.TranslationX = _translateX;
                _image.TranslationY = _translateY;
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                if (_scale <= 1.0)
                {
                    _scale = 1.0;
                    _translateX = 0.0;
                    _translateY = 0.0;
                    _image.Scale = 1.0;
                    _image.TranslationX = 0.0;
                    _image.TranslationY = 0.0;
                }
                break;
        }
    }

    // ── Pan ────────────────────────────────────────────────────────────────────

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _image.CancelAnimations();
                _panStartScale = _scale;
                _startPanTranslateX = _translateX;
                _startPanTranslateY = _translateY;
                _accX = _accY = _prevTotalX = _prevTotalY = 0;
                break;

            case GestureStatus.Running:
                _accX += e.TotalX - _prevTotalX;
                _accY += e.TotalY - _prevTotalY;
                _prevTotalX = e.TotalX;
                _prevTotalY = e.TotalY;

                if (_panStartScale <= 1.0)
                {
                    // Navigate mode: rubber-band feedback on X only
                    _image.TranslationX = _accX;
                }
                else
                {
                    // Pan mode: clamp within zoomed content bounds
                    var maxTx = (_panStartScale - 1.0) * Width / 2.0;
                    var maxTy = (_panStartScale - 1.0) * Height / 2.0;
                    _translateX = Math.Clamp(_startPanTranslateX + _accX, -maxTx, maxTx);
                    _translateY = Math.Clamp(_startPanTranslateY + _accY, -maxTy, maxTy);
                    _image.TranslationX = _translateX;
                    _image.TranslationY = _translateY;
                }
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                if (_panStartScale <= 1.0)
                {
                    var isHorizontal = Math.Abs(_accY) < VerticalSwipeLimit;
                    if (_accX < -SwipeThreshold && isHorizontal)
                    {
                        _image.TranslationX = 0;
                        NavigateNextCommand?.Execute(null);
                    }
                    else if (_accX > SwipeThreshold && isHorizontal)
                    {
                        _image.TranslationX = 0;
                        NavigatePreviousCommand?.Execute(null);
                    }
                    else
                    {
                        _translateX = 0;
                        _translateY = 0;
                        _image.TranslateToAsync(0, 0, AnimationDuration, Easing.CubicOut);
                    }
                }
                break;
        }
    }

    // ── Double-tap ─────────────────────────────────────────────────────────────

    private async void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        _image.CancelAnimations();
        _scale = 1.0;
        _translateX = 0.0;
        _translateY = 0.0;
        await Task.WhenAll(
            _image.ScaleToAsync(1.0, AnimationDuration, Easing.CubicInOut),
            _image.TranslateToAsync(0, 0, AnimationDuration, Easing.CubicInOut)
        );
    }
}
