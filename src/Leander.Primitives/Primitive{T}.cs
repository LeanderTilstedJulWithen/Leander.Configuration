using Leander.Primitives.Normalization;
using Leander.Primitives.Parsing;
using Leander.Primitives.Validation;

namespace Leander.Primitives;

// A resolved primitive definition: the converter is always present, and the lists include the defaults for T.
public sealed class Primitive<T> : Primitive
{
    internal Primitive(
        string? name,
        string? description,
        IConverter<T> converter,
        IReadOnlyList<INormalizer<T>> normalizers,
        IReadOnlyList<IValidator<T>> validators)
    {
        Name = name;
        Description = description;
        Converter = converter;
        Normalizers = normalizers;
        Validators = validators;
    }

    public override string? Name { get; }

    public override Type ValueType => typeof(T);

    public override string? Description { get; }

    public IConverter<T> Converter { get; }

    public IReadOnlyList<INormalizer<T>> Normalizers { get; }

    public IReadOnlyList<IValidator<T>> Validators { get; }
}
