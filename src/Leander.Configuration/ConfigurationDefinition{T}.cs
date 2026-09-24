using Leander.Configuration.Internal;
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

    internal ValueReader<T> Reader => _settings.Reader;

    internal static ConfigurationDefinition<T> Create(string key, ValueReader<T> reader) => new(new Settings(key, reader));

    public ConfigurationDefinition<T> Describe(string description) =>
        new(_settings with { Description = description });

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
        });

    internal override void Resolve(ContractResolver resolver) => Reader.Resolve(resolver, this);

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

            case ReadStatus.Missing:
                context.Report(DiagnosticSeverity.Error, key, "value is required", this);
                value = default!;
                return false;

            default:
                value = default!;
                return false;
        }
    }

    private bool TryProcess(ReadContext context, string key, T value, out T result) =>
        Pipeline.TryProcess(context, this, key, _settings.Normalizers, _settings.Validators, value, out result);

    private sealed record Settings(string Key, ValueReader<T> Reader)
    {
        public string? Description { get; init; }

        public bool HasDefault { get; init; }

        public T? DefaultValue { get; init; }

        public IReadOnlyList<INormalizer<T>> Normalizers { get; init; } = [];

        public IReadOnlyList<IValidator<T>> Validators { get; init; } = [];
    }
}
