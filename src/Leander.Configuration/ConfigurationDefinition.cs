using Leander.Configuration.Descriptors;
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

    // A definition without a default, that is not optional, is required.
    public bool IsRequired => !HasDefault && !IsOptional;

    public abstract bool HasDefault { get; }

    // A missing optional value is null. See Optional().
    public abstract bool IsOptional { get; }

    // A sensitive value is left out of diagnostics and documentation. See Sensitive().
    public abstract bool IsSensitive { get; }

    public static ConfigurationDefinition<T> Define<T>(string key, Primitive<T> primitive) =>
        ConfigurationDefinition<T>.Create(key, new ScalarReader<T>(primitive));

    internal abstract void Check(ContractChecker checker);

    internal abstract ValueDescriptor CreateValueDescriptor(DescriptorContext context);

    // Reads the value of the definition as an object; used to read a whole contract.
    internal abstract bool TryRead(ReadContext context, out object? value);
}
