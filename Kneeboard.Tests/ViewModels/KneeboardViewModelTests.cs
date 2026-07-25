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
        Assert.Equal(0, vm.SelectedSectionIndex);
        Assert.True(vm.Sections[0].IsSelected);
        Assert.False(vm.Sections[1].IsSelected);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public void SetDocument_SetsTitle()
    {
        var vm = Load(("A", ["a1"]), ("B", ["b1"]));

        Assert.Equal("EPKK 2026-06-10", vm.Title);
    }

    [Fact]
    public void SelectSection_UpdatesSelectedIndexAndIsSelected()
    {
        var vm = Load(("A", ["a1"]), ("B", ["b1"]));

        vm.SelectSectionCommand.Execute(vm.Sections[1]);

        Assert.Equal(1, vm.SelectedSectionIndex);
        Assert.False(vm.Sections[0].IsSelected);
        Assert.True(vm.Sections[1].IsSelected);
    }

    [Fact]
    public void SelectSection_UnvisitedSection_StartsAtPageZero()
    {
        var vm = Load(("A", ["a1", "a2", "a3", "a4"]), ("B", ["b1", "b2"]));
        vm.CurrentPageIndex = 3;

        vm.SelectSectionCommand.Execute(vm.Sections[1]);

        Assert.Equal(0, vm.CurrentPageIndex);
    }

    [Fact]
    public void SelectSection_RevisitedSection_RestoresLastViewedPage()
    {
        var vm = Load(("A", ["a1", "a2"]), ("B", ["b1", "b2", "b3"]));

        vm.SelectSectionCommand.Execute(vm.Sections[1]);
        vm.CurrentPageIndex = 2;

        vm.SelectSectionCommand.Execute(vm.Sections[0]);
        vm.SelectSectionCommand.Execute(vm.Sections[1]);

        Assert.Equal(2, vm.CurrentPageIndex);
    }

    [Fact]
    public void SelectSection_NeverPublishesPageFromStalePageIndex()
    {
        var vm = Load(("A", ["a1", "a2"]), ("B", ["b1", "b2"]));
        vm.NextPageCommand.Execute(null); // section A, page index 1

        var published = new List<string>();
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(KneeboardViewModel.CurrentPage))
                published.Add(Decode(vm.CurrentPage));
        };

        vm.SelectSectionCommand.Execute(vm.Sections[1]); // switch to section B

        Assert.NotEmpty(published);
        Assert.All(published, page => Assert.Equal("b1", page));
    }

    [Fact]
    public void CurrentPageDots_ReflectsCurrentPageIndex()
    {
        var vm = Load(("A", ["p1", "p2", "p3"]));

        vm.CurrentPageIndex = 1;

        Assert.Equal(3, vm.CurrentPageDots.Count);
        Assert.False(vm.CurrentPageDots[0]);
        Assert.True(vm.CurrentPageDots[1]);
        Assert.False(vm.CurrentPageDots[2]);
    }

    [Fact]
    public void CurrentPage_ReturnsPageAtCurrentIndex()
    {
        var vm = Load(("X", ["p1", "p2"]));

        vm.CurrentPageIndex = 1;

        Assert.Equal("p2", Decode(vm.CurrentPage));
    }

    [Fact]
    public void CurrentPage_EmptyWhenNoPages()
    {
        var vm = Load(("X", []));

        Assert.True(vm.CurrentPage.IsEmpty);
    }

    [Fact]
    public void NextPageCommand_AdvancesIndex()
    {
        var vm = Load(("X", ["p1", "p2"]));

        vm.NextPageCommand.Execute(null);

        Assert.Equal(1, vm.CurrentPageIndex);
    }

    [Fact]
    public void NextPageCommand_ClampsAtLastPage()
    {
        var vm = Load(("X", ["p1", "p2"]));
        vm.CurrentPageIndex = 1;

        vm.NextPageCommand.Execute(null);

        Assert.Equal(1, vm.CurrentPageIndex);
    }

    [Fact]
    public void PreviousPageCommand_DecrementsIndex()
    {
        var vm = Load(("X", ["p1", "p2"]));
        vm.CurrentPageIndex = 1;

        vm.PreviousPageCommand.Execute(null);

        Assert.Equal(0, vm.CurrentPageIndex);
    }

    [Fact]
    public void PreviousPageCommand_ClampsAtZero()
    {
        var vm = Load(("X", ["p1"]));

        vm.PreviousPageCommand.Execute(null);

        Assert.Equal(0, vm.CurrentPageIndex);
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
        Assert.True(vm.CurrentPage.IsEmpty);
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

    // ── helpers ────────────────────────────────────────────────────────────────

    private static KneeboardViewModel Load(params (string Label, string[] Pages)[] sections)
    {
        var vm = new KneeboardViewModel(
            new StubDocumentService(),
            new FakeSectionSource(sections.ToDictionary(s => s.Label, s => s.Pages)));

        vm.Document = DocumentFor(sections);
        return vm;
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

    /// <summary>Keys pages off the section's folder, which the document builder sets to the label.</summary>
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
