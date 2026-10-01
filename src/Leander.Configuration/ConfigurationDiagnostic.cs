namespace Leander.Configuration;

/// <summary>
/// Something found while reading a contract: an error, a warning or a trace.
/// </summary>
/// <param name="Severity">How serious it is.</param>
/// <param name="Key">The key it is about.</param>
/// <param name="Message">What was found, e.g. "must be between 1 and 65535". Leaves out values of sensitive definitions.</param>
/// <param name="Definition">
/// The definition it is about; <see langword="null"/> for a key the contract doesn't define
/// (see <see cref="ReadOptions.CheckedSections"/>).
/// </param>
public sealed record ConfigurationDiagnostic(
    DiagnosticSeverity Severity,
    string Key,
    string Message,
    ConfigurationDefinition? Definition)
{
    /// <summary>
    /// Formats the diagnostic as <c>Severity: Key: Message</c>.
    /// </summary>
    public override string ToString() => $"{Severity}: {Key}: {Message}";
}
