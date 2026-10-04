namespace Leander.Configuration;

/// <summary>
/// Where a contract reads its values from: string values under keys with sections separated by
/// <see cref="ValueSource.KeySeparator"/>. Create one with <see cref="ValueSource"/>.
/// </summary>
public interface IValueSource
{
    /// <summary>
    /// Returns the names of the direct children of <paramref name="key"/>,
    /// e.g. "0" and "1" for <c>Key:0</c> and <c>Key:1</c>; each name once.
    /// </summary>
    public IReadOnlyList<string> GetChildNames(string key);

    /// <summary>
    /// Returns the value of <paramref name="key"/>, or <see langword="null"/> when the key has no value.
    /// </summary>
    public string? GetValue(string key);
}
