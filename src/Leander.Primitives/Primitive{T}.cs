using Leander.Primitives.Internal;
using Leander.Primitives.Normalization;
using Leander.Primitives.Parsing;
using Leander.Primitives.Validation;

namespace Leander.Primitives;

// A complete primitive: the name and converter are always present. Immutable, and the constructors never throw.
// A derived primitive takes its base's converter, and its rules are added to the base's: the base's rules run first.
public sealed class Primitive<T> : Primitive
{
    private IReadOnlyList<INormalizer<T>>? _allNormalizers;
    private IReadOnlyList<IValidator<T>>? _allValidators;

    public Primitive(string name, IConverter<T> converter)
        : base(name)
    {
        Converter = converter;
    }

    public Primitive(string name, Primitive<T> @base)
        : base(name)
    {
        Converter = @base.Converter;
        Base = @base;
    }

    public override Type ValueType => typeof(T);

    public override Primitive<T>? Base { get; }

    public IConverter<T> Converter { get; }

    // Its own normalizers; a base's normalizers run before these.
    public IReadOnlyList<INormalizer<T>> Normalizers { get; init; } = [];

    // Its own validators; a base's validators run before these.
    public IReadOnlyList<IValidator<T>> Validators { get; init; } = [];

    // The base's rules followed by its own. Computed on first use, because init properties are set after the constructor.
    private IReadOnlyList<INormalizer<T>> AllNormalizers =>
        _allNormalizers ??= Base is null ? Normalizers : [.. Base.AllNormalizers, .. Normalizers];

    private IReadOnlyList<IValidator<T>> AllValidators =>
        _allValidators ??= Base is null ? Validators : [.. Base.AllValidators, .. Validators];

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
    public bool TryAccept(T value, out T result) => Rules.TryApply(AllNormalizers, AllValidators, value, out result, null);

    public bool TryAccept(T value, out T result, out IReadOnlyList<string> errors) =>
        TryAccept(value, redact: false, out result, out errors);

    internal bool TryAccept(T value, bool redact, out T result, out IReadOnlyList<string> errors)
    {
        var list = new List<string>();
        var success = Rules.TryApply(AllNormalizers, AllValidators, value, out result, list, redact);
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

        return Rules.TryApply(AllNormalizers, AllValidators, parsed, out value, errors, redact);
    }
}
