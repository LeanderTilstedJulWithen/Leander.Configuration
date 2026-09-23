using Leander.Configuration.Internal;

namespace Leander.Configuration;

// Definitions are immutable: every builder method returns a new definition.
// Builder methods never throw; problems are reported as diagnostics when the definition is read.
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

    public override bool IsRequired => _settings.IsRequired;

    public override bool HasDefault => _settings.HasDefault;

    internal ValueReader<T> Reader => _settings.Reader;

    internal static ConfigurationDefinition<T> Create(string key, ValueReader<T> reader) => new(new Settings(key, reader));

    public ConfigurationDefinition<T> Describe(string description) =>
        new(_settings with { Description = description });

    // Required and Default are mutually exclusive; the last one called wins.
    public ConfigurationDefinition<T> Required() =>
        new(_settings with { IsRequired = true, HasDefault = false, DefaultValue = default });

    public ConfigurationDefinition<T> Default(T value) =>
        new(_settings with { IsRequired = false, HasDefault = true, DefaultValue = value });

    public ConfigurationDefinition<T> Normalize(INormalizer<T> normalizer) =>
        new(_settings with { Normalizers = [.. _settings.Normalizers, new ComponentReference<INormalizer<T>>(normalizer, null)] });

    public ConfigurationDefinition<T> Normalize(string normalizerKey) =>
        new(_settings with { Normalizers = [.. _settings.Normalizers, new ComponentReference<INormalizer<T>>(null, normalizerKey)] });

    public ConfigurationDefinition<T> Validate(IValidator<T> validator) =>
        new(_settings with { Validators = [.. _settings.Validators, new ComponentReference<IValidator<T>>(validator, null)] });

    public ConfigurationDefinition<T> Validate(string validatorKey) =>
        new(_settings with { Validators = [.. _settings.Validators, new ComponentReference<IValidator<T>>(null, validatorKey)] });

    // Everything configured so far applies to each element; everything configured afterwards applies to the list.
    public ConfigurationDefinition<IReadOnlyList<T>> Indexed() => ToList(new IndexedReader<T>(this));

    public ConfigurationDefinition<IReadOnlyList<T>> Delimited(char delimiter = ',') => ToList(new DelimitedReader<T>(this, delimiter));

    private ConfigurationDefinition<IReadOnlyList<T>> ToList(ValueReader<IReadOnlyList<T>> reader) =>
        new(new ConfigurationDefinition<IReadOnlyList<T>>.Settings(Key, reader)
        {
            Description = Description,
            IsRequired = IsRequired,
        });

    internal bool TryRead(ConfigurationReader reader, string key, out T value)
    {
        switch (Reader.Read(reader, this, key, out var raw))
        {
            case ReadStatus.Read:
                return TryProcess(reader, key, raw, out value);

            case ReadStatus.Missing when HasDefault:
                return TryProcess(reader, key, _settings.DefaultValue!, out value);

            case ReadStatus.Missing when IsRequired:
                reader.Report(DiagnosticSeverity.Error, key, "value is required", this);
                value = default!;
                return false;

            case ReadStatus.Missing:
                reader.Report(DiagnosticSeverity.Trace, key, "value is not set and has no default", this);
                value = default!;
                return true;

            default:
                value = default!;
                return false;
        }
    }

    // Runs the fixed pipeline stages after parsing: normalize, then validate.
    internal bool TryProcess(ConfigurationReader reader, string key, T value, out T result)
    {
        result = value;

        foreach (var reference in _settings.Normalizers)
        {
            if (!reader.TryResolveNormalizer(reference, key, this, out var normalizer))
            {
                result = default!;
                return false;
            }

            try
            {
                result = normalizer.Normalize(result);
            }
            catch (Exception exception)
            {
                reader.Report(DiagnosticSeverity.Error, key, $"normalizer '{normalizer.Description}' failed: {exception.Message}", this);
                result = default!;
                return false;
            }
        }

        var isValid = true;
        foreach (var reference in _settings.Validators)
        {
            if (!reader.TryResolveValidator(reference, key, this, out var validator))
            {
                isValid = false;
                continue;
            }

            try
            {
                if (validator.Validate(result) is { } message)
                {
                    reader.Report(DiagnosticSeverity.Error, key, message, this);
                    isValid = false;
                }
            }
            catch (Exception exception)
            {
                reader.Report(DiagnosticSeverity.Error, key, $"validator '{validator.Description}' failed: {exception.Message}", this);
                isValid = false;
            }
        }

        return isValid;
    }

    private sealed record Settings(string Key, ValueReader<T> Reader)
    {
        public string? Description { get; init; }

        public bool IsRequired { get; init; }

        public bool HasDefault { get; init; }

        public T? DefaultValue { get; init; }

        public IReadOnlyList<ComponentReference<INormalizer<T>>> Normalizers { get; init; } = [];

        public IReadOnlyList<ComponentReference<IValidator<T>>> Validators { get; init; } = [];
    }
}
