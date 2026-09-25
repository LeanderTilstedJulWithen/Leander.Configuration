using Leander.Primitives.Internal;
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

    // Parses, then accepts the parsed value (see TryAccept). On failure, value is default.
    public bool TryParse(string input, out T value) => TryParse(input, out value, null);

    public bool TryParse(string input, out T value, out IReadOnlyList<string> errors)
    {
        var list = new List<string>();
        var success = TryParse(input, out value, list);
        errors = list;
        return success;
    }

    // Normalizes, then validates a value that is already a T, e.g. a default. On failure, result is default.
    public bool TryAccept(T value, out T result) => Rules.TryApply(Normalizers, Validators, value, out result, null);

    public bool TryAccept(T value, out T result, out IReadOnlyList<string> errors)
    {
        var list = new List<string>();
        var success = Rules.TryApply(Normalizers, Validators, value, out result, list);
        errors = list;
        return success;
    }

    private bool TryParse(string input, out T value, List<string>? errors)
    {
        if (!Converter.TryParse(input, out var parsed))
        {
            errors?.Add($"'{input}' is not a valid {DisplayName}");
            value = default!;
            return false;
        }

        return Rules.TryApply(Normalizers, Validators, parsed, out value, errors);
    }
}
