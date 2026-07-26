using System.Windows.Input;

namespace Kneeboard.Controls;

public class ZoomablePageView : View
{
    public static readonly BindableProperty PageImageProperty =
        BindableProperty.Create(nameof(PageImage), typeof(ReadOnlyMemory<byte>), typeof(ZoomablePageView), default(ReadOnlyMemory<byte>));

    public static readonly BindableProperty PreviousPageCommandProperty =
        BindableProperty.Create(nameof(PreviousPageCommand), typeof(ICommand), typeof(ZoomablePageView), null);

    public static readonly BindableProperty NextPageCommandProperty =
        BindableProperty.Create(nameof(NextPageCommand), typeof(ICommand), typeof(ZoomablePageView), null);

    // Two-way, and the only property here that is: zoom and pan happen to the platform view under the
    // pilot's fingers, so this is how the screen both asks for a framing and hears about theirs.
    // Written as ViewModels.Framing throughout — the property and its type share a name.
    public static readonly BindableProperty FramingProperty =
        BindableProperty.Create(
            nameof(Framing),
            typeof(ViewModels.Framing),
            typeof(ZoomablePageView),
            defaultValue: ViewModels.Framing.Fit,
            defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Encoded image bytes for the page on screen. Empty renders nothing.</summary>
    public ReadOnlyMemory<byte> PageImage
    {
        get => (ReadOnlyMemory<byte>)GetValue(PageImageProperty);
        set => SetValue(PageImageProperty, value);
    }

    /// <summary>How far into the page to zoom, and which point of it to centre on.</summary>
    public ViewModels.Framing Framing
    {
        get => (ViewModels.Framing)GetValue(FramingProperty);
        set => SetValue(FramingProperty, value);
    }

    public ICommand? PreviousPageCommand
    {
        get => (ICommand?)GetValue(PreviousPageCommandProperty);
        set => SetValue(PreviousPageCommandProperty, value);
    }

    public ICommand? NextPageCommand
    {
        get => (ICommand?)GetValue(NextPageCommandProperty);
        set => SetValue(NextPageCommandProperty, value);
    }
}
