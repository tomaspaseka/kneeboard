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
