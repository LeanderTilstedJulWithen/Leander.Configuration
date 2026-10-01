namespace Leander.Configuration;

/// <summary>
/// How serious a <see cref="ConfigurationDiagnostic"/> is.
/// </summary>
public enum DiagnosticSeverity
{
    /// <summary>
    /// Detail about how a value was read. Never affects the read.
    /// </summary>
    Trace,

    /// <summary>
    /// Something likely wrong that doesn't prevent a snapshot, unless <see cref="ReadOptions.WarningsAsErrors"/> is set.
    /// </summary>
    Warning,

    /// <summary>
    /// The source doesn't satisfy the contract. Prevents a snapshot.
    /// </summary>
    Error,
}
