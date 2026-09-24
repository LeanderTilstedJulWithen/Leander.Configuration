namespace Leander.Primitives;

// Supplies a default definition for types that have no registered default, e.g. all enums.
public interface IPrimitiveFallback
{
    // Returns null when this fallback does not handle T.
    public PrimitiveDefinition<T>? Define<T>();
}
