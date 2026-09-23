namespace Leander.Configuration.Internal;

internal enum ReadStatus
{
    Missing,
    Read,
    Failed,
}

// Reads the raw value of a definition from the source and parses it. Presence rules,
// normalization and validation are handled by the definition.
internal abstract class ValueReader<T>
{
    public abstract ReadStatus Read(ConfigurationReader reader, ConfigurationDefinition definition, string key, out T value);
}
