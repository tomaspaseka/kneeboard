using Kneeboard.Controls;
using Xunit;

namespace Kneeboard.Tests.Controls;

/// <summary>
/// Positions are in the page's own coordinates, so orientation and zoom don't feature: a portrait
/// page on a landscape screen and the same page at 3× both put a tap on the empty left margin at a
/// negative position, and the page's own edges are always a fifth of its width. The page here is 672
/// wide — what an A4 page fits to on a 1920×950 landscape content area — so the boundaries are at
/// 134.4 and 537.6.
/// </summary>
public class PageNavigationZoneTests
{
    private const double PageWidth = 672;

    [Theory]
    [InlineData(-614)]  // empty space beyond the page — on a 1920 screen, 10px from its left edge
    [InlineData(-1)]    // empty space, a hair short of the page
    [InlineData(0)]     // the page's own left edge
    [InlineData(134)]   // the page's outer fifth, just inside the boundary
    public void LeftOfCentre_IsPrevious(double tapX) =>
        Assert.Equal(PageNavigationZone.Previous, PageNavigationZones.Resolve(tapX, PageWidth));

    [Theory]
    [InlineData(135)]
    [InlineData(336)]   // the centre of the page
    [InlineData(537)]
    public void MiddleOfPage_IsNone(double tapX) =>
        Assert.Equal(PageNavigationZone.None, PageNavigationZones.Resolve(tapX, PageWidth));

    [Theory]
    [InlineData(538)]   // the page's outer fifth, just inside the boundary
    [InlineData(672)]   // the page's own right edge
    [InlineData(1286)]  // empty space beyond the page — on a 1920 screen, 10px from its right edge
    public void RightOfCentre_IsNext(double tapX) =>
        Assert.Equal(PageNavigationZone.Next, PageNavigationZones.Resolve(tapX, PageWidth));

    /// <summary>
    /// Before a document is open there is no page to measure against. A zero-width page puts both
    /// boundaries in the same place, which would otherwise leave every tap on an empty screen paging
    /// a section that isn't there.
    /// </summary>
    [Theory]
    [InlineData(-10)]
    [InlineData(0)]
    [InlineData(10)]
    public void NoPage_IsNone(double tapX) =>
        Assert.Equal(PageNavigationZone.None, PageNavigationZones.Resolve(tapX, pageWidth: 0));
}
