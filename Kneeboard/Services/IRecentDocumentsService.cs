using Kneeboard.Models;

namespace Kneeboard.Services;

public interface IRecentDocumentsService
{
    /// <summary>Returns up to 4 recently-opened documents, most-recently-opened first.</summary>
    Task<IReadOnlyList<RecentDocument>> GetRecentAsync();

    /// <summary>
    /// Records that a document was opened. If <paramref name="doc"/>'s path already exists in the
    /// list, the existing entry is replaced and moved to the front; otherwise it's inserted at the
    /// front. The list is capped at 4 entries, evicting the oldest as needed.
    /// </summary>
    Task RecordOpenedAsync(RecentDocument doc);

    /// <summary>Removes the entry with the given path, if present. No-op otherwise.</summary>
    Task RemoveAsync(string path);
}
