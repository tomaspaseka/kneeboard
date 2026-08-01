namespace Kneeboard.Models;

public record RecentDocument(string Path, string Title, DateTimeOffset LastOpenedUtc);
