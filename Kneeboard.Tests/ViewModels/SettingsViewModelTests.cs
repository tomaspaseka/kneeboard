using Kneeboard.Models;
using Kneeboard.Services;
using Kneeboard.ViewModels;
using Xunit;

namespace Kneeboard.Tests.ViewModels;

public class SettingsViewModelTests
{
    private readonly SpyNavigationService _nav = new();

    [Fact]
    public async Task Construction_PopulatesTabsFromService()
    {
        var existing = new GlobalTabSetting("id-1", "Checklist", new PdfSource { Path = "a.pdf" }, 0, true);
        var service = new FakeGlobalTabsSettingsService([existing]);
        var vm = new SettingsViewModel(service, _nav);

        await WaitForConstructionLoadAsync();

        Assert.Single(vm.Tabs);
        Assert.Equal(existing, vm.Tabs[0]);
    }

    [Fact]
    public async Task Add_WithPdfSource_AddsTabAndClearsForm()
    {
        var vm = BuildVm();

        vm.NewLabel = "Checklist";
        vm.ApplyPdfPick("C:/checklist.pdf");
        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Single(vm.Tabs);
        Assert.Equal("Checklist", vm.Tabs[0].Label);
        Assert.IsType<PdfSource>(vm.Tabs[0].Source);
        Assert.Equal("C:/checklist.pdf", ((PdfSource)vm.Tabs[0].Source).Path);
        Assert.Equal(string.Empty, vm.NewLabel);
        Assert.Null(vm.NewSource);
        Assert.Null(vm.NewSourceDisplay);
        Assert.Null(vm.ErrorMessage);
    }

    [Fact]
    public async Task Add_WithImageFolderSource_AddsTab()
    {
        var vm = BuildVm();

        vm.NewLabel = "Charts";
        vm.ApplyImageFolderPick("C:/charts");
        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Single(vm.Tabs);
        Assert.Equal("Charts", vm.Tabs[0].Label);
        Assert.IsType<ImageFolderSource>(vm.Tabs[0].Source);
        Assert.Equal("C:/charts", ((ImageFolderSource)vm.Tabs[0].Source).Folder);
    }

    [Fact]
    public async Task Add_WithoutSourcePicked_SetsErrorMessage_DoesNotAdd()
    {
        var vm = BuildVm();
        vm.NewLabel = "Checklist";

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Empty(vm.Tabs);
        Assert.NotNull(vm.ErrorMessage);
        Assert.True(vm.HasError);
    }

    [Fact]
    public async Task Add_WithoutLabel_SetsErrorMessage_DoesNotAdd()
    {
        var vm = BuildVm();
        vm.ApplyPdfPick("C:/checklist.pdf");

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Empty(vm.Tabs);
        Assert.NotNull(vm.ErrorMessage);
    }

    [Fact]
    public async Task Add_FourthEntry_IsBlockedWithClearReason()
    {
        var vm = BuildVm();
        await AddAsync(vm, "A");
        await AddAsync(vm, "B");
        await AddAsync(vm, "C");

        Assert.False(vm.CanAddMore);

        vm.NewLabel = "D";
        vm.ApplyPdfPick("d.pdf");
        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal(3, vm.Tabs.Count);
        Assert.NotNull(vm.ErrorMessage);
        Assert.Contains("3", vm.ErrorMessage);
    }

    [Fact]
    public async Task CanAddMore_IsFalseOnlyWhenThreeTabsExist()
    {
        var vm = BuildVm();
        Assert.True(vm.CanAddMore);

        await AddAsync(vm, "A");
        await AddAsync(vm, "B");
        Assert.True(vm.CanAddMore);

        await AddAsync(vm, "C");
        Assert.False(vm.CanAddMore);
    }

    [Fact]
    public async Task Edit_UpdatesLabelAndSource()
    {
        var vm = BuildVm();
        await AddAsync(vm, "Checklist", "old.pdf");
        var tab = vm.Tabs[0];

        vm.EditCommand.Execute(tab);
        Assert.True(vm.IsEditing);
        Assert.Equal("Checklist", vm.NewLabel);

        vm.NewLabel = "Checklist Renamed";
        vm.ApplyImageFolderPick("C:/new-folder");
        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Single(vm.Tabs);
        Assert.Equal("Checklist Renamed", vm.Tabs[0].Label);
        Assert.IsType<ImageFolderSource>(vm.Tabs[0].Source);
        Assert.False(vm.IsEditing);
        Assert.Equal(string.Empty, vm.NewLabel);
    }

    [Fact]
    public async Task CancelEdit_ResetsFormWithoutChangingTab()
    {
        var vm = BuildVm();
        await AddAsync(vm, "Checklist", "old.pdf");
        var tab = vm.Tabs[0];

        vm.EditCommand.Execute(tab);
        vm.NewLabel = "Something Else";

        vm.CancelEditCommand.Execute(null);

        Assert.False(vm.IsEditing);
        Assert.Equal(string.Empty, vm.NewLabel);
        Assert.Null(vm.NewSource);
        Assert.Equal("Checklist", vm.Tabs[0].Label);
    }

    [Fact]
    public async Task Remove_RemovesTabAndRenumbersOrder()
    {
        var vm = BuildVm();
        await AddAsync(vm, "A");
        await AddAsync(vm, "B");
        var toRemove = vm.Tabs[0];

        await vm.RemoveCommand.ExecuteAsync(toRemove);

        Assert.Single(vm.Tabs);
        Assert.Equal("B", vm.Tabs[0].Label);
        Assert.Equal(0, vm.Tabs[0].Order);
    }

    [Fact]
    public async Task MoveUp_SwapsWithPreviousEntry()
    {
        var vm = BuildVm();
        await AddAsync(vm, "A");
        await AddAsync(vm, "B");
        var b = vm.Tabs[1];

        await vm.MoveUpCommand.ExecuteAsync(b);

        Assert.Equal(["B", "A"], vm.Tabs.Select(t => t.Label));
    }

    [Fact]
    public async Task MoveDown_SwapsWithNextEntry()
    {
        var vm = BuildVm();
        await AddAsync(vm, "A");
        await AddAsync(vm, "B");
        var a = vm.Tabs[0];

        await vm.MoveDownCommand.ExecuteAsync(a);

        Assert.Equal(["B", "A"], vm.Tabs.Select(t => t.Label));
    }

    [Fact]
    public async Task ToggleEnabled_FlipsEnabledFlag()
    {
        var vm = BuildVm();
        await AddAsync(vm, "A");
        var a = vm.Tabs[0];
        Assert.True(a.Enabled);

        await vm.ToggleEnabledCommand.ExecuteAsync(a);
        Assert.False(vm.Tabs[0].Enabled);

        await vm.ToggleEnabledCommand.ExecuteAsync(vm.Tabs[0]);
        Assert.True(vm.Tabs[0].Enabled);
    }

    [Fact]
    public async Task Back_NavigatesToParentRoute()
    {
        var vm = BuildVm();

        await vm.BackCommand.ExecuteAsync(null);

        Assert.Single(_nav.Routes);
        Assert.Equal("..", _nav.Routes[0]);
    }

    // ── helpers ──────────────────────────────────────────────────────────

    private SettingsViewModel BuildVm() => new(new FakeGlobalTabsSettingsService(), _nav);

    private static async Task AddAsync(SettingsViewModel vm, string label, string path = "x.pdf")
    {
        vm.NewLabel = label;
        vm.ApplyPdfPick(path);
        await vm.SaveCommand.ExecuteAsync(null);
    }

    private static async Task WaitForConstructionLoadAsync()
    {
        // The constructor fire-and-forgets GetTabsAsync(); give it a tick to complete.
        await Task.Yield();
        await Task.Delay(1);
    }

    // ── test doubles ──────────────────────────────────────────────────────────

    private class SpyNavigationService : INavigationService
    {
        public List<string> Routes { get; } = [];
        public Task GoToAsync(string route, IDictionary<string, object>? parameters = null)
        {
            Routes.Add(route);
            return Task.CompletedTask;
        }
    }

    private class FakeGlobalTabsSettingsService(IReadOnlyList<GlobalTabSetting>? initial = null) : IGlobalTabsSettingsService
    {
        private const int MaxEntries = 3;
        private readonly List<GlobalTabSetting> _tabs = initial is null ? [] : [.. initial];

        public Task<IReadOnlyList<GlobalTabSetting>> GetTabsAsync() =>
            Task.FromResult<IReadOnlyList<GlobalTabSetting>>([.. _tabs]);

        public Task<GlobalTabSetting> AddAsync(string label, ContentSource source)
        {
            if (_tabs.Count >= MaxEntries)
                throw new InvalidOperationException($"Cannot add a Global Tab: the limit of {MaxEntries} is reached.");

            var tab = new GlobalTabSetting(Guid.NewGuid().ToString(), label, source, _tabs.Count, Enabled: true);
            _tabs.Add(tab);
            return Task.FromResult(tab);
        }

        public Task EditAsync(string id, string label, ContentSource source)
        {
            var index = _tabs.FindIndex(t => t.Id == id);
            if (index >= 0)
                _tabs[index] = _tabs[index] with { Label = label, Source = source };
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string id)
        {
            if (_tabs.RemoveAll(t => t.Id == id) > 0)
                Renumber();
            return Task.CompletedTask;
        }

        public Task ReorderAsync(string id, int newOrder)
        {
            var index = _tabs.FindIndex(t => t.Id == id);
            if (index >= 0)
            {
                var tab = _tabs[index];
                _tabs.RemoveAt(index);
                _tabs.Insert(Math.Clamp(newOrder, 0, _tabs.Count), tab);
                Renumber();
            }
            return Task.CompletedTask;
        }

        public Task SetEnabledAsync(string id, bool enabled)
        {
            var index = _tabs.FindIndex(t => t.Id == id);
            if (index >= 0)
                _tabs[index] = _tabs[index] with { Enabled = enabled };
            return Task.CompletedTask;
        }

        private void Renumber()
        {
            for (var i = 0; i < _tabs.Count; i++)
                _tabs[i] = _tabs[i] with { Order = i };
        }
    }
}
