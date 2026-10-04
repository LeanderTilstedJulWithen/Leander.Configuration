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

    private sealed class RangeConverter(Primitive<T> bound) : IConverter<Range<T>>
    {
        private const string Separator = "..";

        private readonly Primitive<T> _bound = bound;

        public string Description => $"Two {_bound.Name} values separated by {Separator}, the smallest first.";

        public string Format(Range<T> value) =>
            $"{_bound.Converter.Format(value.Minimum)}{Separator}{_bound.Converter.Format(value.Maximum)}";

        // Both ends must be valid values of the bound, and in order.
        public bool TryParse(string input, out Range<T> result)
        {
            result = default;
            var separator = input.IndexOf(Separator, StringComparison.Ordinal);
            if (separator < 0 ||
                !_bound.TryParse(input[..separator].Trim(), out var minimum) ||
                !_bound.TryParse(input[(separator + Separator.Length)..].Trim(), out var maximum) ||
                minimum.CompareTo(maximum) > 0)
            {
                return false;
            }

            result = new Range<T>(minimum, maximum);
            return true;
        }
    }
}
