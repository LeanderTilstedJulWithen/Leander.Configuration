using System.Diagnostics.CodeAnalysis;
using Leander.Configuration.Internal;
using Leander.Parsing;

namespace Leander.Configuration;

// Reads definitions from a source and collects diagnostics instead of throwing.
// Values of definitions that failed are default(T); call ThrowIfInvalid before using them.
public sealed class ConfigurationReader
{
    private static readonly ConverterRegistry DefaultConverters = new ConverterRegistryBuilder().RegisterDefaults().Build();
    private static readonly ValidatorRegistry EmptyValidators = new ValidatorRegistryBuilder().Build();
    private static readonly NormalizerRegistry EmptyNormalizers = new NormalizerRegistryBuilder().Build();

    private readonly List<ConfigurationDiagnostic> _diagnostics = [];

    public ConfigurationReader(
        IValueSource source,
        ConverterRegistry? converters = null,
        ValidatorRegistry? validators = null,
        NormalizerRegistry? normalizers = null)
    {
        Source = source;
        Converters = converters ?? DefaultConverters;
        Validators = validators ?? EmptyValidators;
        Normalizers = normalizers ?? EmptyNormalizers;
    }

    public IValueSource Source { get; }

    public ConverterRegistry Converters { get; }

    public ValidatorRegistry Validators { get; }

    public NormalizerRegistry Normalizers { get; }

    public IReadOnlyList<ConfigurationDiagnostic> Diagnostics => _diagnostics;

    public bool HasErrors => _diagnostics.Exists(d => d.Severity == DiagnosticSeverity.Error);

    public T Get<T>(ConfigurationDefinition<T> definition)
    {
        TryGet(definition, out var value);
        return value;
    }

    public bool TryGet<T>(ConfigurationDefinition<T> definition, out T value) =>
        definition.TryRead(this, definition.Key, out value);

    public void ThrowIfInvalid()
    {
        if (HasErrors)
        {
            throw new InvalidConfigurationException([.. _diagnostics]);
        }
    }

    internal void Report(DiagnosticSeverity severity, string key, string message, ConfigurationDefinition definition) =>
        _diagnostics.Add(new ConfigurationDiagnostic(severity, key, message, definition));

    internal bool TryResolveConverter<T>(
        IConverter<T>? converter,
        string? converterKey,
        string key,
        ConfigurationDefinition definition,
        [NotNullWhen(true)] out IConverter<T>? resolved)
    {
        resolved = converter;
        if (resolved is not null || (Converters.TryGetConverter(out resolved, converterKey) && resolved is not null))
        {
            return true;
        }

        var name = converterKey is null ? "" : $" with key '{converterKey}'";
        Report(DiagnosticSeverity.Error, key, $"no converter is registered for {TypeNames.Get(typeof(T))}{name}", definition);
        return false;
    }

    internal bool TryResolveValidator<T>(
        ComponentReference<IValidator<T>> reference,
        string key,
        ConfigurationDefinition definition,
        [NotNullWhen(true)] out IValidator<T>? validator)
    {
        validator = reference.Instance;
        if (validator is not null || Validators.TryGetValidator(reference.Key!, out validator))
        {
            return true;
        }

        Report(DiagnosticSeverity.Error, key, $"no validator is registered for {TypeNames.Get(typeof(T))} with key '{reference.Key}'", definition);
        return false;
    }

    internal bool TryResolveNormalizer<T>(
        ComponentReference<INormalizer<T>> reference,
        string key,
        ConfigurationDefinition definition,
        [NotNullWhen(true)] out INormalizer<T>? normalizer)
    {
        normalizer = reference.Instance;
        if (normalizer is not null || Normalizers.TryGetNormalizer(reference.Key!, out normalizer))
        {
            return true;
        }

        Report(DiagnosticSeverity.Error, key, $"no normalizer is registered for {TypeNames.Get(typeof(T))} with key '{reference.Key}'", definition);
        return false;
    }
}
