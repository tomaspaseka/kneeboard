using Kneeboard.Models;
using Kneeboard.Services;
using Xunit;

namespace Kneeboard.Tests.Services;

public class RecentDocumentsServiceTests
{
    private readonly FakeKeyValueStore _store = new();
    private readonly RecentDocumentsService _sut;

    public RecentDocumentsServiceTests() => _sut = new RecentDocumentsService(_store);

    [Fact]
    public async Task GetRecent_WhenStoreEmpty_ReturnsEmptyList()
    {
        var result = await _sut.GetRecentAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task RecordOpened_ThenGetRecent_RoundTrips()
    {
        var doc = new RecentDocument("C:/a.kneeboard", "A", DateTimeOffset.UtcNow);

        await _sut.RecordOpenedAsync(doc);
        var result = await _sut.GetRecentAsync();

        Assert.Single(result);
        Assert.Equal(doc, result[0]);
    }

    [Fact]
    public async Task RecordOpened_ExistingPath_MovesToFrontAndUpdatesFields_WithoutDuplicating()
    {
        var original = new RecentDocument("C:/a.kneeboard", "A", DateTimeOffset.UtcNow.AddMinutes(-10));
        var other = new RecentDocument("C:/b.kneeboard", "B", DateTimeOffset.UtcNow.AddMinutes(-5));
        await _sut.RecordOpenedAsync(original);
        await _sut.RecordOpenedAsync(other);

        var updated = new RecentDocument("C:/a.kneeboard", "A renamed", DateTimeOffset.UtcNow);
        await _sut.RecordOpenedAsync(updated);

        var result = await _sut.GetRecentAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal(updated, result[0]);
        Assert.Equal(other, result[1]);
    }

    [Fact]
    public async Task RecordOpened_FifthDistinctPath_EvictsOldest()
    {
        var docs = Enumerable.Range(1, 4)
            .Select(i => new RecentDocument($"C:/{i}.kneeboard", $"Doc {i}", DateTimeOffset.UtcNow.AddMinutes(-i)))
            .ToList();
        foreach (var doc in docs)
            await _sut.RecordOpenedAsync(doc);

        var fifth = new RecentDocument("C:/5.kneeboard", "Doc 5", DateTimeOffset.UtcNow);
        await _sut.RecordOpenedAsync(fifth);

        var result = await _sut.GetRecentAsync();

        Assert.Equal(4, result.Count);
        Assert.DoesNotContain(result, d => d.Path == "C:/1.kneeboard");
        Assert.Contains(result, d => d.Path == "C:/5.kneeboard");
    }

    [Fact]
    public async Task Remove_ByPath_RemovesOnlyThatEntry()
    {
        var a = new RecentDocument("C:/a.kneeboard", "A", DateTimeOffset.UtcNow.AddMinutes(-2));
        var b = new RecentDocument("C:/b.kneeboard", "B", DateTimeOffset.UtcNow.AddMinutes(-1));
        var c = new RecentDocument("C:/c.kneeboard", "C", DateTimeOffset.UtcNow);
        await _sut.RecordOpenedAsync(a);
        await _sut.RecordOpenedAsync(b);
        await _sut.RecordOpenedAsync(c);

        await _sut.RemoveAsync("C:/b.kneeboard");

        var result = await _sut.GetRecentAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal(c, result[0]);
        Assert.Equal(a, result[1]);
    }

    [Fact]
    public async Task Remove_PathNotFound_IsNoOp()
    {
        var a = new RecentDocument("C:/a.kneeboard", "A", DateTimeOffset.UtcNow);
        await _sut.RecordOpenedAsync(a);

        await _sut.RemoveAsync("C:/does-not-exist.kneeboard");

        var result = await _sut.GetRecentAsync();
        Assert.Single(result);
        Assert.Equal(a, result[0]);
    }

    [Fact]
    public async Task Ordering_IsAlwaysMostRecentFirst()
    {
        var a = new RecentDocument("C:/a.kneeboard", "A", DateTimeOffset.UtcNow.AddMinutes(-3));
        var b = new RecentDocument("C:/b.kneeboard", "B", DateTimeOffset.UtcNow.AddMinutes(-2));
        var c = new RecentDocument("C:/c.kneeboard", "C", DateTimeOffset.UtcNow.AddMinutes(-1));

        await _sut.RecordOpenedAsync(a);
        await _sut.RecordOpenedAsync(b);
        await _sut.RecordOpenedAsync(c);

        var result = await _sut.GetRecentAsync();

        Assert.Equal([c.Path, b.Path, a.Path], result.Select(d => d.Path));
    }

    // ── test doubles ──────────────────────────────────────────────────────────

    private class FakeKeyValueStore : IKeyValueStore
    {
        private readonly Dictionary<string, string> _values = [];

        public string? Get(string key) => _values.TryGetValue(key, out var value) ? value : null;

        public void Set(string key, string value) => _values[key] = value;
    }
}
