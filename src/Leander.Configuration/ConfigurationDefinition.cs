using Leander.Configuration.Internal;
using Leander.Primitives;

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

    // Uses the default primitive for T.
    public static ConfigurationDefinition<T> Define<T>(string key) =>
        ConfigurationDefinition<T>.Create(key, new ScalarReader<T>(PrimitiveDefinition.Define<T>(), primitiveName: null));

    public static ConfigurationDefinition<T> Define<T>(string key, PrimitiveDefinition<T> primitive) =>
        ConfigurationDefinition<T>.Create(key, new ScalarReader<T>(primitive, primitiveName: null));

    // Uses a primitive registered under the given name.
    public static ConfigurationDefinition<T> Define<T>(string key, string primitiveName) =>
        ConfigurationDefinition<T>.Create(key, new ScalarReader<T>(primitive: null, primitiveName));

    internal abstract void Resolve(ContractResolver resolver);

    // Reads the definition and discards the value; used to validate a whole contract.
    internal abstract void Read(ConfigurationReader reader);
}
