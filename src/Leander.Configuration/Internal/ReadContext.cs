namespace Leander.Configuration.Internal;

// The state of one read of a contract from a source: diagnostics are collected instead of thrown.
internal sealed class ReadContext(ConfigurationContract contract, IValueSource source, ReadOptions options)
{
    private readonly List<ConfigurationDiagnostic> _diagnostics = [];

    public ConfigurationContract Contract { get; } = contract;

    public IReadOnlyList<ConfigurationDiagnostic> Diagnostics => _diagnostics;

    public bool HasErrors { get; private set; }

    // Whether the contract definition being read is sensitive; its values are left out of diagnostics.
    // Set per contract definition, because the readers it wraps (e.g. for Optional()) don't know.
    public bool IsSensitive { get; set; }

    public ReadOptions Options { get; } = options;

    public IValueSource Source { get; } = source;

    // Definition is null for a key the contract doesn't define.
    public void Report(DiagnosticSeverity severity, string key, string message, ConfigurationDefinition? definition)
    {
        if (severity == DiagnosticSeverity.Warning && Options.WarningsAsErrors)
        {
            severity = DiagnosticSeverity.Error;
        }

        HasErrors |= severity == DiagnosticSeverity.Error;
        _diagnostics.Add(new ConfigurationDiagnostic(severity, key, message, definition));
    }
}
