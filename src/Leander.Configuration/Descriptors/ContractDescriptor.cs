namespace Leander.Configuration.Descriptors;

// A text-only description of a contract, for documentation and contract files. See ConfigurationContract.CreateDescriptor().
// It holds no types or delegates, so a descriptor read back from a contract file is the same kind of object.
public sealed record ContractDescriptor(
    IReadOnlyList<DefinitionDescriptor> Definitions,
    IReadOnlyList<PrimitiveDescriptor> Primitives);
