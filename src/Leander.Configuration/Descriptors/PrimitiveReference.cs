namespace Leander.Configuration.Descriptors;

// Refers to a primitive in ContractDescriptor.Primitives. Names are unique per type within a contract.
public sealed record PrimitiveReference(string Type, string Name);
