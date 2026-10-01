namespace Leander.Configuration;

/// <summary>
/// The values of every definition in a contract, read from a source.
/// </summary>
/// <remarks>
/// A snapshot only exists when the source satisfies the contract: every value is present or defaulted,
/// parsed, normalized and valid. Getting a value cannot fail.
/// </remarks>
public sealed class ConfigurationSnapshot
{
    private readonly IReadOnlyDictionary<ConfigurationDefinition, object?> _values;

    internal ConfigurationSnapshot(
        ConfigurationContract contract,
        IReadOnlyDictionary<ConfigurationDefinition, object?> values,
        IReadOnlyList<ConfigurationDiagnostic> diagnostics)
    {
        Contract = contract;
        _values = values;
        Diagnostics = diagnostics;
    }

    /// <summary>
    /// The contract the snapshot was read with.
    /// </summary>
    public ConfigurationContract Contract { get; }

    /// <summary>
    /// Warnings and traces produced while reading. Never contains errors.
    /// </summary>
    public IReadOnlyList<ConfigurationDiagnostic> Diagnostics { get; }

    /// <summary>
    /// Gets the value of <paramref name="definition"/>.
    /// </summary>
    /// <exception cref="ArgumentException">The definition is not part of the contract.</exception>
    public T Get<T>(ConfigurationDefinition<T> definition) =>
        _values.TryGetValue(definition, out var value)
            ? (T)value!
            : throw new ArgumentException($"{definition.Key}: definition is not part of the configuration contract.", nameof(definition));
}
