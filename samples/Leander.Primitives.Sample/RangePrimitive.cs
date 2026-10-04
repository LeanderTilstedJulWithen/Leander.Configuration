using Leander.Primitives.Parsing;

namespace Leander.Primitives.Sample;

// An inclusive range, written "1024..2048".
public readonly record struct Range<T>(T Minimum, T Maximum);

// A primitive of a new kind, like ListPrimitive<T>: it wraps the primitive each end goes through. Outside the
// library a primitive can't change how values are read, so the wrapping happens in its converter. The range's
// own rules, set with Validators, run after both ends are read.
public sealed class RangePrimitive<T>(string name, Primitive<T> bound)
    : Primitive<Range<T>>(name, new RangeConverter(bound))
    where T : IComparable<T>
{
    private readonly Primitive<T> _bound = bound;

    // The primitive each end goes through, with all its rules.
    public Primitive<T> Bound => _bound;

    // Holds the bound as a converter, so the bound's errors become the range's reasons: "maximum: must be between 1 and 65535".
    private sealed class RangeConverter(Primitive<T> bound) : IConverter<Range<T>>
    {
        private const string Separator = "..";

        private readonly IConverter<T> _bound = bound.AsConverter();
        private readonly string _boundName = bound.Name;

        public string Description => $"Two {_boundName} values separated by {Separator}, the smallest first.";

        public string Format(Range<T> value) => $"{_bound.Format(value.Minimum)}{Separator}{_bound.Format(value.Maximum)}";

        // Both ends must be valid values of the bound, and in order. Every reason is reported. Pieces of the input go
        // through the formatter, so the primitive can hide them for a sensitive value.
        public bool TryParse(string input, out Range<T> result, out IReadOnlyList<IFormattableText<string>> errors)
        {
            result = default;
            var separator = input.IndexOf(Separator, StringComparison.Ordinal);
            if (separator < 0)
            {
                errors = [FormattableText.Create<string>($"has no {Separator}")];
                return false;
            }

            var minimumText = input[..separator].Trim();
            var maximumText = input[(separator + Separator.Length)..].Trim();
            var reasons = new List<IFormattableText<string>>();

            var hasMinimum = _bound.TryParse(minimumText, out var minimum, out var minimumErrors);
            reasons.AddRange(minimumErrors.Select(error => Prefixed("minimum: ", error)));

            var hasMaximum = _bound.TryParse(maximumText, out var maximum, out var maximumErrors);
            reasons.AddRange(maximumErrors.Select(error => Prefixed("maximum: ", error)));

            if (hasMinimum && hasMaximum && minimum.CompareTo(maximum) > 0)
            {
                reasons.Add(FormattableText.Create<string>(formatter =>
                    $"{formatter.Format(minimumText)} is greater than {formatter.Format(maximumText)}"));
            }

            errors = reasons;
            if (reasons.Count > 0)
            {
                return false;
            }

            result = new Range<T>(minimum, maximum);
            return true;
        }

        private static IFormattableText<string> Prefixed(string prefix, IFormattableText<string> error) =>
            FormattableText.Create<string>(formatter => prefix + error.FormatWith(formatter));
    }
}
