using Kneeboard.Models;
using Kneeboard.Services;
using Kneeboard.ViewModels;
using Xunit;

namespace Kneeboard.Tests.ViewModels;

public class WelcomeViewModelTests
{
    [Fact]
    public async Task OpenFile_WhenCancelled_DoesNotNavigateAndClearsError()
    {
        var vm = BuildVm(DocumentLoadResult.Cancelled());

        await vm.OpenFileCommand.ExecuteAsync(null);

        Assert.Empty(_nav.Routes);
        Assert.Null(vm.ErrorMessage);
        Assert.False(vm.HasError);
    }

    [Fact]
    public async Task OpenFile_WhenFailed_SetsErrorMessage()
    {
        var vm = BuildVm(DocumentLoadResult.Failed("bad file"));

        await vm.OpenFileCommand.ExecuteAsync(null);

        Assert.Equal("bad file", vm.ErrorMessage);
        Assert.True(vm.HasError);
        Assert.Empty(_nav.Routes);
    }

    [Fact]
    public async Task OpenFile_WhenSuccessful_NavigatesToKneeboard()
    {
        var doc = new KneeboardDocument { Title = "T", Sections = [] };
        var vm = BuildVm(DocumentLoadResult.Succeeded(doc));

        await vm.OpenFileCommand.ExecuteAsync(null);

        Assert.Single(_nav.Routes);
        Assert.Equal("kneeboard", _nav.Routes[0]);
        Assert.Null(vm.ErrorMessage);
    }

    [Fact]
    public async Task OpenFile_DuringLoad_IsBusyIsTrue()
    {
        var tcs = new TaskCompletionSource<DocumentLoadResult>();
        var vm = new WelcomeViewModel(new TcsDocumentService(tcs), _nav, new StubRecentDocumentsService());
        bool busyDuringLoad = false;

        var task = vm.OpenFileCommand.ExecuteAsync(null);
        busyDuringLoad = vm.IsBusy;
        tcs.SetResult(DocumentLoadResult.Cancelled());
        await task;

        Assert.True(busyDuringLoad);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task Construction_PopulatesRecentDocumentsFromService()
    {
        var recent = new RecentDocument("C:\\a.kneeboard", "A", DateTimeOffset.UtcNow);
        var recentService = new StubRecentDocumentsService([recent]);
        var vm = new WelcomeViewModel(new StubDocumentService(DocumentLoadResult.Cancelled()), _nav, recentService);

        await WaitForConstructionLoadAsync(vm);

        Assert.Single(vm.RecentDocuments);
        Assert.Equal(recent, vm.RecentDocuments[0]);
        Assert.True(vm.HasRecentDocuments);
    }

    [Fact]
    public async Task Construction_WhenNoRecentDocuments_HasRecentDocumentsIsFalse()
    {
        var vm = new WelcomeViewModel(new StubDocumentService(DocumentLoadResult.Cancelled()), _nav, new StubRecentDocumentsService());

        await WaitForConstructionLoadAsync(vm);

        Assert.Empty(vm.RecentDocuments);
        Assert.False(vm.HasRecentDocuments);
    }

    [Fact]
    public async Task OpenRecent_WhenSuccessful_NavigatesToKneeboard()
    {
        var doc = new KneeboardDocument { Title = "T", Sections = [] };
        var recent = new RecentDocument("C:\\a.kneeboard", "A", DateTimeOffset.UtcNow);
        var vm = new WelcomeViewModel(
            new StubDocumentService(DocumentLoadResult.Succeeded(doc)),
            _nav,
            new StubRecentDocumentsService([recent]));

        await vm.OpenRecentCommand.ExecuteAsync(recent);

        Assert.Single(_nav.Routes);
        Assert.Equal("kneeboard", _nav.Routes[0]);
        Assert.Null(vm.ErrorMessage);
    }

    [Fact]
    public async Task OpenRecent_WhenFailed_SetsErrorAndRemovesEntry()
    {
        var recent = new RecentDocument("C:\\a.kneeboard", "A", DateTimeOffset.UtcNow);
        var recentService = new StubRecentDocumentsService([recent]);
        var vm = new WelcomeViewModel(
            new StubDocumentService(DocumentLoadResult.Failed("missing file")),
            _nav,
            recentService);

        await WaitForConstructionLoadAsync(vm);
        await vm.OpenRecentCommand.ExecuteAsync(recent);

        Assert.Equal("missing file", vm.ErrorMessage);
        Assert.Empty(_nav.Routes);
        Assert.DoesNotContain(recent, vm.RecentDocuments);
        Assert.Contains(recent.Path, recentService.RemovedPaths);
    }

    // ── test doubles ──────────────────────────────────────────────────────────

    private readonly SpyNavigationService _nav = new();

    private WelcomeViewModel BuildVm(DocumentLoadResult result) =>
        new(new StubDocumentService(result), _nav, new StubRecentDocumentsService());

    private static async Task WaitForConstructionLoadAsync(WelcomeViewModel vm)
    {
        // The constructor fire-and-forgets GetRecentAsync(); give it a tick to complete.
        await Task.Yield();
        await Task.Delay(1);
    }

    private class StubDocumentService(DocumentLoadResult result) : IDocumentService
    {
        public Task<DocumentLoadResult> PickAndLoadAsync() => Task.FromResult(result);
        public Task<DocumentLoadResult> LoadFromPathAsync(string path) => Task.FromResult(result);
    }

    private class TcsDocumentService(TaskCompletionSource<DocumentLoadResult> tcs) : IDocumentService
    {
        public Task<DocumentLoadResult> PickAndLoadAsync() => tcs.Task;
        public Task<DocumentLoadResult> LoadFromPathAsync(string path) => tcs.Task;
    }

    private class SpyNavigationService : INavigationService
    {
        public List<string> Routes { get; } = [];
        public Task GoToAsync(string route, IDictionary<string, object>? parameters = null)
        {
            Routes.Add(route);
            return Task.CompletedTask;
        }
    }

    private class StubRecentDocumentsService(IReadOnlyList<RecentDocument>? recent = null) : IRecentDocumentsService
    {
        private readonly List<RecentDocument> _recent = recent is null ? [] : [.. recent];

        public List<string> RemovedPaths { get; } = [];

        public Task<IReadOnlyList<RecentDocument>> GetRecentAsync() =>
            Task.FromResult<IReadOnlyList<RecentDocument>>(_recent);

        public Task RecordOpenedAsync(RecentDocument doc)
        {
            _recent.RemoveAll(d => d.Path == doc.Path);
            _recent.Insert(0, doc);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string path)
        {
            RemovedPaths.Add(path);
            _recent.RemoveAll(d => d.Path == path);
            return Task.CompletedTask;
        }
    }
}
