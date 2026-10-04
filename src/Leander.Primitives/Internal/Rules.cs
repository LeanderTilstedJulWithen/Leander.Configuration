using Leander.Primitives.Normalization;
using Leander.Primitives.Parsing;
using Leander.Primitives.Validation;

namespace Leander.Primitives.Internal;

internal static class Rules
{
    // Normalizes, then validates. A failing normalizer stops processing.
    // With an error list, all validators run and every failure is collected; without one, the first failure stops,
    // and validators are only asked IsValid.
    // Rule texts are formatted with the primitive's converter. In the hidden form, every value of T in them is hidden
    // and exception messages are left out, because they may contain the value.
    public static bool TryApply<T>(
        IReadOnlyList<INormalizer<T>> normalizers,
        IReadOnlyList<IValidator<T>> validators,
        IFormatter<T> converter,
        T value,
        out T result,
        List<ErrorText>? errors)
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
                errors?.Add(Failed("normalizer", normalizer.Description, converter, exception));
                result = default!;
                return false;
            }
        }

        var isValid = true;
        foreach (var validator in validators)
        {
            try
            {
                if (errors is null)
                {
                    if (!validator.IsValid(result))
                    {
                        isValid = false;
                        break;
                    }

                    continue;
                }

                // Formatted here, so an exception from a text is reported like one from the rule.
                var failures = validator.Validate(result)
                    .Select(failure => new ErrorText(failure.FormatWith(converter), failure.FormatWith(RedactingFormatter<T>.Instance)))
                    .ToList();
                isValid &= failures.Count == 0;
                errors.AddRange(failures);
            }
            catch (Exception exception)
            {
                isValid = false;
                if (errors is null)
                {
                    break;
                }

                errors.Add(Failed("validator", validator.Description, converter, exception));
            }
        }

        if (!isValid)
        {
            result = default!;
        }

        return isValid;
    }

    // A description that throws while formatting still gives a message.
    private static string Describe<T>(IFormattableText<T> description, IFormatter<T> formatter)
    {
        try
        {
            return description.FormatWith(formatter);
        }
        catch (Exception)
        {
            return "(description failed)";
        }
    }

    private static ErrorText Failed<T>(string kind, IFormattableText<T> description, IFormatter<T> converter, Exception exception) => new(
        $"{kind} '{Describe(description, converter)}' failed: {exception.Message}",
        $"{kind} '{Describe(description, RedactingFormatter<T>.Instance)}' failed");
}
