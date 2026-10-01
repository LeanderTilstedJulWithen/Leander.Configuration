namespace Leander.Configuration.Descriptors;

/// <summary>
/// Refers to a primitive in <see cref="ContractDescriptor.Primitives"/>. Names are unique per type within a contract.
/// </summary>
/// <param name="Type">The value type's name, e.g. <c>Int32</c>.</param>
/// <param name="Name">The primitive's name.</param>
public sealed record PrimitiveReference(string Type, string Name);
