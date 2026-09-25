namespace Leander.Primitives.Internal;

// A registered primitive definition that could not be resolved. Name is null for the default of the type.
internal sealed record PrimitiveFailure(Type Type, string? Name, string Message);
