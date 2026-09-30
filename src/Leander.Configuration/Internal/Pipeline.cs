namespace Leander.Configuration.Internal;

internal static class Pipeline
{
    // Every error from a primitive becomes a diagnostic under the key.
    public static void Report(ReadContext context, ConfigurationDefinition definition, string key, IReadOnlyList<string> errors)
    {
        foreach (var error in errors)
        {
            context.Report(DiagnosticSeverity.Error, key, error, definition);
        }
    }
}
