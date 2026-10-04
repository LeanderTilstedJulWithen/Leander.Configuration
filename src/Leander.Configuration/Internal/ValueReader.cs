using Leander.Configuration.Descriptors;

namespace Leander.Configuration.Internal;

internal enum ReadStatus
{
    Missing,
    Read,
    Failed,
}

// Reads the value of a definition from the source, including the primitive's rules.
// Presence rules and definition-level rules are handled by the definition.
internal abstract class ValueReader<T>
{
    public abstract void Check(ContractChecker checker, ConfigurationDefinition definition);

    // Describes how the value is read: its form, primitive and elements. The definition adds type, presence, default and rules.
    public abstract ValueDescriptor Describe(DescriptorContext context);

    // Formats a value as it would be written in the source, e.g. a default for documentation.
    public abstract string Format(DescriptorContext context, T value);

    // Formats each item of a list value, for indexed lists. Null for other values.
    public virtual IReadOnlyList<string>? FormatItems(DescriptorContext context, T value) => null;

    public abstract ReadStatus Read(ReadContext context, ConfigurationDefinition definition, string key, out T value);

    // Applies the primitive's rules to a value that did not come from the source, e.g. a default.
    public virtual bool TryProcess(ReadContext context, ConfigurationDefinition definition, string key, T value, out T result)
    {
        result = value;
        return true;
    }
}
