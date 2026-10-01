using System.Text;

namespace Leander.Configuration;

/// <summary>
/// Thrown when a source doesn't satisfy a contract. The message lists every error.
/// </summary>
public sealed class InvalidConfigurationException : Exception
{
    /// <summary>
    /// Creates the exception, with a message listing the errors in <paramref name="diagnostics"/>.
    /// </summary>
    public InvalidConfigurationException(IReadOnlyList<ConfigurationDiagnostic> diagnostics)
        : base(FormatMessage(diagnostics))
    {
        Diagnostics = diagnostics;
    }

    /// <summary>
    /// All diagnostics, including warnings and traces. The message lists errors only.
    /// </summary>
    public IReadOnlyList<ConfigurationDiagnostic> Diagnostics { get; }

    private static string FormatMessage(IReadOnlyList<ConfigurationDiagnostic> diagnostics)
    {
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Count == 0)
        {
            return "Configuration is invalid.";
        }

        var width = errors.Max(e => e.Key.Length);
        var builder = new StringBuilder("Configuration is invalid:").AppendLine().AppendLine();

        foreach (var error in errors)
        {
            builder.Append("  ").Append(error.Key.PadRight(width)).Append("  ").AppendLine(error.Message);
        }

        return builder.ToString().TrimEnd();
    }
}
