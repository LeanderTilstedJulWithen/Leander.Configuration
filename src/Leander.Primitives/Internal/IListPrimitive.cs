namespace Leander.Primitives.Internal;

// A list primitive without its item type, for code that walks primitives, e.g. contract checks and descriptors.
internal interface IListPrimitive
{
    Primitive Element { get; }

    char Delimiter { get; }
}
