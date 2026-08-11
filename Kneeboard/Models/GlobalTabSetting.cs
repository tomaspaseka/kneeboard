namespace Kneeboard.Models;

/// <summary>
/// An app-owned Global Tab, configured once from Settings and shown in every document's tab bar.
/// Holds the same kind of <see cref="ContentSource"/> a <see cref="KneeboardSection"/> does.
/// </summary>
public record GlobalTabSetting(string Id, string Label, ContentSource Source, int Order, bool Enabled);
