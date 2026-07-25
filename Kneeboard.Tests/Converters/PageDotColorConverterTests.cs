using System.Globalization;
using Kneeboard.Converters;
using Xunit;

namespace Kneeboard.Tests.Converters;

public class PageDotColorConverterTests
{
    private static readonly Color Active = Colors.Red;
    private static readonly Color Inactive = Colors.Gray;

    private static readonly PageDotColorConverter Converter =
        new() { ActiveColor = Active, InactiveColor = Inactive };

    [Fact]
    public void CurrentPage_IsActiveColor() => Assert.Equal(Active, Convert(true));

    [Fact]
    public void OtherPage_IsInactiveColor() => Assert.Equal(Inactive, Convert(false));

    /// <summary>
    /// BindableLayout applies the dot template's bindings before it assigns the item, so the first
    /// conversion of every dot has no value. Throwing there took the whole notification down with
    /// it and left the loading overlay up.
    /// </summary>
    [Fact]
    public void NoValueYet_IsInactiveColor_DoesNotThrow() => Assert.Equal(Inactive, Convert(null));

    private static object Convert(object? value) =>
        Converter.Convert(value, typeof(Color), null, CultureInfo.InvariantCulture);
}
