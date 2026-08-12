using System.Text.Json;
using Kneeboard.Models;

namespace Kneeboard.Services;

public class GlobalTabsSettingsService(IKeyValueStore store) : IGlobalTabsSettingsService
{
    private const string StoreKey = "GlobalTabs";
    private const int MaxEntries = 3;

    public Task<IReadOnlyList<GlobalTabSetting>> GetTabsAsync() =>
        Task.FromResult<IReadOnlyList<GlobalTabSetting>>(Load());

    public Task<GlobalTabSetting> AddAsync(string label, ContentSource source)
    {
        var tabs = Load();
        if (tabs.Count >= MaxEntries)
            throw new InvalidOperationException($"Cannot add a Global Tab: the limit of {MaxEntries} is reached.");

        var tab = new GlobalTabSetting(Guid.NewGuid().ToString(), label, source, tabs.Count, Enabled: true);
        tabs.Add(tab);
        Save(tabs);
        return Task.FromResult(tab);
    }

    public Task EditAsync(string id, string label, ContentSource source)
    {
        var tabs = Load();
        var index = tabs.FindIndex(t => t.Id == id);
        if (index >= 0)
        {
            tabs[index] = tabs[index] with { Label = label, Source = source };
            Save(tabs);
        }
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string id)
    {
        var tabs = Load();
        if (tabs.RemoveAll(t => t.Id == id) > 0)
        {
            Renumber(tabs);
            Save(tabs);
        }
        return Task.CompletedTask;
    }

    public Task ReorderAsync(string id, int newOrder)
    {
        var tabs = Load();
        var index = tabs.FindIndex(t => t.Id == id);
        if (index >= 0)
        {
            var tab = tabs[index];
            tabs.RemoveAt(index);
            tabs.Insert(Math.Clamp(newOrder, 0, tabs.Count), tab);
            Renumber(tabs);
            Save(tabs);
        }
        return Task.CompletedTask;
    }

    public Task SetEnabledAsync(string id, bool enabled)
    {
        var tabs = Load();
        var index = tabs.FindIndex(t => t.Id == id);
        if (index >= 0)
        {
            tabs[index] = tabs[index] with { Enabled = enabled };
            Save(tabs);
        }
        return Task.CompletedTask;
    }

    private static void Renumber(List<GlobalTabSetting> tabs)
    {
        for (var i = 0; i < tabs.Count; i++)
            tabs[i] = tabs[i] with { Order = i };
    }

    private List<GlobalTabSetting> Load()
    {
        var json = store.Get(StoreKey);
        if (string.IsNullOrEmpty(json))
            return [];

        return JsonSerializer.Deserialize<List<GlobalTabSetting>>(json) ?? [];
    }

    private void Save(List<GlobalTabSetting> tabs) =>
        store.Set(StoreKey, JsonSerializer.Serialize(tabs));
}
