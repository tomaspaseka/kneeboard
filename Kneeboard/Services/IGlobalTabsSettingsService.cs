using Kneeboard.Models;

namespace Kneeboard.Services;

public interface IGlobalTabsSettingsService
{
    /// <summary>Returns the configured Global Tabs, ordered by <see cref="GlobalTabSetting.Order"/>.</summary>
    Task<IReadOnlyList<GlobalTabSetting>> GetTabsAsync();

    /// <summary>
    /// Adds a new Global Tab, appended after the existing entries. Throws
    /// <see cref="InvalidOperationException"/> if there are already 3 entries (the cap).
    /// </summary>
    Task<GlobalTabSetting> AddAsync(string label, ContentSource source);

    /// <summary>Updates the label and source of the entry with the given id. No-op if not found.</summary>
    Task EditAsync(string id, string label, ContentSource source);

    /// <summary>Removes the entry with the given id. No-op if not found.</summary>
    Task RemoveAsync(string id);

    /// <summary>
    /// Moves the entry with the given id to <paramref name="newOrder"/> (0-based), shifting the
    /// others to keep order contiguous. <paramref name="newOrder"/> is clamped to the valid range.
    /// No-op if the id isn't found.
    /// </summary>
    Task ReorderAsync(string id, int newOrder);

    /// <summary>Sets the enabled flag of the entry with the given id. No-op if not found.</summary>
    Task SetEnabledAsync(string id, bool enabled);
}
