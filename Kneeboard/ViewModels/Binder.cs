using System.Collections.Immutable;

namespace Kneeboard.ViewModels;

/// <summary>
/// The material of an open document as the pilot has it in front of them: every section's pages, in
/// order, together with the page they are on and how they have it framed in each section.
/// </summary>
public sealed record Binder
{
    /// <summary>A binder with nothing in it: no sections, and so no section selected.</summary>
    public static readonly Binder Empty = new([], [], [], -1);

    // Aligned by section index, and only ever built together in Of: _currentPages[i] is the page the
    // pilot is on in _sections[i] and _framings[i] is how they have it framed. Nothing here can
    // change any length — SetItem cannot — so the three stay the same length for the binder's life.
    private readonly ImmutableArray<ImmutableArray<ReadOnlyMemory<byte>>> _sections;
    private readonly ImmutableArray<int> _currentPages;
    private readonly ImmutableArray<Framing> _framings;

    private Binder(
        ImmutableArray<ImmutableArray<ReadOnlyMemory<byte>>> sections,
        ImmutableArray<int> currentPages,
        ImmutableArray<Framing> framings,
        int selectedSectionIndex)
    {
        _sections = sections;
        _currentPages = currentPages;
        _framings = framings;
        SelectedSectionIndex = selectedSectionIndex;
    }

    /// <summary>
    /// A binder over the given sections' pages, open at the first page of the first section with
    /// every section fitted to the screen.
    /// </summary>
    public static Binder Of(IEnumerable<IReadOnlyList<ReadOnlyMemory<byte>>> pagesPerSection)
    {
        var sections = pagesPerSection.Select(ImmutableArray.CreateRange).ToImmutableArray();
        return sections.IsEmpty
            ? Empty
            : new Binder(sections, [.. sections.Select(_ => 0)], [.. sections.Select(_ => Framing.Fit)], 0);
    }

    public int SelectedSectionIndex { get; }

    /// <summary>The page on screen; empty bytes when there is nothing to show.</summary>
    public ReadOnlyMemory<byte> CurrentPage
    {
        get
        {
            var pages = SelectedPages;
            return pages.IsEmpty ? ReadOnlyMemory<byte>.Empty : pages[CurrentPageIndex];
        }
    }

    /// <summary>How the pilot has the selected section framed; the fit when there is nothing open.</summary>
    public Framing CurrentFraming =>
        SelectedSectionIndex < 0 ? Framing.Fit : _framings[SelectedSectionIndex];

    /// <summary>One entry per page of the selected section, true for the page on screen.</summary>
    public IReadOnlyList<bool> PageDots
    {
        get
        {
            var current = CurrentPageIndex;
            return [.. SelectedPages.Select((_, index) => index == current)];
        }
    }

    /// <summary>
    /// Selects the section at <paramref name="sectionIndex"/>, on the page it was left on. An index
    /// outside the sections — including the -1 a not-found tab lookup returns — is a no-op.
    /// </summary>
    public Binder Select(int sectionIndex) =>
        sectionIndex < 0 || sectionIndex >= _sections.Length || sectionIndex == SelectedSectionIndex
            ? this
            : new Binder(_sections, _currentPages, _framings, sectionIndex);

    /// <summary>
    /// Records how the pilot has framed the selected section. Nothing open, and the framing the
    /// section already has, are both no-ops — the second because the platform view reports the framing
    /// it was handed back as every gesture settles, and that echo is not a change.
    /// </summary>
    public Binder Framed(Framing framing) =>
        SelectedSectionIndex < 0 || framing == CurrentFraming
            ? this
            : new Binder(_sections, _currentPages, _framings.SetItem(SelectedSectionIndex, framing), SelectedSectionIndex);

    /// <summary>Turns to the next page of the selected section, or stays on the last one.</summary>
    public Binder Next() => TurnTo(CurrentPageIndex + 1);

    /// <summary>Turns back a page in the selected section, or stays on the first one.</summary>
    public Binder Previous() => TurnTo(CurrentPageIndex - 1);

    /// <summary>
    /// Turns to <paramref name="pageIndex"/>, clamped to the selected section's pages. A section
    /// with no pages clamps to zero, so no sequence of turns can produce a negative page index.
    /// </summary>
    private Binder TurnTo(int pageIndex)
    {
        // Nothing open to turn. Stated rather than left to the clamp below, which happens to reach
        // the same answer — SetItem against a selected index of -1 would throw if it ever didn't.
        if (SelectedSectionIndex < 0)
            return this;

        var lastPage = Math.Max(SelectedPages.Length - 1, 0);
        var clamped = Math.Clamp(pageIndex, 0, lastPage);

        // Short-circuited explicitly, because record equality would not do it: ImmutableArray
        // equality is reference equality of its backing array and SetItem allocates
        // unconditionally, so a rebuilt no-op would compare unequal and cost a needless
        // notification on every turn at either end of a section.
        // A turn returns the section to the fit: the pilot pages to read a new page whole, not to
        // arrive at whatever corner of it the last page's framing happened to point at. Only on a real
        // turn — the short-circuit above means a tap at either end of a section keeps the framing,
        // because nothing was turned.
        return clamped == CurrentPageIndex
            ? this
            : new Binder(
                _sections,
                _currentPages.SetItem(SelectedSectionIndex, clamped),
                _framings.SetItem(SelectedSectionIndex, Framing.Fit),
                SelectedSectionIndex);
    }

    private ImmutableArray<ReadOnlyMemory<byte>> SelectedPages =>
        SelectedSectionIndex < 0 ? [] : _sections[SelectedSectionIndex];

    private int CurrentPageIndex =>
        SelectedSectionIndex < 0 ? 0 : _currentPages[SelectedSectionIndex];
}
