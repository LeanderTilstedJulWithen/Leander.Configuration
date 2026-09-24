using Leander.Primitives.Parsing;

namespace Leander.Primitives;

public abstract class PrimitiveDefinition
{
    private protected PrimitiveDefinition()
    {
    }

    // Null for the default definition of a type.
    public abstract string? Name { get; }

    public abstract Type ValueType { get; }

    public abstract string? Description { get; }

    public static PrimitiveDefinition<T> Define<T>() =>
        PrimitiveDefinition<T>.Create(name: null, converter: null);

    public static PrimitiveDefinition<T> Define<T>(IConverter<T> converter) =>
        PrimitiveDefinition<T>.Create(name: null, converter);

    public static PrimitiveDefinition<T> Define<T>(string name) =>
        PrimitiveDefinition<T>.Create(name, converter: null);

    public static PrimitiveDefinition<T> Define<T>(string name, IConverter<T> converter) =>
        PrimitiveDefinition<T>.Create(name, converter);

    // Combines this definition with the default for its type from the registry (if any).
    // Returns null when no converter can be found.
    internal abstract Primitive? ResolveIn(PrimitiveRegistry? registry);
}
