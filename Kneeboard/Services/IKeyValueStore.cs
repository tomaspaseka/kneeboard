namespace Kneeboard.Services;

public interface IKeyValueStore
{
    string? Get(string key);
    void Set(string key, string value);
}
