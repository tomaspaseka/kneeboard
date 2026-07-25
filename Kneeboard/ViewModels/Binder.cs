using System.Collections.Immutable;

namespace Kneeboard.ViewModels;

/// <summary>
/// The material of an open document as the pilot has it in front of them: every section's pages, in
/// order, together with the page they are on in each section.
/// </summary>
public sealed record Binder
{
    /// <summary>A binder with nothing in it: no sections, and so no section selected.</summary>
    public static readonly Binder Empty = new([], [], -1);

    // Aligned by section index, and only ever built together in Of: _currentPages[i] is the page the
    // pilot is on in _sections[i]. Nothing here can change either length — SetItem cannot — so the
    // two stay the same length for the life of the binder.
    private readonly ImmutableArray<ImmutableArray<ReadOnlyMemory<byte>>> _sections;
    private readonly ImmutableArray<int> _currentPages;

    private Binder(
        ImmutableArray<ImmutableArray<ReadOnlyMemory<byte>>> sections,
        ImmutableArray<int> currentPages,
        int selectedSectionIndex)
    {
        _sections = sections;
        _currentPages = currentPages;
        SelectedSectionIndex = selectedSectionIndex;
    }

    /// <summary>A binder over the given sections' pages, open at the first page of the first section.</summary>
    public static Binder Of(IEnumerable<IReadOnlyList<ReadOnlyMemory<byte>>> pagesPerSection)
    {
        var sections = pagesPerSection.Select(ImmutableArray.CreateRange).ToImmutableArray();
        return sections.IsEmpty ? Empty : new Binder(sections, [.. sections.Select(_ => 0)], 0);
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
            : new Binder(_sections, _currentPages, sectionIndex);

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
        return clamped == CurrentPageIndex
            ? this
            : new Binder(_sections, _currentPages.SetItem(SelectedSectionIndex, clamped), SelectedSectionIndex);
    }

    private ImmutableArray<ReadOnlyMemory<byte>> SelectedPages =>
        SelectedSectionIndex < 0 ? [] : _sections[SelectedSectionIndex];

    private int CurrentPageIndex =>
        SelectedSectionIndex < 0 ? 0 : _currentPages[SelectedSectionIndex];
}
