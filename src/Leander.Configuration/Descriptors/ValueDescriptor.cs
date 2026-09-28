namespace Leander.Configuration.Descriptors;

// How a value is read, and its rules. A list describes its elements with a ValueDescriptor of their own.
// Type is the value type without Nullable<>: an optional value shows in Presence.
public sealed record ValueDescriptor(string Type, ValuePresence Presence, ValueForm Form)
{
    // Formatted with the primitive's converter, as it would be written in the source.
    // Null without a default, and when the definition is sensitive.
    public string? Default { get; init; }

    // Scalar only: the primitive, described once in ContractDescriptor.Primitives.
    public PrimitiveReference? Primitive { get; init; }

    // Delimited only.
    public char? Delimiter { get; init; }

    // Indexed and Delimited only.
    public ValueDescriptor? Element { get; init; }

    // Rules on the value as a whole, i.e. list rules. Rules on scalar values belong to the primitive.
    public IReadOnlyList<string> Normalizers { get; init; } = [];

    public IReadOnlyList<string> Validators { get; init; } = [];
}
