namespace Leander.Configuration.Descriptors;

/// <summary>
/// A text-only description of a contract, for documentation and contract files.
/// See <see cref="ConfigurationContract.CreateDescriptor"/>.
/// </summary>
/// <remarks>
/// It holds no types or delegates, so a descriptor read back from a contract file is the same kind of object.
/// </remarks>
/// <param name="Definitions">Every definition, in the contract's order.</param>
/// <param name="Primitives">Every primitive the definitions use, directly or as a base or element; each once.</param>
public sealed record ContractDescriptor(
    IReadOnlyList<DefinitionDescriptor> Definitions,
    IReadOnlyList<PrimitiveDescriptor> Primitives);
