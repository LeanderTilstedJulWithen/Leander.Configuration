using Leander.Configuration.Descriptors;
using Leander.Configuration.Internal;
using Leander.Primitives.Internal;
using Leander.Primitives.Normalization;
using Leander.Primitives.Validation;

namespace Leander.Configuration;

// Definitions are immutable: every builder method returns a new definition.
// Builder methods never throw; problems are reported when the contract is built or the definition is read.
// Value rules live on the primitive. Definition-level normalizers and validators exist only for lists (see ConfigurationDefinitionExtensions).
public sealed class ConfigurationDefinition<T> : ConfigurationDefinition
{
    private readonly Settings _settings;

    private ConfigurationDefinition(Settings settings)
    {
        _settings = settings;
    }

    public override string Key => _settings.Key;

    public override Type ValueType => typeof(T);

    public override string? Description => _settings.Description;

    public override bool HasDefault => _settings.HasDefault;

    public override bool IsOptional => _settings.IsOptional;

    public override bool IsSensitive => _settings.IsSensitive;

    internal ValueReader<T> Reader => _settings.Reader;

    internal static ConfigurationDefinition<T> Create(string key, ValueReader<T> reader) => new(new Settings(key, reader));

    public ConfigurationDefinition<T> Describe(string description) =>
        new(_settings with { Description = description });

    public ConfigurationDefinition<T> Sensitive() =>
        new(_settings with { IsSensitive = true });

    // Without a default, the value is required.
    public ConfigurationDefinition<T> Default(T value) =>
        new(_settings with { HasDefault = true, DefaultValue = value });

    // Everything configured so far applies to each element; everything configured afterwards applies to the list.
    public ConfigurationDefinition<IReadOnlyList<T>> Indexed() => ToList(new IndexedReader<T>(this));

    public ConfigurationDefinition<IReadOnlyList<T>> Delimited(char delimiter = ',') => ToList(new DelimitedReader<T>(this, delimiter));

    internal ConfigurationDefinition<T> AddNormalizer(INormalizer<T> normalizer) =>
        new(_settings with { Normalizers = [.. _settings.Normalizers, normalizer] });

    internal ConfigurationDefinition<T> AddValidator(IValidator<T> validator) =>
        new(_settings with { Validators = [.. _settings.Validators, validator] });

    private ConfigurationDefinition<IReadOnlyList<T>> ToList(ValueReader<IReadOnlyList<T>> reader) =>
        new(new ConfigurationDefinition<IReadOnlyList<T>>.Settings(Key, reader)
        {
            Description = Description,
            IsSensitive = IsSensitive,
        });

    // Used by Optional(). The reader turns T into TOptional, i.e. T?.
    internal ConfigurationDefinition<TOptional> ToOptional<TOptional>(ValueReader<TOptional> reader) =>
        new(new ConfigurationDefinition<TOptional>.Settings(Key, reader)
        {
            Description = Description,
            IsOptional = true,
            IsSensitive = IsSensitive,
        });

    internal override void Check(ContractChecker checker)
    {
        if (HasDefault && IsOptional)
        {
            checker.Failures.Add($"{Key}: Optional() cannot be combined with a default.");
        }
        else if (HasDefault && _settings.DefaultValue is null)
        {
            checker.Failures.Add($"{Key}: the default is null, but the definition is not optional.");
        }

        Reader.Check(checker, this);
    }

    internal override ValueDescriptor CreateValueDescriptor(DescriptorContext context)
    {
        var descriptor = Reader.Describe(context);

        return descriptor with
        {
            Type = TypeNames.Get(Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T)),
            Presence = IsOptional ? ValuePresence.Optional : HasDefault ? ValuePresence.Default : ValuePresence.Required,
            Default = HasDefault && !context.IsSensitive ? Reader.Format(context, _settings.DefaultValue!) : null,
            Normalizers = [.. descriptor.Normalizers, .. _settings.Normalizers.Select(normalizer => normalizer.Description)],
            Validators = [.. descriptor.Validators, .. _settings.Validators.Select(validator => validator.Description)],
        };
    }

    internal override bool TryRead(ReadContext context, out object? value)
    {
        var success = TryRead(context, Key, out var typed);
        value = typed;
        return success;
    }

    internal bool TryRead(ReadContext context, string key, out T value)
    {
        switch (Reader.Read(context, this, key, out var raw))
        {
            case ReadStatus.Read:
                return TryProcess(context, key, raw, out value);

            case ReadStatus.Missing when HasDefault:
                if (!Reader.TryProcess(context, this, key, _settings.DefaultValue!, out var defaultValue))
                {
                    value = default!;
                    return false;
                }

                return TryProcess(context, key, defaultValue, out value);

            // T is nullable, so default is null. The pipeline never sees it.
            case ReadStatus.Missing when IsOptional:
                value = default!;
                return true;

            case ReadStatus.Missing:
                context.Report(DiagnosticSeverity.Error, key, "value is required", this);
                value = default!;
                return false;

            default:
                value = default!;
                return false;
        }
    }

    // Applies definition-level rules.
    internal bool TryProcess(ReadContext context, string key, T value, out T result) =>
        Pipeline.TryProcess(context, this, key, _settings.Normalizers, _settings.Validators, value, out result);

    private sealed record Settings(string Key, ValueReader<T> Reader)
    {
        public string? Description { get; init; }

        public bool HasDefault { get; init; }

        public bool IsOptional { get; init; }

        public bool IsSensitive { get; init; }

        public T? DefaultValue { get; init; }

        public IReadOnlyList<INormalizer<T>> Normalizers { get; init; } = [];

        public IReadOnlyList<IValidator<T>> Validators { get; init; } = [];
    }
}
