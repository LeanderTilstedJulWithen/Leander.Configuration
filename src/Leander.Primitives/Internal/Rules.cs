using Leander.Primitives.Normalization;
using Leander.Primitives.Validation;

namespace Leander.Primitives.Internal;

internal static class Rules
{
    // Normalizes, then validates. A failing normalizer stops processing.
    // With an error list, all validators run and every failure is collected; without one, the first failure stops.
    // With redact, exception messages are left out, because they may contain the value.
    public static bool TryApply<T>(
        IReadOnlyList<INormalizer<T>> normalizers,
        IReadOnlyList<IValidator<T>> validators,
        T value,
        out T result,
        List<string>? errors,
        bool redact = false)
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
                errors?.Add(redact
                    ? $"normalizer '{normalizer.Description}' failed"
                    : $"normalizer '{normalizer.Description}' failed: {exception.Message}");
                result = default!;
                return false;
            }
        }

        var isValid = true;
        foreach (var validator in validators)
        {
            string? message;
            try
            {
                message = validator.Validate(result);
            }
            catch (Exception exception)
            {
                message = redact
                    ? $"validator '{validator.Description}' failed"
                    : $"validator '{validator.Description}' failed: {exception.Message}";
            }

            if (message is null)
            {
                continue;
            }

            isValid = false;
            if (errors is null)
            {
                break;
            }

            errors.Add(message);
        }

        if (!isValid)
        {
            result = default!;
        }

        return isValid;
    }
}
