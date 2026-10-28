namespace Leander.Configuration;

/// <summary>
/// The definitions of a contract under one key, e.g. <c>Server</c>. Get one with <see cref="ConfigurationContract.GetSection"/>.
/// </summary>
/// <remarks>
/// A section describes a part of a contract; only the whole contract is read.
/// Definitions keep their absolute keys, e.g. <c>Server:Port</c>.
/// </remarks>
public sealed class ContractSection
{
    private readonly ConfigurationContract _contract;

    internal ContractSection(ConfigurationContract contract, string key)
    {
        _contract = contract;
        Key = key;

        var prefix = key + ValueSource.KeySeparator;
        Definitions = contract.Definitions
            .Where(definition => definition.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToList();
        OwnedSections = contract.OwnedSections
            .Where(section => section.Key.Equals(key, StringComparison.OrdinalIgnoreCase) ||
                              section.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// The definitions under the section, in the order they were registered. Empty when there are none.
    /// </summary>
    /// <remarks>
    /// A definition at the section's own key, e.g. <c>Server</c>, is not under it.
    /// </remarks>
    public IReadOnlyList<ConfigurationDefinition> Definitions { get; }

    /// <summary>
    /// The owned sections at the section's key or under it, in the order they were registered.
    /// See <see cref="ConfigurationContract.OwnedSections"/>.
    /// </summary>
    public IReadOnlyList<SectionDefinition> OwnedSections { get; }

    /// <summary>
    /// The section's absolute key, e.g. <c>Server:Tls</c>.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Returns the section at <paramref name="name"/> under this one, e.g. <c>Tls</c> under <c>Server</c> is <c>Server:Tls</c>.
    /// </summary>
    public ContractSection GetSection(string name) => new(_contract, $"{Key}{ValueSource.KeySeparator}{name}");
}
