namespace Leander.Configuration;

/// <summary>
/// The values of a snapshot under one key, e.g. <c>Server</c>. Get one with <see cref="ConfigurationSnapshot.GetSection"/>.
/// </summary>
/// <remarks>
/// A section holds its own copy of its values and diagnostics, not the snapshot it came from.
/// Getting a value of a definition in the section cannot fail.
/// </remarks>
public sealed class SnapshotSection
{
    private readonly IReadOnlyDictionary<ConfigurationDefinition, object?> _values;

    private SnapshotSection(
        ContractSection contract,
        IReadOnlyDictionary<ConfigurationDefinition, object?> values,
        IReadOnlyList<ConfigurationDiagnostic> diagnostics)
    {
        Contract = contract;
        _values = values;
        Diagnostics = diagnostics;
    }

    /// <summary>
    /// The part of the contract the section holds the values of.
    /// </summary>
    public ContractSection Contract { get; }

    /// <summary>
    /// Warnings and traces under the section's key, including keys the contract doesn't define. Never contains errors.
    /// </summary>
    public IReadOnlyList<ConfigurationDiagnostic> Diagnostics { get; }

    /// <summary>
    /// The section's absolute key, e.g. <c>Server:Tls</c>.
    /// </summary>
    public string Key => Contract.Key;

    /// <summary>
    /// Gets the value of <paramref name="definition"/>.
    /// </summary>
    /// <exception cref="ArgumentException">The definition is not part of the section.</exception>
    public T Get<T>(ConfigurationDefinition<T> definition) =>
        _values.TryGetValue(definition, out var value)
            ? (T)value!
            : throw new ArgumentException($"{definition.Key}: definition is not part of the section {Key}.", nameof(definition));

    /// <summary>
    /// Returns the values under <paramref name="name"/> in this section, e.g. <c>Tls</c> under <c>Server</c> is <c>Server:Tls</c>.
    /// </summary>
    public SnapshotSection GetSection(string name) => Create(Contract.GetSection(name), _values, Diagnostics);

    // Copies the values of the section's definitions, and the diagnostics under its key, by key rather than by definition:
    // an unknown key has none.
    internal static SnapshotSection Create(
        ContractSection contract,
        IReadOnlyDictionary<ConfigurationDefinition, object?> values,
        IReadOnlyList<ConfigurationDiagnostic> diagnostics)
    {
        var prefix = contract.Key + ValueSource.KeySeparator;

        return new SnapshotSection(
            contract,
            contract.Definitions.ToDictionary(definition => definition, definition => values[definition]),
            diagnostics.Where(diagnostic => diagnostic.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList());
    }
}
