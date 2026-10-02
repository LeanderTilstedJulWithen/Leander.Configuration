using Leander.Primitives.Parsing;

namespace Leander.Primitives.Internal;

// A primitive as a converter: parsing applies all of the primitive's rules, formatting uses its converter.
internal sealed class PrimitiveConverter<T>(Primitive<T> primitive) : IConverter<T>
{
    private readonly Primitive<T> _primitive = primitive;

    // Only the name: the primitive's format and rules are documented with the primitive itself.
    public string Description => $"{_primitive.DisplayName}.";

    public bool TryParse(string input, out T result) => _primitive.TryParse(input, out result);

    public string Format(T value) => _primitive.Converter.Format(value);
}
