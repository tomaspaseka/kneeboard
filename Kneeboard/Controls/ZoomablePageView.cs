using System.Windows.Input;

namespace Kneeboard.Controls;

public class ZoomablePageView : View
{
    public static readonly BindableProperty ImageSourceProperty =
        BindableProperty.Create(nameof(ImageSource), typeof(string), typeof(ZoomablePageView), default(string));

    public static readonly BindableProperty PreviousPageCommandProperty =
        BindableProperty.Create(nameof(PreviousPageCommand), typeof(ICommand), typeof(ZoomablePageView), null);

    public static readonly BindableProperty NextPageCommandProperty =
        BindableProperty.Create(nameof(NextPageCommand), typeof(ICommand), typeof(ZoomablePageView), null);

    public string? ImageSource
    {
        get => (string?)GetValue(ImageSourceProperty);
        set => SetValue(ImageSourceProperty, value);
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
