namespace Leander.Primitives;

public abstract class Primitive
{
    private protected Primitive()
    {
    }

    // Null for the default primitive of a type.
    public abstract string? Name { get; }

    public abstract Type ValueType { get; }

    public abstract string? Description { get; }
}
