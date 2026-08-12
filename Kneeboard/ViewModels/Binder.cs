using System.Collections.Immutable;

namespace Kneeboard.ViewModels;

/// <summary>
/// The material of an open document as the pilot has it in front of them: every tab's pages, in
/// order, together with the page they are on and how they have it framed in each tab.
/// </summary>
public sealed record Binder
{
    /// <summary>A binder with nothing in it: no tabs, and so no tab selected.</summary>
    public static readonly Binder Empty = new([], [], [], -1);

    // Aligned by tab index, and only ever built together in Of: _tabs[i] is the pages of the i-th
    // tab, _currentPages[i] is the page the pilot is on in it and _framings[i] is how they have it
    // framed. Nothing here can change any length — SetItem cannot — so the three stay the same
    // length for the binder's life.
    private readonly ImmutableArray<ImmutableArray<ReadOnlyMemory<byte>>> _tabs;
    private readonly ImmutableArray<int> _currentPages;
    private readonly ImmutableArray<Framing> _framings;

    private Binder(
        ImmutableArray<ImmutableArray<ReadOnlyMemory<byte>>> tabs,
        ImmutableArray<int> currentPages,
        ImmutableArray<Framing> framings,
        int selectedTabIndex)
    {
        _tabs = tabs;
        _currentPages = currentPages;
        _framings = framings;
        SelectedTabIndex = selectedTabIndex;
    }

    /// <summary>
    /// A binder over the given tabs' pages, open at the first page of the first tab with every tab
    /// fitted to the screen. Callers pass one page-list per tab, in the order the tab bar shows them —
    /// the binder itself has no notion of a tab's kind, only its position.
    /// </summary>
    public static Binder Of(IEnumerable<IReadOnlyList<ReadOnlyMemory<byte>>> pagesPerTab)
    {
        var tabs = pagesPerTab.Select(ImmutableArray.CreateRange).ToImmutableArray();
        return tabs.IsEmpty
            ? Empty
            : new Binder(tabs, [.. tabs.Select(_ => 0)], [.. tabs.Select(_ => Framing.Fit)], 0);
    }

    public int SelectedTabIndex { get; }

    /// <summary>The page on screen; empty bytes when there is nothing to show.</summary>
    public ReadOnlyMemory<byte> CurrentPage
    {
        get
        {
            var pages = SelectedPages;
            return pages.IsEmpty ? ReadOnlyMemory<byte>.Empty : pages[CurrentPageIndex];
        }
    }

    /// <summary>How the pilot has the selected tab framed; the fit when there is nothing open.</summary>
    public Framing CurrentFraming =>
        SelectedTabIndex < 0 ? Framing.Fit : _framings[SelectedTabIndex];

    /// <summary>One entry per page of the selected tab, true for the page on screen.</summary>
    public IReadOnlyList<bool> PageDots
    {
        get
        {
            var current = CurrentPageIndex;
            return [.. SelectedPages.Select((_, index) => index == current)];
        }
    }

    /// <summary>
    /// Selects the tab at <paramref name="tabIndex"/>, on the page it was left on. An index outside
    /// the tabs — including the -1 a not-found tab lookup returns — is a no-op.
    /// </summary>
    public Binder Select(int tabIndex) =>
        tabIndex < 0 || tabIndex >= _tabs.Length || tabIndex == SelectedTabIndex
            ? this
            : new Binder(_tabs, _currentPages, _framings, tabIndex);

    /// <summary>
    /// Records how the pilot has framed the selected tab. Nothing open, and the framing the tab
    /// already has, are both no-ops — the second because the platform view reports the framing it
    /// was handed back as every gesture settles, and that echo is not a change.
    /// </summary>
    public Binder Framed(Framing framing) =>
        SelectedTabIndex < 0 || framing == CurrentFraming
            ? this
            : new Binder(_tabs, _currentPages, _framings.SetItem(SelectedTabIndex, framing), SelectedTabIndex);

    /// <summary>Turns to the next page of the selected tab, or stays on the last one.</summary>
    public Binder Next() => TurnTo(CurrentPageIndex + 1);

    /// <summary>Turns back a page in the selected tab, or stays on the first one.</summary>
    public Binder Previous() => TurnTo(CurrentPageIndex - 1);

    /// <summary>
    /// Turns to <paramref name="pageIndex"/>, clamped to the selected tab's pages. A tab with no
    /// pages clamps to zero, so no sequence of turns can produce a negative page index.
    /// </summary>
    private Binder TurnTo(int pageIndex)
    {
        // Nothing open to turn. Stated rather than left to the clamp below, which happens to reach
        // the same answer — SetItem against a selected index of -1 would throw if it ever didn't.
        if (SelectedTabIndex < 0)
            return this;

        var lastPage = Math.Max(SelectedPages.Length - 1, 0);
        var clamped = Math.Clamp(pageIndex, 0, lastPage);

        // Short-circuited explicitly, because record equality would not do it: ImmutableArray
        // equality is reference equality of its backing array and SetItem allocates
        // unconditionally, so a rebuilt no-op would compare unequal and cost a needless
        // notification on every turn at either end of a tab.
        // A turn returns the tab to the fit: the pilot pages to read a new page whole, not to
        // arrive at whatever corner of it the last page's framing happened to point at. Only on a real
        // turn — the short-circuit above means a tap at either end of a tab keeps the framing,
        // because nothing was turned.
        return clamped == CurrentPageIndex
            ? this
            : new Binder(
                _tabs,
                _currentPages.SetItem(SelectedTabIndex, clamped),
                _framings.SetItem(SelectedTabIndex, Framing.Fit),
                SelectedTabIndex);
    }

    private ImmutableArray<ReadOnlyMemory<byte>> SelectedPages =>
        SelectedTabIndex < 0 ? [] : _tabs[SelectedTabIndex];

    private int CurrentPageIndex =>
        SelectedTabIndex < 0 ? 0 : _currentPages[SelectedTabIndex];
}
