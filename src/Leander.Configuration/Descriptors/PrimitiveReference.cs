namespace Leander.Configuration.Descriptors;

// Refers to a primitive in ContractDescriptor.Primitives. Name is null for the default primitive of a type.
public sealed record PrimitiveReference(string Type, string? Name);
