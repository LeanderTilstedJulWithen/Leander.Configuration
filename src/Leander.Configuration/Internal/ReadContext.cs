namespace Leander.Configuration.Internal;

// The state of one read of a contract from a source: diagnostics are collected instead of thrown.
internal sealed class ReadContext(ConfigurationContract contract, IValueSource source)
{
    private readonly List<ConfigurationDiagnostic> _diagnostics = [];

    public ConfigurationContract Contract { get; } = contract;

    public IValueSource Source { get; } = source;

    public IReadOnlyList<ConfigurationDiagnostic> Diagnostics => _diagnostics;

    // Whether the contract definition being read is sensitive; its values are left out of diagnostics.
    // Set per contract definition, because its elements don't know: .Indexed().Sensitive() leaves the element as it was.
    public bool IsSensitive { get; set; }

    public void Report(DiagnosticSeverity severity, string key, string message, ConfigurationDefinition definition) =>
        _diagnostics.Add(new ConfigurationDiagnostic(severity, key, message, definition));
}
