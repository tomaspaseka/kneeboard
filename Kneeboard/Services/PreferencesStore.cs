namespace Kneeboard.Services;

/// <summary>
/// Thin wrapper over MAUI Essentials <see cref="Preferences"/>. Not unit tested — MAUI Essentials
/// statics cannot run in the test host (Kneeboard.Tests has &lt;UseMaui&gt;false&lt;/UseMaui&gt;).
/// </summary>
public class PreferencesStore : IKeyValueStore
{
    public string? Get(string key) => Preferences.Default.Get(key, (string?)null);

    public void Set(string key, string value) => Preferences.Default.Set(key, value);
}
