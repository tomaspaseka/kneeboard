using System.Globalization;

namespace Kneeboard.Converters;

/// <summary>
/// Colours one page-indicator dot: active for the page on screen, inactive for the rest.
/// </summary>
/// <remarks>
/// Tolerating a null value is the point of this type. BindableLayout applies a dot template's
/// bindings while hydrating it, before it hands the item over, so the first conversion of every
/// dot sees no value at all. CommunityToolkit's BoolToObjectConverter throws on that null, and the
/// throw travels back up the notification that raised it.
/// </remarks>
public class PageDotColorConverter : IValueConverter
{
    public Color ActiveColor { get; set; } = Colors.Transparent;

    public Color InactiveColor { get; set; } = Colors.Transparent;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? ActiveColor : InactiveColor;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("Page dots are display-only.");
}
