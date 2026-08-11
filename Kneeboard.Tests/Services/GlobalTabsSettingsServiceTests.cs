using Kneeboard.Models;
using Kneeboard.Services;
using Xunit;

namespace Kneeboard.Tests.Services;

public class GlobalTabsSettingsServiceTests
{
    private readonly FakeKeyValueStore _store = new();
    private readonly GlobalTabsSettingsService _sut;

    public GlobalTabsSettingsServiceTests() => _sut = new GlobalTabsSettingsService(_store);

    [Fact]
    public async Task GetTabs_WhenStoreEmpty_ReturnsEmptyList()
    {
        var result = await _sut.GetTabsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task Add_ThenGetTabs_RoundTrips()
    {
        var source = new PdfSource { Path = "C:/checklist.pdf" };

        var added = await _sut.AddAsync("Checklist", source);
        var result = await _sut.GetTabsAsync();

        Assert.Single(result);
        Assert.Equal(added.Id, result[0].Id);
        Assert.Equal("Checklist", result[0].Label);
        Assert.IsType<PdfSource>(result[0].Source);
        Assert.Equal("C:/checklist.pdf", ((PdfSource)result[0].Source).Path);
        Assert.Equal(0, result[0].Order);
        Assert.True(result[0].Enabled);
    }

    [Fact]
    public async Task Add_MultipleEntries_AssignsIncreasingOrder()
    {
        await _sut.AddAsync("A", new PdfSource { Path = "a.pdf" });
        await _sut.AddAsync("B", new PdfSource { Path = "b.pdf" });
        await _sut.AddAsync("C", new PdfSource { Path = "c.pdf" });

        var result = await _sut.GetTabsAsync();

        Assert.Equal(["A", "B", "C"], result.Select(t => t.Label));
        Assert.Equal([0, 1, 2], result.Select(t => t.Order));
    }

    [Fact]
    public async Task Add_FourthEntry_Throws()
    {
        await _sut.AddAsync("A", new PdfSource { Path = "a.pdf" });
        await _sut.AddAsync("B", new PdfSource { Path = "b.pdf" });
        await _sut.AddAsync("C", new PdfSource { Path = "c.pdf" });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.AddAsync("D", new PdfSource { Path = "d.pdf" }));

        var result = await _sut.GetTabsAsync();
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task Edit_UpdatesLabelAndSource_ButNotOrderOrEnabled()
    {
        var added = await _sut.AddAsync("Checklist", new PdfSource { Path = "old.pdf" });
        await _sut.SetEnabledAsync(added.Id, false);

        await _sut.EditAsync(added.Id, "Checklist Renamed", new ImageFolderSource { Folder = "C:/pics" });

        var result = await _sut.GetTabsAsync();
        Assert.Single(result);
        Assert.Equal("Checklist Renamed", result[0].Label);
        Assert.IsType<ImageFolderSource>(result[0].Source);
        Assert.Equal("C:/pics", ((ImageFolderSource)result[0].Source).Folder);
        Assert.Equal(0, result[0].Order);
        Assert.False(result[0].Enabled);
    }

    [Fact]
    public async Task Edit_IdNotFound_IsNoOp()
    {
        await _sut.AddAsync("A", new PdfSource { Path = "a.pdf" });

        await _sut.EditAsync("missing-id", "B", new PdfSource { Path = "b.pdf" });

        var result = await _sut.GetTabsAsync();
        Assert.Single(result);
        Assert.Equal("A", result[0].Label);
    }

    [Fact]
    public async Task Remove_ById_RemovesOnlyThatEntryAndRenumbersOrder()
    {
        var a = await _sut.AddAsync("A", new PdfSource { Path = "a.pdf" });
        var b = await _sut.AddAsync("B", new PdfSource { Path = "b.pdf" });
        var c = await _sut.AddAsync("C", new PdfSource { Path = "c.pdf" });

        await _sut.RemoveAsync(b.Id);

        var result = await _sut.GetTabsAsync();
        Assert.Equal([a.Id, c.Id], result.Select(t => t.Id));
        Assert.Equal([0, 1], result.Select(t => t.Order));
    }

    [Fact]
    public async Task Remove_IdNotFound_IsNoOp()
    {
        var a = await _sut.AddAsync("A", new PdfSource { Path = "a.pdf" });

        await _sut.RemoveAsync("missing-id");

        var result = await _sut.GetTabsAsync();
        Assert.Single(result);
        Assert.Equal(a.Id, result[0].Id);
    }

    [Fact]
    public async Task Reorder_MovesEntryAndRenumbersOthers_RoundTrips()
    {
        var a = await _sut.AddAsync("A", new PdfSource { Path = "a.pdf" });
        var b = await _sut.AddAsync("B", new PdfSource { Path = "b.pdf" });
        var c = await _sut.AddAsync("C", new PdfSource { Path = "c.pdf" });

        await _sut.ReorderAsync(c.Id, 0);

        var result = await _sut.GetTabsAsync();
        Assert.Equal([c.Id, a.Id, b.Id], result.Select(t => t.Id));
        Assert.Equal([0, 1, 2], result.Select(t => t.Order));
    }

    [Fact]
    public async Task SetEnabled_TogglesFlag_RoundTrips()
    {
        var a = await _sut.AddAsync("A", new PdfSource { Path = "a.pdf" });
        Assert.True((await _sut.GetTabsAsync())[0].Enabled);

        await _sut.SetEnabledAsync(a.Id, false);
        Assert.False((await _sut.GetTabsAsync())[0].Enabled);

        await _sut.SetEnabledAsync(a.Id, true);
        Assert.True((await _sut.GetTabsAsync())[0].Enabled);
    }

    // ── test doubles ──────────────────────────────────────────────────────────

    private class FakeKeyValueStore : IKeyValueStore
    {
        private readonly Dictionary<string, string> _values = [];

        public string? Get(string key) => _values.TryGetValue(key, out var value) ? value : null;

        public void Set(string key, string value) => _values[key] = value;
    }
}
