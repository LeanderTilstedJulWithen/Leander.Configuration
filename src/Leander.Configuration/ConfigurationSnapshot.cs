namespace Leander.Configuration;

// The values of every definition in a contract, read from a source. A snapshot only exists when the source satisfies
// the contract: every value is present or defaulted, parsed, normalized and valid. Reading a value cannot fail.
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

    public ConfigurationContract Contract { get; }

    // Warnings and traces produced while reading. Never contains errors.
    public IReadOnlyList<ConfigurationDiagnostic> Diagnostics { get; }

    // Throws if the definition is not part of the contract.
    public T Get<T>(ConfigurationDefinition<T> definition) =>
        _values.TryGetValue(definition, out var value)
            ? (T)value!
            : throw new ArgumentException($"{definition.Key}: definition is not part of the configuration contract.", nameof(definition));
}
