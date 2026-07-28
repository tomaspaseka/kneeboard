using System.Text;
using Kneeboard.ViewModels;
using Xunit;

namespace Kneeboard.Tests.ViewModels;

public class BinderTests
{
    [Fact]
    public void Of_OpensAtTheFirstPageOfTheFirstSection()
    {
        var binder = BinderOf(["a1", "a2"], ["b1"]);

        Assert.Equal(0, binder.SelectedSectionIndex);
        Assert.Equal("a1", CurrentPage(binder));
    }

    [Fact]
    public void Of_NoSections_IsTheEmptyBinder() => Assert.Same(Binder.Empty, BinderOf());

    [Fact]
    public void Empty_HasNoSelectedSection_NoDots_AndNoPage()
    {
        Assert.Equal(-1, Binder.Empty.SelectedSectionIndex);
        Assert.Empty(Binder.Empty.PageDots);
        Assert.Empty(CurrentPage(Binder.Empty));
    }

    [Fact]
    public void Next_TurnsToTheNextPage()
    {
        var binder = BinderOf(["p1", "p2", "p3"]);

        Assert.Equal("p2", CurrentPage(binder.Next()));
    }

    [Fact]
    public void Next_OnTheLastPage_StaysOnIt()
    {
        var binder = BinderOf(["p1", "p2"]).Next();

        Assert.Equal("p2", CurrentPage(binder.Next()));
    }

    [Fact]
    public void Previous_TurnsBackAPage()
    {
        var binder = BinderOf(["p1", "p2"]).Next();

        Assert.Equal("p1", CurrentPage(binder.Previous()));
    }

    [Fact]
    public void Previous_OnTheFirstPage_StaysOnIt()
    {
        var binder = BinderOf(["p1", "p2"]);

        Assert.Equal("p1", CurrentPage(binder.Previous()));
    }

    /// <summary>
    /// Reference identity, not equality: the screen replaces the binder it holds and announces the
    /// change, so a clamped turn has to be indistinguishable from no turn at all. Record equality
    /// would not do it — <c>ImmutableArray</c> equality is reference equality of its backing array
    /// and <c>SetItem</c> allocates unconditionally, so a rebuilt no-op would compare unequal and
    /// publish a needless notification.
    /// </summary>
    [Fact]
    public void Next_OnTheLastPage_ReturnsTheSameInstance()
    {
        var binder = BinderOf(["p1"]);

        Assert.Same(binder, binder.Next());
    }

    [Fact]
    public void Previous_OnTheFirstPage_ReturnsTheSameInstance()
    {
        var binder = BinderOf(["p1", "p2"]);

        Assert.Same(binder, binder.Previous());
    }

    [Fact]
    public void Select_ShowsThatSectionsPage()
    {
        var binder = BinderOf(["a1"], ["b1"]);

        var selected = binder.Select(1);

        Assert.Equal(1, selected.SelectedSectionIndex);
        Assert.Equal("b1", CurrentPage(selected));
    }

    [Fact]
    public void Select_ASectionNeverOpened_ShowsItsFirstPage()
    {
        var binder = BinderOf(["a1", "a2", "a3"], ["b1", "b2"]).Next().Next(); // third page of A

        Assert.Equal("b1", CurrentPage(binder.Select(1)));
    }

    [Fact]
    public void Select_ASectionAlreadyRead_ShowsThePageItWasLeftOn()
    {
        var binder = BinderOf(["a1", "a2"], ["b1", "b2", "b3"])
            .Select(1).Next().Next() // third page of B
            .Select(0)               // away to A
            .Select(1);              // and back

        Assert.Equal("b3", CurrentPage(binder));
    }

    [Fact]
    public void Next_LeavesEveryOtherSectionOnItsOwnPage()
    {
        var binder = BinderOf(["a1", "a2"], ["b1", "b2"])
            .Select(1).Next()  // second page of B
            .Select(0).Next(); // second page of A

        Assert.Equal("b2", CurrentPage(binder.Select(1)));
    }

    /// <summary>-1 is what a not-found lookup for a tab returns, so it has to be harmless.</summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    [InlineData(int.MaxValue)]
    public void Select_OutsideTheSections_IsANoOp(int sectionIndex)
    {
        var binder = BinderOf(["a1"], ["b1"]).Select(1);

        Assert.Same(binder, binder.Select(sectionIndex));
    }

    [Fact]
    public void Select_TheSectionAlreadyOpen_ReturnsTheSameInstance()
    {
        var binder = BinderOf(["a1"], ["b1"]);

        Assert.Same(binder, binder.Select(0));
    }

    [Fact]
    public void PageDots_LightTheDotForThePageOnScreen()
    {
        var binder = BinderOf(["p1", "p2", "p3"]).Next();

        Assert.Equal([false, true, false], binder.PageDots);
    }

    [Fact]
    public void PageDots_CountThePagesOfTheSelectedSection()
    {
        var binder = BinderOf(["a1"], ["b1", "b2", "b3"]).Select(1);

        Assert.Equal([true, false, false], binder.PageDots);
    }

    /// <summary>
    /// A packing mistake leaves a tab with no material. It has to sit on its first page showing
    /// nothing, and no run of turns may walk its page index off the front — which is what the old
    /// screen did, recording -1 against the section.
    /// </summary>
    [Fact]
    public void ASectionWithNoPages_ShowsNothingAndCannotBePagedOffTheStart()
    {
        var empty = BinderOf(["a1"], []).Select(1);

        var paged = empty.Previous().Next().Previous().Previous();

        Assert.Same(empty, paged);
        Assert.Empty(paged.PageDots);
        Assert.Empty(CurrentPage(paged));
    }

    [Fact]
    public void Empty_HasNothingToSelectOrTurn()
    {
        Assert.Same(Binder.Empty, Binder.Empty.Select(0));
        Assert.Same(Binder.Empty, Binder.Empty.Next());
        Assert.Same(Binder.Empty, Binder.Empty.Previous());
    }

    // ── framing ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Of_OpensEverySectionFittedToTheScreen()
    {
        var binder = BinderOf(["a1"], ["b1"]);

        Assert.Equal(Framing.Fit, binder.CurrentFraming);
        Assert.Equal(Framing.Fit, binder.Select(1).CurrentFraming);
    }

    [Fact]
    public void Framed_RecordsHowThePilotFramedTheSection()
    {
        var binder = BinderOf(["a1"]);

        Assert.Equal(Zoomed, binder.Framed(Zoomed).CurrentFraming);
    }

    [Fact]
    public void Select_ASectionAlreadyRead_RestoresHowItWasFramed()
    {
        var binder = BinderOf(["a1"], ["b1"])
            .Select(1).Framed(Zoomed) // B zoomed in on a corner
            .Select(0)                // away to A
            .Select(1);               // and back

        Assert.Equal(Zoomed, binder.CurrentFraming);
    }

    [Fact]
    public void Framed_LeavesEveryOtherSectionFitted()
    {
        var binder = BinderOf(["a1"], ["b1"]).Select(1).Framed(Zoomed);

        Assert.Equal(Framing.Fit, binder.Select(0).CurrentFraming);
    }

    [Fact]
    public void Next_ReturnsTheSectionToTheFit()
    {
        var binder = BinderOf(["p1", "p2"]).Framed(Zoomed);

        Assert.Equal(Framing.Fit, binder.Next().CurrentFraming);
    }

    [Fact]
    public void Previous_ReturnsTheSectionToTheFit()
    {
        var binder = BinderOf(["p1", "p2"]).Next().Framed(Zoomed);

        Assert.Equal(Framing.Fit, binder.Previous().CurrentFraming);
    }

    /// <summary>
    /// A tap on the page edge at the end of a section turns nothing, so it must not throw the pilot's
    /// framing away either — the same no-op the paging tests pin by reference identity.
    /// </summary>
    [Fact]
    public void Next_OnTheLastPage_KeepsTheFraming()
    {
        var binder = BinderOf(["p1"]).Framed(Zoomed);

        Assert.Equal(Zoomed, binder.Next().CurrentFraming);
    }

    [Fact]
    public void Previous_OnTheFirstPage_KeepsTheFraming()
    {
        var binder = BinderOf(["p1", "p2"]).Framed(Zoomed);

        Assert.Equal(Zoomed, binder.Previous().CurrentFraming);
    }

    [Fact]
    public void Next_LeavesEveryOtherSectionsFramingAlone()
    {
        var binder = BinderOf(["a1", "a2"], ["b1", "b2"])
            .Select(1).Framed(Zoomed) // B zoomed in
            .Select(0).Next();        // page forward through A

        Assert.Equal(Zoomed, binder.Select(1).CurrentFraming);
    }

    /// <summary>
    /// Reference identity, for the reason paging has it and one more: the platform view reports the
    /// framing it was just given back as each gesture settles, so an unchanged framing arrives
    /// constantly. Rebuilding for that echo would publish a notification every time.
    /// </summary>
    [Fact]
    public void Framed_WithTheFramingItAlreadyHas_ReturnsTheSameInstance()
    {
        var binder = BinderOf(["p1"]).Framed(Zoomed);

        Assert.Same(binder, binder.Framed(Zoomed));
    }

    [Fact]
    public void Framed_AFittedSectionWithTheFit_ReturnsTheSameInstance()
    {
        var binder = BinderOf(["p1"]);

        Assert.Same(binder, binder.Framed(Framing.Fit));
    }

    [Fact]
    public void Empty_HasNothingToFrame() =>
        Assert.Same(Binder.Empty, Binder.Empty.Framed(Zoomed));

    // ── helpers ─────────────────────────────────────────────────────────────────

    /// <summary>Zoomed well in, reading the upper left of the page — any framing that isn't the fit.</summary>
    private static readonly Framing Zoomed = new(2.5, 0.2, 0.3);

    /// <summary>A binder over the given sections, each written as its pages' contents.</summary>
    private static Binder BinderOf(params string[][] sections) =>
        Binder.Of(sections.Select(PagesOf));

    private static IReadOnlyList<ReadOnlyMemory<byte>> PagesOf(string[] pages) =>
        [.. pages.Select(page => (ReadOnlyMemory<byte>)Encoding.UTF8.GetBytes(page))];

    /// <summary>The page on screen, decoded.</summary>
    private static string CurrentPage(Binder binder) => Encoding.UTF8.GetString(binder.CurrentPage.Span);
}
