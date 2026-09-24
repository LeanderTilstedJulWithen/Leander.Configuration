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
    public abstract void Resolve(ContractResolver resolver, ConfigurationDefinition definition);

    public abstract ReadStatus Read(ConfigurationReader reader, ConfigurationDefinition definition, string key, out T value);

    // Applies the primitive's rules to a value that did not come from the source, e.g. a default.
    public virtual bool TryProcess(ConfigurationReader reader, ConfigurationDefinition definition, string key, T value, out T result)
    {
        result = value;
        return true;
    }
}
