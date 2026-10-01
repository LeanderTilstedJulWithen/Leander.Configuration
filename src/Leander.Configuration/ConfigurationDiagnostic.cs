namespace Leander.Configuration;

// Definition is null for a key the contract doesn't define (see ReadOptions.CheckedSections).
public sealed record ConfigurationDiagnostic(
    DiagnosticSeverity Severity,
    string Key,
    string Message,
    ConfigurationDefinition? Definition)
{
    public override string ToString() => $"{Severity}: {Key}: {Message}";
}
