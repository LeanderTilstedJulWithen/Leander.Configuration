using Leander.Configuration.Descriptors;
using Leander.Configuration.Internal;
using Leander.Primitives.Internal;

namespace Leander.Configuration;

// Definitions are immutable: every builder method returns a new definition.
// Builder methods never throw; problems are reported when the contract is built or the definition is read.
// Rules live on the primitive, list rules on the list primitive. A definition only says where the value lives and its presence.
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
        };
    }

    internal override bool TryRead(ReadContext context, out object? value)
    {
        var success = TryRead(context, out T typed);
        value = typed;
        return success;
    }

    private bool TryRead(ReadContext context, out T value)
    {
        switch (Reader.Read(context, this, Key, out value))
        {
            case ReadStatus.Read:
                return true;

            // The default goes through the primitive's rules, like a value from the source.
            case ReadStatus.Missing when HasDefault:
                return Reader.TryProcess(context, this, Key, _settings.DefaultValue!, out value);

            // T is nullable, so default is null. The primitive never sees it.
            case ReadStatus.Missing when IsOptional:
                value = default!;
                return true;

            case ReadStatus.Missing:
                context.Report(DiagnosticSeverity.Error, Key, "value is required", this);
                value = default!;
                return false;

            default:
                value = default!;
                return false;
        }
    }

    private sealed record Settings(string Key, ValueReader<T> Reader)
    {
        public string? Description { get; init; }

        public bool HasDefault { get; init; }

        public bool IsOptional { get; init; }

        public bool IsSensitive { get; init; }

        public T? DefaultValue { get; init; }
    }
}
