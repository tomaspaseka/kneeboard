namespace Kneeboard.Controls;

/// <summary>What a tap at a given position does to the section's paging.</summary>
public enum PageNavigationZone
{
    None,
    Previous,
    Next,
}

public static class PageNavigationZones
{
    /// <summary>How much of the page's width each navigation zone claims at its side.</summary>
    public const double PageEdgeFraction = 0.2;

    /// <summary>
    /// Which zone a tap falls in, given its distance from the page's left edge and the page's width,
    /// both in the page's own coordinates. A tap on the empty space around the page lands outside
    /// 0..<paramref name="pageWidth"/> and so falls in a navigation zone by the same comparison that
    /// covers the page's own edges — the empty space is not a second rule.
    /// </summary>
    public static PageNavigationZone Resolve(double tapX, double pageWidth)
    {
        // No page to measure against means no navigation: a zero width would put both boundaries in
        // the same place and leave every tap paging a section that isn't there.
        if (pageWidth <= 0)
            return PageNavigationZone.None;

        if (tapX < pageWidth * PageEdgeFraction)
            return PageNavigationZone.Previous;

        if (tapX > pageWidth * (1 - PageEdgeFraction))
            return PageNavigationZone.Next;

        return PageNavigationZone.None;
    }
}
