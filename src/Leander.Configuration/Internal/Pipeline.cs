using Leander.Primitives.Internal;
using Leander.Primitives.Normalization;
using Leander.Primitives.Validation;

namespace Leander.Configuration.Internal;

internal static class Pipeline
{
    // Normalizes, then validates definition-level rules. Every failure becomes a diagnostic.
    public static bool TryProcess<T>(
        ReadContext context,
        ConfigurationDefinition definition,
        string key,
        IReadOnlyList<INormalizer<T>> normalizers,
        IReadOnlyList<IValidator<T>> validators,
        T value,
        out T result)
    {
        var errors = new List<string>();
        var success = Rules.TryApply(normalizers, validators, value, out result, errors);
        Report(context, definition, key, errors);
        return success;
    }

    public static void Report(ReadContext context, ConfigurationDefinition definition, string key, IReadOnlyList<string> errors)
    {
        foreach (var error in errors)
        {
            context.Report(DiagnosticSeverity.Error, key, error, definition);
        }
    }
}
