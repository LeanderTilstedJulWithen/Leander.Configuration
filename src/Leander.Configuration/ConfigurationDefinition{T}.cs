using Leander.Configuration.Descriptors;
using Leander.Configuration.Internal;
using Leander.Primitives.Internal;

namespace Leander.Configuration;

/// <summary>
/// A configuration value of <typeparamref name="T"/>. Create one with <see cref="ConfigurationDefinition.Define{T}"/>
/// or <see cref="ConfigurationDefinition.Indexed{T}"/>.
/// </summary>
/// <remarks>
/// <para>
/// Definitions are immutable: every builder method returns a new definition. Builder methods never throw;
/// problems are reported when the contract is built or the definition is read.
/// </para>
/// <para>
/// Rules live on the primitive, list rules on the list primitive.
/// A definition only says where the value lives and whether it must be present.
/// </para>
/// </remarks>
public sealed class ConfigurationDefinition<T> : ConfigurationDefinition
{
    private readonly Settings _settings;

    private ConfigurationDefinition(Settings settings)
    {
        _settings = settings;
    }

    /// <inheritdoc/>
    public override string? Description => _settings.Description;

    /// <inheritdoc/>
    public override bool HasDefault => _settings.HasDefault;

    /// <inheritdoc/>
    public override bool IsOptional => _settings.IsOptional;

    /// <inheritdoc/>
    public override bool IsSensitive => _settings.IsSensitive;

    /// <inheritdoc/>
    public override string Key => _settings.Key;

    /// <inheritdoc/>
    public override Type ValueType => typeof(T);

    internal ValueReader<T> Reader => _settings.Reader;

    /// <summary>
    /// Returns a copy that uses <paramref name="value"/> when the value is missing.
    /// Without a default, the value is required.
    /// </summary>
    /// <remarks>
    /// The default goes through the primitive's rules, like a value from the source.
    /// It must not be <see langword="null"/>, and can't be combined with <c>Optional()</c>.
    /// </remarks>
    public ConfigurationDefinition<T> Default(T value) =>
        new(_settings with { HasDefault = true, DefaultValue = value });

    /// <summary>
    /// Returns a copy with a description, for documentation.
    /// </summary>
    public ConfigurationDefinition<T> Describe(string description) =>
        new(_settings with { Description = description });

    /// <summary>
    /// Returns a copy whose value is left out of diagnostics and documentation, e.g. a password.
    /// </summary>
    public ConfigurationDefinition<T> Sensitive() =>
        new(_settings with { IsSensitive = true });

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

    internal static ConfigurationDefinition<T> Create(string key, ValueReader<T> reader) => new(new Settings(key, reader));

    internal override ValueDescriptor CreateValueDescriptor(DescriptorContext context)
    {
        var descriptor = Reader.Describe(context);
        var showDefault = HasDefault && !context.IsSensitive;
        var defaultItems = showDefault ? Reader.FormatItems(context, _settings.DefaultValue!) : null;

        // An indexed default has items only: the source has no single entry to show.
        return descriptor with
        {
            Type = TypeNames.Get(Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T)),
            Presence = IsOptional ? ValuePresence.Optional : HasDefault ? ValuePresence.Default : ValuePresence.Required,
            Default = showDefault && defaultItems is null ? Reader.Format(context, _settings.DefaultValue!) : null,
            DefaultItems = defaultItems,
        };
    }

    // Used by Optional(). The reader turns T into TOptional, i.e. T?.
    internal ConfigurationDefinition<TOptional> ToOptional<TOptional>(ValueReader<TOptional> reader) =>
        new(new ConfigurationDefinition<TOptional>.Settings(Key, reader)
        {
            Description = Description,
            IsOptional = true,
            IsSensitive = IsSensitive,
        });

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
        public T? DefaultValue { get; init; }

        public string? Description { get; init; }

        public bool HasDefault { get; init; }

        public bool IsOptional { get; init; }

        public bool IsSensitive { get; init; }
    }
}
