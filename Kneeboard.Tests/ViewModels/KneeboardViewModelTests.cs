using Kneeboard.Models;
using Kneeboard.Services;
using Kneeboard.ViewModels;
using Xunit;

namespace Kneeboard.Tests.ViewModels;

public class KneeboardViewModelTests : IDisposable
{
    private readonly List<string> _tempDirs = [];

    public void Dispose()
    {
        foreach (var d in _tempDirs)
            if (Directory.Exists(d)) Directory.Delete(d, recursive: true);
    }

    [Fact]
    public void SetDocument_LoadsAllSections_FirstSelected()
    {
        var doc = TwoSectionDoc();
        var vm = new KneeboardViewModel(new StubDocumentService(), new StubPdfService());

        vm.Document = doc;

        Assert.Equal(2, vm.Sections.Count);
        Assert.Equal(0, vm.SelectedSectionIndex);
        Assert.True(vm.Sections[0].IsSelected);
        Assert.False(vm.Sections[1].IsSelected);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public void SetDocument_SetsTitle()
    {
        var doc = TwoSectionDoc();
        var vm = new KneeboardViewModel(new StubDocumentService(), new StubPdfService());

        vm.Document = doc;

        Assert.Equal("EPKK 2026-06-10", vm.Title);
    }

    [Fact]
    public void SelectSection_UpdatesSelectedIndexAndIsSelected()
    {
        var doc = TwoSectionDoc();
        var vm = new KneeboardViewModel(new StubDocumentService(), new StubPdfService());
        vm.Document = doc;

        vm.SelectSectionCommand.Execute(vm.Sections[1]);

        Assert.Equal(1, vm.SelectedSectionIndex);
        Assert.False(vm.Sections[0].IsSelected);
        Assert.True(vm.Sections[1].IsSelected);
    }

    [Fact]
    public void SelectSection_ResetsPageIndexToZero()
    {
        var doc = TwoSectionDoc();
        var vm = new KneeboardViewModel(new StubDocumentService(), new StubPdfService());
        vm.Document = doc;
        vm.CurrentPageIndex = 3;

        vm.SelectSectionCommand.Execute(vm.Sections[1]);

        Assert.Equal(0, vm.CurrentPageIndex);
    }

    [Fact]
    public void CurrentPageDots_ReflectsCurrentPageIndex()
    {
        var folder = CreateTempFolder();
        File.WriteAllText(Path.Combine(folder, "p1.png"), "");
        File.WriteAllText(Path.Combine(folder, "p2.png"), "");
        File.WriteAllText(Path.Combine(folder, "p3.png"), "");

        var doc = new KneeboardDocument
        {
            Title = "T",
            Sections =
            [
                new() { Id = "a", Label = "A", Source = new ImageFolderSource { Folder = folder } }
            ]
        };
        var vm = new KneeboardViewModel(new StubDocumentService(), new StubPdfService());
        vm.Document = doc;

        vm.CurrentPageIndex = 1;

        Assert.Equal(3, vm.CurrentPageDots.Count);
        Assert.False(vm.CurrentPageDots[0]);
        Assert.True(vm.CurrentPageDots[1]);
        Assert.False(vm.CurrentPageDots[2]);
    }

    // ── helpers ────────────────────────────────────────────────────────────────

    private KneeboardDocument TwoSectionDoc() => new()
    {
        Title = "EPKK 2026-06-10",
        Sections =
        [
            new() { Id = "a", Label = "A", Source = new ImageFolderSource { Folder = CreateTempFolder() } },
            new() { Id = "b", Label = "B", Source = new ImageFolderSource { Folder = CreateTempFolder() } }
        ]
    };

    private string CreateTempFolder()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        _tempDirs.Add(dir);
        return dir;
    }

    private class StubDocumentService : IDocumentService
    {
        public Task<DocumentLoadResult> PickAndLoadAsync() => Task.FromResult(DocumentLoadResult.Cancelled());
        public Task<DocumentLoadResult> LoadFromPathAsync(string p) => Task.FromResult(DocumentLoadResult.Cancelled());
    }

    private class StubPdfService : IPdfService
    {
        public Task<IReadOnlyList<string>> RenderAllPagesAsync(string pdfPath) =>
            Task.FromResult<IReadOnlyList<string>>([]);
    }
}

public class KneeboardViewModelNavigationTests : IDisposable
{
    private readonly List<string> _tempDirs = [];

    public void Dispose()
    {
        foreach (var d in _tempDirs)
            if (Directory.Exists(d)) Directory.Delete(d, recursive: true);
    }

    [Fact]
    public void CurrentPage_WhenNoPagesLoaded_ReturnsEmpty()
    {
        var vm = new KneeboardViewModel(new StubDocumentService(), new StubPdfService());

        Assert.Equal(string.Empty, vm.CurrentPage);
    }

    [Fact]
    public void CurrentPage_ReturnsPageAtCurrentIndex()
    {
        var vm = new KneeboardViewModel(new StubDocumentService(), new StubPdfService());
        vm.Document = DocWithPages(3);

        vm.CurrentPageIndex = 1;

        Assert.Equal(vm.CurrentPages[1], vm.CurrentPage);
    }

    [Fact]
    public void CurrentPage_RaisesPropertyChangedWhenIndexChanges()
    {
        var vm = new KneeboardViewModel(new StubDocumentService(), new StubPdfService());
        vm.Document = DocWithPages(3);
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        vm.CurrentPageIndex = 2;

        Assert.Contains(nameof(vm.CurrentPage), raised);
    }

    [Fact]
    public void CurrentPage_RaisesPropertyChangedWhenSectionChanges()
    {
        var vm = new KneeboardViewModel(new StubDocumentService(), new StubPdfService());
        vm.Document = TwoSectionDocWithPages();
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        vm.SelectSectionCommand.Execute(vm.Sections[1]);

        Assert.Contains(nameof(vm.CurrentPage), raised);
    }

    [Fact]
    public void NavigateNext_IncrementsCurrentPageIndex()
    {
        var vm = new KneeboardViewModel(new StubDocumentService(), new StubPdfService());
        vm.Document = DocWithPages(3);
        vm.CurrentPageIndex = 0;

        vm.NavigateNextCommand.Execute(null);

        Assert.Equal(1, vm.CurrentPageIndex);
    }

    [Fact]
    public void NavigateNext_AtLastPage_DoesNothing()
    {
        var vm = new KneeboardViewModel(new StubDocumentService(), new StubPdfService());
        vm.Document = DocWithPages(3);
        vm.CurrentPageIndex = 2;

        vm.NavigateNextCommand.Execute(null);

        Assert.Equal(2, vm.CurrentPageIndex);
    }

    [Fact]
    public void NavigatePrevious_DecrementsCurrentPageIndex()
    {
        var vm = new KneeboardViewModel(new StubDocumentService(), new StubPdfService());
        vm.Document = DocWithPages(3);
        vm.CurrentPageIndex = 2;

        vm.NavigatePreviousCommand.Execute(null);

        Assert.Equal(1, vm.CurrentPageIndex);
    }

    [Fact]
    public void NavigatePrevious_AtFirstPage_DoesNothing()
    {
        var vm = new KneeboardViewModel(new StubDocumentService(), new StubPdfService());
        vm.Document = DocWithPages(3);
        vm.CurrentPageIndex = 0;

        vm.NavigatePreviousCommand.Execute(null);

        Assert.Equal(0, vm.CurrentPageIndex);
    }

    // ── helpers ────────────────────────────────────────────────────────────────

    private KneeboardDocument DocWithPages(int pageCount)
    {
        var folder = CreateTempFolder();
        for (var i = 0; i < pageCount; i++)
            File.WriteAllText(Path.Combine(folder, $"p{i:00}.png"), "");
        return new KneeboardDocument
        {
            Title = "T",
            Sections = [new() { Id = "a", Label = "A", Source = new ImageFolderSource { Folder = folder } }]
        };
    }

    private KneeboardDocument TwoSectionDocWithPages()
    {
        var folderA = CreateTempFolder();
        File.WriteAllText(Path.Combine(folderA, "p00.png"), "");
        var folderB = CreateTempFolder();
        File.WriteAllText(Path.Combine(folderB, "p00.png"), "");
        return new KneeboardDocument
        {
            Title = "T",
            Sections =
            [
                new() { Id = "a", Label = "A", Source = new ImageFolderSource { Folder = folderA } },
                new() { Id = "b", Label = "B", Source = new ImageFolderSource { Folder = folderB } }
            ]
        };
    }

    private string CreateTempFolder()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        _tempDirs.Add(dir);
        return dir;
    }

    private class StubDocumentService : IDocumentService
    {
        public Task<DocumentLoadResult> PickAndLoadAsync() => Task.FromResult(DocumentLoadResult.Cancelled());
        public Task<DocumentLoadResult> LoadFromPathAsync(string p) => Task.FromResult(DocumentLoadResult.Cancelled());
    }

    private class StubPdfService : IPdfService
    {
        public Task<IReadOnlyList<string>> RenderAllPagesAsync(string pdfPath) =>
            Task.FromResult<IReadOnlyList<string>>([]);
    }
}
