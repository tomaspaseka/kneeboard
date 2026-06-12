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
        var vm = new WelcomeViewModel(new TcsDocumentService(tcs), _nav);
        bool busyDuringLoad = false;

        var task = vm.OpenFileCommand.ExecuteAsync(null);
        busyDuringLoad = vm.IsBusy;
        tcs.SetResult(DocumentLoadResult.Cancelled());
        await task;

        Assert.True(busyDuringLoad);
        Assert.False(vm.IsBusy);
    }

    // ── test doubles ──────────────────────────────────────────────────────────

    private readonly SpyNavigationService _nav = new();

    private WelcomeViewModel BuildVm(DocumentLoadResult result) =>
        new(new StubDocumentService(result), _nav);

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
}
