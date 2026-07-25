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

    /// <summary>Encoded image bytes for the page on screen. Empty renders nothing.</summary>
    public ReadOnlyMemory<byte> PageImage
    {
        get => (ReadOnlyMemory<byte>)GetValue(PageImageProperty);
        set => SetValue(PageImageProperty, value);
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
