using Leander.Primitives.Normalization;
using Leander.Primitives.Parsing;
using Leander.Primitives.Validation;

namespace Leander.Primitives;

// Definitions are immutable: every builder method returns a new definition, and none of them throw.
// Only the rules are described here; resolving and running them is the job of an evaluator.
// A null converter means "not specified": the evaluator uses the default for T.
// Normalizers and validators are added to the defaults for T, never replace them.
public sealed class PrimitiveDefinition<T> : PrimitiveDefinition
{
    private readonly Settings _settings;

    private PrimitiveDefinition(Settings settings)
    {
        _settings = settings;
    }

    public override string? Name => _settings.Name;

    public override Type ValueType => typeof(T);

    public override string? Description => _settings.Description;

    public IConverter<T>? Converter => _settings.Converter;

    public IReadOnlyList<INormalizer<T>> Normalizers => _settings.Normalizers;

    public IReadOnlyList<IValidator<T>> Validators => _settings.Validators;

    internal static PrimitiveDefinition<T> Create(string? name, IConverter<T>? converter) =>
        new(new Settings(name) { Converter = converter });

    public PrimitiveDefinition<T> Describe(string description) =>
        new(_settings with { Description = description });

    public PrimitiveDefinition<T> Normalize(INormalizer<T> normalizer) =>
        new(_settings with { Normalizers = [.. _settings.Normalizers, normalizer] });

    public PrimitiveDefinition<T> Validate(IValidator<T> validator) =>
        new(_settings with { Validators = [.. _settings.Validators, validator] });

    internal override Primitive? ResolveIn(PrimitiveRegistry? registry)
    {
        Primitive<T>? defaultPrimitive = null;
        registry?.TryGet(null, out defaultPrimitive);
        return Resolve(defaultPrimitive);
    }

    // The converter replaces the default's; normalizers and validators are appended to the default's.
    internal Primitive<T>? Resolve(Primitive<T>? defaultPrimitive)
    {
        var converter = Converter ?? defaultPrimitive?.Converter;
        if (converter is null)
        {
            return null;
        }

        return new Primitive<T>(
            Name,
            Description,
            converter,
            [.. defaultPrimitive?.Normalizers ?? [], .. Normalizers],
            [.. defaultPrimitive?.Validators ?? [], .. Validators]);
    }

    private sealed record Settings(string? Name)
    {
        public string? Description { get; init; }

        public IConverter<T>? Converter { get; init; }

        public IReadOnlyList<INormalizer<T>> Normalizers { get; init; } = [];

        public IReadOnlyList<IValidator<T>> Validators { get; init; } = [];
    }
}
