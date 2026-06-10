namespace Kneeboard.Models;

public class KneeboardDocument
{
    public string Title { get; set; } = string.Empty;
    public List<KneeboardSection> Sections { get; set; } = [];
}

public class KneeboardSection
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public ContentSource? Source { get; set; }
}
