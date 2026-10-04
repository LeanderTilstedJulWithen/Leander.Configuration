using Leander.Primitives.Internal;
using Leander.Primitives.Normalization;
using Leander.Primitives.Parsing;
using Leander.Primitives.Validation;

namespace Leander.Primitives;

/// <summary>
/// A named kind of value of <typeparamref name="T"/>: a converter, then normalizers, then validators.
/// </summary>
/// <remarks>
/// <para>The name and converter are always present. Immutable, and the constructors never throw.</para>
/// <para>
/// A derived primitive takes its base's converter, and its rules are added to the base's: the base's rules run first.
/// </para>
/// </remarks>
// Not sealed, for ListPrimitive<T>. The hooks are private protected, so a subclass outside the library can't change how values are read.
public class Primitive<T> : Primitive
{
    private IReadOnlyList<INormalizer<T>>? _allNormalizers;
    private IReadOnlyList<IValidator<T>>? _allValidators;

    /// <summary>
    /// Creates a primitive that reads values with <paramref name="converter"/>.
    /// </summary>
    public Primitive(string name, IConverter<T> converter)
        : base(name)
    {
        Converter = converter;
    }

    /// <summary>
    /// Derives a primitive from <paramref name="base"/>: it keeps the base's converter and rules, and adds its own.
    /// </summary>
    public Primitive(string name, Primitive<T> @base)
        : base(name)
    {
        Converter = @base.Converter;
        Base = @base;
    }

    /// <inheritdoc/>
    public override Primitive<T>? Base { get; }

    /// <summary>
    /// Reads values from strings, and formats them, e.g. bounds in rule descriptions.
    /// </summary>
    public IConverter<T> Converter { get; }

    /// <summary>
    /// Its own normalizers. A base's normalizers run before these.
    /// </summary>
    public IReadOnlyList<INormalizer<T>> Normalizers { get; init; } = [];

    /// <summary>
    /// Its own validators. A base's validators run before these.
    /// </summary>
    public IReadOnlyList<IValidator<T>> Validators { get; init; } = [];

    /// <inheritdoc/>
    public override Type ValueType => typeof(T);

    internal override string? ConverterDescription => Converter.Description;

    // Bounds are formatted with the converter, e.g. "must be less than or equal to 0xFF" for a Hex primitive.
    internal override IReadOnlyList<string> NormalizerDescriptions =>
        [.. Normalizers.Select(normalizer => normalizer.Description.FormatWith(Converter))];

    internal override IReadOnlyList<string> ValidatorDescriptions =>
        [.. Validators.Select(validator => validator.Description.FormatWith(Converter))];

    // The base's rules followed by its own. Computed on first use, because init properties are set after the constructor.
    private IReadOnlyList<INormalizer<T>> AllNormalizers =>
        _allNormalizers ??= Base is null ? Normalizers : [.. Base.AllNormalizers, .. Normalizers];

    private IReadOnlyList<IValidator<T>> AllValidators =>
        _allValidators ??= Base is null ? Validators : [.. Base.AllValidators, .. Validators];

    /// <summary>
    /// Normalizes, then validates a value that is already a <typeparamref name="T"/>, e.g. a default.
    /// On failure, <paramref name="result"/> is <see langword="default"/>.
    /// </summary>
    public bool TryAccept(T value, out T result) => TryAccept(value, out result, null, redact: false);

    /// <summary>
    /// Normalizes, then validates a value that is already a <typeparamref name="T"/>, e.g. a default.
    /// On failure, <paramref name="result"/> is <see langword="default"/> and <paramref name="errors"/> lists every failure.
    /// </summary>
    public bool TryAccept(T value, out T result, out IReadOnlyList<string> errors) =>
        TryAccept(value, redact: false, out result, out errors);

    /// <summary>
    /// Parses <paramref name="input"/> with the converter, then normalizes and validates the value.
    /// On failure, <paramref name="value"/> is <see langword="default"/>.
    /// </summary>
    public bool TryParse(string input, out T value) => TryParse(input, out value, null, redact: false);

    /// <summary>
    /// Parses <paramref name="input"/> with the converter, then normalizes and validates the value.
    /// On failure, <paramref name="value"/> is <see langword="default"/> and <paramref name="errors"/> lists every failure.
    /// </summary>
    public bool TryParse(string input, out T value, out IReadOnlyList<string> errors) =>
        TryParse(input, redact: false, out value, out errors);

    internal bool TryAccept(T value, bool redact, out T result, out IReadOnlyList<string> errors)
    {
        var list = new List<string>();
        var success = TryAccept(value, out result, list, redact);
        errors = list;
        return success;
    }

    internal bool TryAccept(T value, out T result, List<string>? errors, bool redact)
    {
        if (!TryAcceptItems(value, out var accepted, errors, redact))
        {
            result = default!;
            return false;
        }

        return TryApplyRules(accepted, out result, errors, redact);
    }

    // Only this primitive's rules (the base's, then its own), not its items'. For a list whose items were accepted one by one.
    internal bool TryApplyRules(T value, out T result, List<string>? errors, bool redact) =>
        Rules.TryApply(AllNormalizers, AllValidators, Converter, value, out result, errors, redact);

    // With redact, error messages leave out the value, for sensitive configuration values.
    internal bool TryParse(string input, bool redact, out T value, out IReadOnlyList<string> errors)
    {
        var list = new List<string>();
        var success = TryParse(input, out value, list, redact);
        errors = list;
        return success;
    }

    // Without an error list, the first failure stops.
    internal bool TryParse(string input, out T value, List<string>? errors, bool redact)
    {
        if (!TryConvert(input, out var converted, errors, redact))
        {
            value = default!;
            return false;
        }

        return TryApplyRules(converted, out value, errors, redact);
    }

    // Accepts the items of a value made of items, before this primitive's rules run. A plain value has none.
    private protected virtual bool TryAcceptItems(T value, out T result, List<string>? errors, bool redact)
    {
        result = value;
        return true;
    }

    // Turns the input into a T, before this primitive's rules run. Without an error list, the converter isn't asked for reasons.
    // Each reason follows the primitive's own line; pieces of the input in it are hidden with redact.
    private protected virtual bool TryConvert(string input, out T value, List<string>? errors, bool redact)
    {
        if (errors is null)
        {
            return Converter.TryParse(input, out value);
        }

        if (Converter.TryParse(input, out value, out var reasons))
        {
            return true;
        }

        var lead = redact ? $"value is not a valid {DisplayName}" : $"'{input}' is not a valid {DisplayName}";
        if (reasons.Count == 0)
        {
            errors.Add(lead);
            return false;
        }

        IFormatter<string> formatter = redact ? RedactingFormatter<string>.Instance : InvariantFormatter<string>.Instance;
        errors.AddRange(reasons.Select(reason => $"{lead}: {reason.FormatWith(formatter)}"));
        return false;
    }
}
