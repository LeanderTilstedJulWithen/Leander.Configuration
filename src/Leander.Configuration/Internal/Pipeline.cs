using Leander.Primitives.Normalization;
using Leander.Primitives.Validation;

namespace Leander.Configuration.Internal;

internal static class Pipeline
{
    // Normalizes, then validates. All validators run; every failure becomes a diagnostic.
    public static bool TryProcess<T>(
        ReadContext context,
        ConfigurationDefinition definition,
        string key,
        IReadOnlyList<INormalizer<T>> normalizers,
        IReadOnlyList<IValidator<T>> validators,
        T value,
        out T result)
    {
        result = value;

        foreach (var normalizer in normalizers)
        {
            try
            {
                result = normalizer.Normalize(result);
            }
            catch (Exception exception)
            {
                context.Report(DiagnosticSeverity.Error, key, $"normalizer '{normalizer.Description}' failed: {exception.Message}", definition);
                result = default!;
                return false;
            }
        }

        var isValid = true;
        foreach (var validator in validators)
        {
            try
            {
                if (validator.Validate(result) is { } message)
                {
                    context.Report(DiagnosticSeverity.Error, key, message, definition);
                    isValid = false;
                }
            }
            catch (Exception exception)
            {
                context.Report(DiagnosticSeverity.Error, key, $"validator '{validator.Description}' failed: {exception.Message}", definition);
                isValid = false;
            }
        }

        return isValid;
    }
}
