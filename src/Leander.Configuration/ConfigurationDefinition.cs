using Leander.Configuration.Internal;
using Leander.Parsing;

namespace Leander.Configuration;

public abstract class ConfigurationDefinition
{
    private protected ConfigurationDefinition()
    {
    }

    public abstract string Key { get; }

    public abstract Type ValueType { get; }

    public abstract string? Description { get; }

    public abstract bool IsRequired { get; }

    public abstract bool HasDefault { get; }

    public static ConfigurationDefinition<T> Define<T>(string key) =>
        ConfigurationDefinition<T>.Create(key, new ScalarReader<T>(converter: null, converterKey: null));

    public static ConfigurationDefinition<T> Define<T>(string key, IConverter<T> converter) =>
        ConfigurationDefinition<T>.Create(key, new ScalarReader<T>(converter, converterKey: null));

    public static ConfigurationDefinition<T> Define<T>(string key, string converterKey) =>
        ConfigurationDefinition<T>.Create(key, new ScalarReader<T>(converter: null, converterKey));
}
