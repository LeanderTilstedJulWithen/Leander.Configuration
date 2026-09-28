using Leander.Primitives.Internal;
using Leander.Primitives.Normalization;
using Leander.Primitives.Parsing;
using Leander.Primitives.Validation;

namespace Leander.Primitives;

// A complete primitive: the converter is always present.
// Immutable: Describe, Normalize and Validate return a new primitive with the same name and base, and never throw.
public sealed class Primitive<T> : Primitive
{
    internal Primitive(
        string? name,
        string? description,
        IConverter<T> converter,
        IReadOnlyList<INormalizer<T>> normalizers,
        IReadOnlyList<IValidator<T>> validators,
        Primitive<T>? @base = null)
    {
        Name = name;
        Description = description;
        Converter = converter;
        Normalizers = normalizers;
        Validators = validators;
        Base = @base;
    }

    public override string? Name { get; }

    public override Type ValueType => typeof(T);

    public override string? Description { get; }

    public override Primitive<T>? Base { get; }

    public IConverter<T> Converter { get; }

    public IReadOnlyList<INormalizer<T>> Normalizers { get; }

    public IReadOnlyList<IValidator<T>> Validators { get; }

    public Primitive<T> Describe(string description) =>
        new(Name, description, Converter, Normalizers, Validators, Base);

    public Primitive<T> Normalize(INormalizer<T> normalizer) =>
        new(Name, Description, Converter, [.. Normalizers, normalizer], Validators, Base);

    public Primitive<T> Validate(IValidator<T> validator) =>
        new(Name, Description, Converter, Normalizers, [.. Validators, validator], Base);

    // Parses, then accepts the parsed value (see TryAccept). On failure, value is default.
    public bool TryParse(string input, out T value) => TryParse(input, out value, null, redact: false);

    public bool TryParse(string input, out T value, out IReadOnlyList<string> errors) =>
        TryParse(input, redact: false, out value, out errors);

    // With redact, error messages leave out the value, for sensitive configuration values.
    internal bool TryParse(string input, bool redact, out T value, out IReadOnlyList<string> errors)
    {
        var list = new List<string>();
        var success = TryParse(input, out value, list, redact);
        errors = list;
        return success;
    }

    // Normalizes, then validates a value that is already a T, e.g. a default. On failure, result is default.
    public bool TryAccept(T value, out T result) => Rules.TryApply(Normalizers, Validators, value, out result, null);

    public bool TryAccept(T value, out T result, out IReadOnlyList<string> errors) =>
        TryAccept(value, redact: false, out result, out errors);

    internal bool TryAccept(T value, bool redact, out T result, out IReadOnlyList<string> errors)
    {
        var list = new List<string>();
        var success = Rules.TryApply(Normalizers, Validators, value, out result, list, redact);
        errors = list;
        return success;
    }

    private bool TryParse(string input, out T value, List<string>? errors, bool redact)
    {
        if (!Converter.TryParse(input, out var parsed))
        {
            errors?.Add(redact ? $"value is not a valid {DisplayName}" : $"'{input}' is not a valid {DisplayName}");
            value = default!;
            return false;
        }

        return Rules.TryApply(Normalizers, Validators, parsed, out value, errors, redact);
    }
}
