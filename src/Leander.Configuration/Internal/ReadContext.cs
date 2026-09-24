namespace Leander.Configuration.Internal;

// The state of one read of a contract from a source: diagnostics are collected instead of thrown.
internal sealed class ReadContext(ConfigurationContract contract, IValueSource source)
{
    private readonly List<ConfigurationDiagnostic> _diagnostics = [];

    public ConfigurationContract Contract { get; } = contract;

    public IValueSource Source { get; } = source;

    public IReadOnlyList<ConfigurationDiagnostic> Diagnostics => _diagnostics;

    public void Report(DiagnosticSeverity severity, string key, string message, ConfigurationDefinition definition) =>
        _diagnostics.Add(new ConfigurationDiagnostic(severity, key, message, definition));
}
