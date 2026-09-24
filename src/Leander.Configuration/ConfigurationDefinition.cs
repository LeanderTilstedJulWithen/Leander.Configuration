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

    // A definition without a default is required.
    public bool IsRequired => !HasDefault;

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

    // Reads the value of the definition as an object; used to read a whole contract.
    internal abstract bool TryRead(ReadContext context, out object? value);
}
