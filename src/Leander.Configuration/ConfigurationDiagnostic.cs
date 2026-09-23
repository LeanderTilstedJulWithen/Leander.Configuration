namespace Leander.Configuration;

public sealed record ConfigurationDiagnostic(
    DiagnosticSeverity Severity,
    string Key,
    string Message,
    ConfigurationDefinition Definition)
{
    public override string ToString() => $"{Severity}: {Key}: {Message}";
}
