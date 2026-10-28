using Leander.Primitives;

namespace Leander.Configuration;

/// <summary>
/// A part of the configuration under one key, e.g. <c>Server</c>. Defines values with keys under it:
/// <c>Define("Port", …)</c> gives a definition with the key <c>Server:Port</c>.
/// </summary>
/// <remarks>
/// A section only builds keys: the definitions it creates are plain definitions with absolute keys,
/// the same as those created with <see cref="ConfigurationDefinition.Define{T}"/>.
/// </remarks>
public sealed class SectionDefinition(string key)
{
    /// <summary>
    /// The section's key, with sections separated by <c>:</c>, e.g. <c>Database:Primary</c>.
    /// </summary>
    public string Key { get; } = key;

    /// <summary>
    /// Defines a value at <paramref name="name"/> under the section, read with <paramref name="primitive"/>.
    /// See <see cref="ConfigurationDefinition.Define{T}"/>.
    /// </summary>
    public ConfigurationDefinition<T> Define<T>(string name, Primitive<T> primitive) =>
        ConfigurationDefinition.Define(KeyOf(name), primitive);

    /// <summary>
    /// Defines a list at <paramref name="name"/> under the section, with one entry per item.
    /// See <see cref="ConfigurationDefinition.Indexed{T}"/>.
    /// </summary>
    public ConfigurationDefinition<IReadOnlyList<T>> Indexed<T>(string name, ListPrimitive<T> list) =>
        ConfigurationDefinition.Indexed(KeyOf(name), list);

    /// <summary>
    /// Returns the section at <paramref name="name"/> under this one, e.g. <c>Primary</c> under <c>Database</c>
    /// is <c>Database:Primary</c>.
    /// </summary>
    public SectionDefinition Section(string name) => new(KeyOf(name));

    private string KeyOf(string name) => $"{Key}{ValueSource.KeySeparator}{name}";
}
