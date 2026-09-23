using System.Text;

namespace Leander.Configuration;

public sealed class InvalidConfigurationException : Exception
{
    public InvalidConfigurationException(IReadOnlyList<ConfigurationDiagnostic> diagnostics)
        : base(FormatMessage(diagnostics))
    {
        Diagnostics = diagnostics;
    }

    // All diagnostics, including warnings and traces. The message lists errors only.
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
