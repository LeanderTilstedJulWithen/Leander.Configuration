namespace Leander.Configuration;

// Reads definitions of a contract from a source and collects diagnostics instead of throwing.
// Values of definitions that failed are default(T); call ThrowIfInvalid before using them.
public sealed class ConfigurationReader(ConfigurationContract contract, IValueSource source)
{
    private readonly List<ConfigurationDiagnostic> _diagnostics = [];

    public ConfigurationContract Contract { get; } = contract;

    public IValueSource Source { get; } = source;

    public IReadOnlyList<ConfigurationDiagnostic> Diagnostics => _diagnostics;

    public bool HasErrors => _diagnostics.Exists(d => d.Severity == DiagnosticSeverity.Error);

    public T Get<T>(ConfigurationDefinition<T> definition)
    {
        TryGet(definition, out var value);
        return value;
    }

    public bool TryGet<T>(ConfigurationDefinition<T> definition, out T value)
    {
        if (!Contract.Contains(definition))
        {
            Report(DiagnosticSeverity.Error, definition.Key, "definition is not part of the configuration contract", definition);
            value = default!;
            return false;
        }

        return definition.TryRead(this, definition.Key, out value);
    }

    public void ThrowIfInvalid()
    {
        if (HasErrors)
        {
            throw new InvalidConfigurationException([.. _diagnostics]);
        }
    }

    internal void Report(DiagnosticSeverity severity, string key, string message, ConfigurationDefinition definition) =>
        _diagnostics.Add(new ConfigurationDiagnostic(severity, key, message, definition));
}
