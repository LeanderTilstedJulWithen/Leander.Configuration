namespace Leander.Configuration;

public interface IValueSource
{
    // Returns null when the key has no value.
    public string? GetValue(string key);

    // Returns the names of the direct children of the key, e.g. "0" and "1" for "Key:0" and "Key:1".
    public IReadOnlyList<string> GetChildNames(string key);
}
