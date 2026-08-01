using System.Text.Json;
using Kneeboard.Models;

namespace Kneeboard.Services;

public class RecentDocumentsService(IKeyValueStore store) : IRecentDocumentsService
{
    private const string StoreKey = "RecentDocuments";
    private const int MaxEntries = 4;

    public Task<IReadOnlyList<RecentDocument>> GetRecentAsync() =>
        Task.FromResult<IReadOnlyList<RecentDocument>>(Load());

    public Task RecordOpenedAsync(RecentDocument doc)
    {
        var docs = Load();
        docs.RemoveAll(d => d.Path == doc.Path);
        docs.Insert(0, doc);

        if (docs.Count > MaxEntries)
            docs.RemoveRange(MaxEntries, docs.Count - MaxEntries);

        Save(docs);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string path)
    {
        var docs = Load();
        docs.RemoveAll(d => d.Path == path);
        Save(docs);
        return Task.CompletedTask;
    }

    private List<RecentDocument> Load()
    {
        var json = store.Get(StoreKey);
        if (string.IsNullOrEmpty(json))
            return [];

        return JsonSerializer.Deserialize<List<RecentDocument>>(json) ?? [];
    }

    private void Save(List<RecentDocument> docs) =>
        store.Set(StoreKey, JsonSerializer.Serialize(docs));
}
