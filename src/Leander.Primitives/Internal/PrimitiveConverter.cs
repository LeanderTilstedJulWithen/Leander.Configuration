using Leander.Primitives.Parsing;

namespace Leander.Primitives.Internal;

// A primitive as a converter: parsing applies all of the primitive's rules, formatting uses its converter.
// Its errors come back unformatted, so the outer primitive decides whether the value is hidden.
internal sealed class PrimitiveConverter<T>(Primitive<T> primitive) : IConverter<T>
{
    private readonly Primitive<T> _primitive = primitive;

    // Only the name: the primitive's format and rules are documented with the primitive itself.
    public string Description => $"{_primitive.DisplayName}.";

    public string Format(T value) => _primitive.Converter.Format(value);

    public bool TryParse(string input, out T result) => _primitive.TryParse(input, out result);

    public bool TryParse(string input, out T result, out IReadOnlyList<IFormattableText<string>> errors)
    {
        var list = new List<ErrorText>();
        var success = _primitive.TryParse(input, out result, list);
        errors = list;
        return success;
    }
}
