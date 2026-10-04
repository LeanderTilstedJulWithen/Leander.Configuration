using Leander.Configuration.Descriptors;
using Leander.Configuration.Internal;
using Leander.Primitives;

namespace Leander.Configuration;

/// <summary>
/// A configuration value: where it lives, the primitive it is read with, and whether it must be present.
/// See <see cref="ConfigurationDefinition{T}"/>.
/// </summary>
public abstract class ConfigurationDefinition
{
    private protected ConfigurationDefinition()
    {
    }

    /// <summary>
    /// Defines a value at <paramref name="key"/>, read with <paramref name="primitive"/>.
    /// </summary>
    /// <remarks>
    /// With a <see cref="ListPrimitive{T}"/>, the value is one delimited entry, e.g. <c>Hosts=a,b</c>.
    /// </remarks>
    public static ConfigurationDefinition<T> Define<T>(string key, Primitive<T> primitive) =>
        ConfigurationDefinition<T>.Create(key, new ScalarReader<T>(primitive));

    /// <summary>
    /// Defines a list at <paramref name="key"/> with one entry per item: <c>Key:0</c>, <c>Key:1</c>, ...
    /// </summary>
    /// <remarks>
    /// The list's delimiter isn't used; <see cref="Define{T}"/> reads one delimited entry.
    /// </remarks>
    public static ConfigurationDefinition<IReadOnlyList<T>> Indexed<T>(string key, ListPrimitive<T> list) =>
        ConfigurationDefinition<IReadOnlyList<T>>.Create(key, new IndexedReader<T>(list));

    /// <summary>
    /// What the value is for, for documentation. Optional.
    /// </summary>
    public abstract string? Description { get; }

    /// <summary>
    /// Whether a missing value is replaced by a default.
    /// </summary>
    public abstract bool HasDefault { get; }

    /// <summary>
    /// Whether a missing value is <see langword="null"/>. See <c>Optional()</c>.
    /// </summary>
    public abstract bool IsOptional { get; }

    /// <summary>
    /// Whether the value must be present: it has no default and is not optional.
    /// </summary>
    public bool IsRequired => !HasDefault && !IsOptional;

    /// <summary>
    /// Whether the value is left out of diagnostics and documentation. See <see cref="ConfigurationDefinition{T}.Sensitive"/>.
    /// </summary>
    public abstract bool IsSensitive { get; }

    /// <summary>
    /// The configuration key, with sections separated by <c>:</c>, e.g. <c>Server:Port</c>.
    /// </summary>
    public abstract string Key { get; }

    /// <summary>
    /// The type of the value.
    /// </summary>
    public abstract Type ValueType { get; }

    internal abstract void Check(ContractChecker checker);

    internal abstract ValueDescriptor CreateValueDescriptor(DescriptorContext context);

    // Reads the value of the definition as an object; used to read a whole contract.
    internal abstract bool TryRead(ReadContext context, out object? value);
}
