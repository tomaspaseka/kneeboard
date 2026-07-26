using System.ComponentModel;
using System.Text;
using Kneeboard.Models;
using Kneeboard.Services;
using Kneeboard.ViewModels;
using Xunit;

namespace Kneeboard.Tests.ViewModels;

public class KneeboardViewModelTests
{
    [Fact]
    public void SetDocument_LoadsAllSections_FirstSelected()
    {
        var vm = Load(("A", ["a1"]), ("B", ["b1"]));

        Assert.Equal(2, vm.Sections.Count);
        Assert.Equal([true, false], SelectedTabs(vm));
        Assert.Equal("a1", CurrentPage(vm));
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public void SetDocument_SetsTitle()
    {
        var vm = Load(("A", ["a1"]), ("B", ["b1"]));

        Assert.Equal("EPKK 2026-06-10", vm.Title);
    }

    [Fact]
    public void SelectSection_HighlightsThatTabAndShowsItsPage()
    {
        var vm = Load(("A", ["a1"]), ("B", ["b1"]));

        vm.SelectSectionCommand.Execute(vm.Sections[1]);

        Assert.Equal([false, true], SelectedTabs(vm));
        Assert.Equal("b1", CurrentPage(vm));
    }

    [Fact]
    public void SelectSection_UnvisitedSection_StartsAtItsFirstPage()
    {
        var vm = Load(("A", ["a1", "a2", "a3", "a4"]), ("B", ["b1", "b2"]));
        TurnForward(vm, 3); // fourth page of A

        vm.SelectSectionCommand.Execute(vm.Sections[1]);

        Assert.Equal("b1", CurrentPage(vm));
    }

    [Fact]
    public void SelectSection_RevisitedSection_RestoresLastViewedPage()
    {
        var vm = Load(("A", ["a1", "a2"]), ("B", ["b1", "b2", "b3"]));

        vm.SelectSectionCommand.Execute(vm.Sections[1]);
        TurnForward(vm, 2); // third page of B

        vm.SelectSectionCommand.Execute(vm.Sections[0]);
        vm.SelectSectionCommand.Execute(vm.Sections[1]);

        Assert.Equal("b3", CurrentPage(vm));
    }

    /// <summary>
    /// Switching tabs must never announce the page the pilot was on in the section they left, paired
    /// with the section they arrived at — the native image view picks that up as a real, if transient,
    /// wrong page. A tripwire: it fails if the screen publishes anything but the arrived-at page.
    /// </summary>
    [Fact]
    public void SelectSection_NeverPublishesStalePage()
    {
        var vm = Load(("A", ["a1", "a2"]), ("B", ["b1", "b2"]));
        vm.NextPageCommand.Execute(null); // second page of A

        var published = PagesPublishedDuring(vm, () => vm.SelectSectionCommand.Execute(vm.Sections[1]));

        Assert.NotEmpty(published);
        Assert.All(published, page => Assert.Equal("b1", page));
    }

    // ── framing ────────────────────────────────────────────────────────────────

    [Fact]
    public void SelectSection_RevisitedSection_RestoresHowItWasFramed()
    {
        var vm = Load(("A", ["a1"]), ("B", ["b1"]));

        vm.SelectSectionCommand.Execute(vm.Sections[1]);
        vm.CurrentFraming = Zoomed;                      // the pilot zooms into B
        vm.SelectSectionCommand.Execute(vm.Sections[0]); // away to A
        vm.SelectSectionCommand.Execute(vm.Sections[1]); // and back

        Assert.Equal(Zoomed, CurrentFraming(vm));
    }

    [Fact]
    public void SelectSection_UnvisitedSection_StartsFittedToTheScreen()
    {
        var vm = Load(("A", ["a1"]), ("B", ["b1"]));
        vm.CurrentFraming = Zoomed;

        vm.SelectSectionCommand.Execute(vm.Sections[1]);

        Assert.Equal(Framing.Fit, CurrentFraming(vm));
    }

    [Fact]
    public void NextPageCommand_ReturnsTheSectionToTheFit()
    {
        var vm = Load(("A", ["a1", "a2"]));
        vm.CurrentFraming = Zoomed;

        vm.NextPageCommand.Execute(null);

        Assert.Equal(Framing.Fit, CurrentFraming(vm));
    }

    /// <summary>A tap at the end of a section turns nothing, so it takes nothing away either.</summary>
    [Fact]
    public void NextPageCommand_AtTheLastPage_KeepsTheFraming()
    {
        var vm = Load(("A", ["a1"]));
        vm.CurrentFraming = Zoomed;

        vm.NextPageCommand.Execute(null);

        Assert.Equal(Zoomed, CurrentFraming(vm));
    }

    [Fact]
    public void SetDocument_StartsEverySectionFittedToTheScreen()
    {
        var vm = Load(("A", ["a1"]));
        vm.CurrentFraming = Zoomed;

        vm.Document = DocumentFor([("A", ["a1"])]); // another mission opened

        Assert.Equal(Framing.Fit, CurrentFraming(vm));
    }

    /// <summary>
    /// The image view is handed the page and the framing separately, and a framing means nothing until
    /// the page it frames is on screen — its centre is measured against that page's fitted size. So the
    /// page has to be announced first, or the framing lands against the page the pilot just left.
    /// </summary>
    [Fact]
    public void SelectSection_AnnouncesThePageBeforeTheFraming()
    {
        var vm = Load(("A", ["a1"]), ("B", ["b1"]));
        vm.SelectSectionCommand.Execute(vm.Sections[1]);
        vm.CurrentFraming = Zoomed;
        vm.SelectSectionCommand.Execute(vm.Sections[0]);

        var announced = PropertiesAnnouncedDuring(vm, () => vm.SelectSectionCommand.Execute(vm.Sections[1]));

        // Both asserted present before their order is compared: IndexOf answers -1 for a name that was
        // never announced, and -1 is less than everything — so the comparison alone would hold even if
        // the screen stopped announcing the page at all.
        Assert.Contains(PageAnnouncement, announced);
        Assert.Contains(FramingAnnouncement, announced);
        Assert.True(
            announced.IndexOf(PageAnnouncement) < announced.IndexOf(FramingAnnouncement),
            $"page must be announced before framing, got: {string.Join(", ", announced)}");
    }

    [Fact]
    public void PageDots_LightTheDotForThePageOnScreen()
    {
        var vm = Load(("A", ["p1", "p2", "p3"]));

        vm.NextPageCommand.Execute(null);

        Assert.Equal([false, true, false], PageDots(vm));
    }

    [Fact]
    public void CurrentPage_IsThePageTurnedTo()
    {
        var vm = Load(("X", ["p1", "p2", "p3"]));

        TurnForward(vm, 2);

        Assert.Equal("p3", CurrentPage(vm));
    }

    [Fact]
    public void CurrentPage_EmptyWhenNoPages()
    {
        var vm = Load(("X", []));

        Assert.Empty(CurrentPage(vm));
    }

    [Fact]
    public void NextPageCommand_ShowsTheNextPage()
    {
        var vm = Load(("X", ["p1", "p2"]));

        vm.NextPageCommand.Execute(null);

        Assert.Equal("p2", CurrentPage(vm));
    }

    [Fact]
    public void NextPageCommand_ClampsAtLastPage()
    {
        var vm = Load(("X", ["p1", "p2"]));
        TurnForward(vm, 1); // last page of X

        vm.NextPageCommand.Execute(null);

        Assert.Equal("p2", CurrentPage(vm));
    }

    [Fact]
    public void PreviousPageCommand_ShowsThePreviousPage()
    {
        var vm = Load(("X", ["p1", "p2"]));
        TurnForward(vm, 1); // second page of X

        vm.PreviousPageCommand.Execute(null);

        Assert.Equal("p1", CurrentPage(vm));
    }

    [Fact]
    public void PreviousPageCommand_ClampsAtFirstPage()
    {
        var vm = Load(("X", ["p1"]));

        vm.PreviousPageCommand.Execute(null);

        Assert.Equal("p1", CurrentPage(vm));
    }

    [Fact]
    public void SetDocument_WhenSectionSourceThrows_SurfacesErrorAndFinishesLoading()
    {
        var vm = new KneeboardViewModel(new StubDocumentService(), new ThrowingSectionSource("folder not found"));

        vm.Document = DocumentFor([("A", [])]);

        Assert.Equal("folder not found", vm.ErrorMessage);
        Assert.True(vm.HasError);
        Assert.Empty(vm.Sections);
        Assert.False(vm.IsLoading);
        Assert.Empty(CurrentPage(vm));
    }

    [Fact]
    public void SetDocument_WhenABindingThrowsOnNotification_StillFinishesLoading()
    {
        var vm = new KneeboardViewModel(
            new StubDocumentService(),
            new FakeSectionSource(new Dictionary<string, string[]> { ["A"] = ["a1"] }));

        // A binding that throws while consuming a page notification — what BindableLayout does when
        // a dot template fails to hydrate, and the dots are the first page notification of the load.
        // It must not be able to leave the loading overlay up.
        vm.PropertyChanged += (_, e) =>
        {
            if (AnnouncesPage(e))
                throw new InvalidOperationException("binding blew up");
        };

        vm.Document = DocumentFor([("A", ["a1"])]);

        Assert.False(vm.IsLoading);
    }

    [Fact]
    public void SetDocument_AfterFailure_ClearsPreviousError()
    {
        var vm = new KneeboardViewModel(new StubDocumentService(), new ThrowingSectionSource("folder not found"));
        vm.Document = DocumentFor([("A", [])]);

        var recovered = Load(("A", ["a1"]));

        Assert.True(vm.HasError);
        Assert.False(recovered.HasError);
        Assert.Null(recovered.ErrorMessage);
    }

    // ── the screen's surface ───────────────────────────────────────────────────
    //
    // The only place in this file that names how the screen exposes where the pilot is in the
    // document. Every assertion about the page, the dots, the highlighted tab or the framing goes
    // through the accessors below, so when that surface changes, this is the one place that changes
    // with it.
    //
    // The announcement names are the other half of the same surface: some tests observe what the
    // screen publishes rather than what it holds, and a notification can only be recognised by name.
    // They live here for the same reason the accessors do.

    /// <summary>The page on screen, decoded — what the image view is showing.</summary>
    private static string CurrentPage(KneeboardViewModel vm) => Decode(vm.CurrentPage);

    /// <summary>One dot per page of the section on screen, lit for the page the pilot is on.</summary>
    private static IReadOnlyList<bool> PageDots(KneeboardViewModel vm) => vm.CurrentPageDots;

    /// <summary>How the section on screen is framed — what the image view is zoomed and panned to.</summary>
    private static Framing CurrentFraming(KneeboardViewModel vm) => vm.CurrentFraming;

    /// <summary>The tab bar's highlight, one flag per tab.</summary>
    private static IReadOnlyList<bool> SelectedTabs(KneeboardViewModel vm) =>
        [.. vm.Sections.Select(s => s.IsSelected)];

    /// <summary>The notifications on which the markup re-reads the page and its dots.</summary>
    private static bool AnnouncesPage(PropertyChangedEventArgs e) =>
        e.PropertyName is nameof(KneeboardViewModel.CurrentPage) or nameof(KneeboardViewModel.CurrentPageDots);

    /// <summary>The notification on which the image view re-reads the page to show.</summary>
    private static readonly string PageAnnouncement = nameof(KneeboardViewModel.CurrentPage);

    /// <summary>The notification on which the image view re-reads how to frame that page.</summary>
    private static readonly string FramingAnnouncement = nameof(KneeboardViewModel.CurrentFraming);

    // ── helpers ────────────────────────────────────────────────────────────────

    /// <summary>Zoomed well in, reading the upper left of the page — any framing that isn't the fit.</summary>
    private static readonly Framing Zoomed = new(2.5, 0.2, 0.3);

    private static KneeboardViewModel Load(params (string Label, string[] Pages)[] sections)
    {
        var vm = new KneeboardViewModel(
            new StubDocumentService(),
            new FakeSectionSource(sections.ToDictionary(s => s.Label, s => s.Pages)));

        vm.Document = DocumentFor(sections);
        return vm;
    }

    /// <summary>Turns forward the way repeated taps on the page edge do.</summary>
    private static void TurnForward(KneeboardViewModel vm, int pages)
    {
        for (var i = 0; i < pages; i++)
            vm.NextPageCommand.Execute(null);
    }

    /// <summary>Every page the screen announced while <paramref name="act"/> ran, in order.</summary>
    private static List<string> PagesPublishedDuring(KneeboardViewModel vm, Action act)
    {
        var published = new List<string>();

        void Record(object? _, PropertyChangedEventArgs e)
        {
            if (AnnouncesPage(e))
                published.Add(CurrentPage(vm));
        }

        vm.PropertyChanged += Record;
        try
        {
            act();
        }
        finally
        {
            vm.PropertyChanged -= Record;
        }

        return published;
    }

    /// <summary>Every property the screen announced while <paramref name="act"/> ran, in order.</summary>
    private static List<string> PropertiesAnnouncedDuring(KneeboardViewModel vm, Action act)
    {
        var announced = new List<string>();

        void Record(object? _, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is not null)
                announced.Add(e.PropertyName);
        }

        vm.PropertyChanged += Record;
        try
        {
            act();
        }
        finally
        {
            vm.PropertyChanged -= Record;
        }

        return announced;
    }

    private static KneeboardDocument DocumentFor((string Label, string[] Pages)[] sections) => new()
    {
        Title = "EPKK 2026-06-10",
        Sections =
        [
            .. sections.Select(s => new KneeboardSection
            {
                Id = s.Label,
                Label = s.Label,
                Source = new ImageFolderSource { Folder = s.Label }
            })
        ]
    };

    private static string Decode(ReadOnlyMemory<byte> page) => Encoding.UTF8.GetString(page.Span);

    // ── test doubles ───────────────────────────────────────────────────────────

    /// <summary>
    /// Keys pages off the section's folder, which the document builder sets to the label.
    ///
    /// Must complete synchronously. The screen starts its load without awaiting it, so every test
    /// here reads state on the line after it assigns the document; a fake that genuinely awaited
    /// would make the whole file race.
    /// </summary>
    private sealed class FakeSectionSource(Dictionary<string, string[]> pagesBySection) : ISectionSource
    {
        public Task<IReadOnlyList<ReadOnlyMemory<byte>>> GetPagesAsync(ContentSource? source)
        {
            var key = ((ImageFolderSource)source!).Folder;
            return Task.FromResult<IReadOnlyList<ReadOnlyMemory<byte>>>(
                [.. pagesBySection[key].Select(page => (ReadOnlyMemory<byte>)Encoding.UTF8.GetBytes(page))]);
        }
    }

    private sealed class ThrowingSectionSource(string message) : ISectionSource
    {
        public Task<IReadOnlyList<ReadOnlyMemory<byte>>> GetPagesAsync(ContentSource? source) =>
            throw new DirectoryNotFoundException(message);
    }

    private sealed class StubDocumentService : IDocumentService
    {
        public Task<DocumentLoadResult> PickAndLoadAsync() => Task.FromResult(DocumentLoadResult.Cancelled());
        public Task<DocumentLoadResult> LoadFromPathAsync(string p) => Task.FromResult(DocumentLoadResult.Cancelled());
    }
}
