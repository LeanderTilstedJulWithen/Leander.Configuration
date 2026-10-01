namespace Leander.Configuration.Descriptors;

// How a value is read. Values are flat: a list is described by its list primitive, which refers to its element.
// Type is the value type without Nullable<>: an optional value shows in Presence.
public sealed record ValueDescriptor(string Type, ValuePresence Presence, ValueForm Form)
{
    // Formatted with the primitive's converter, as it would be written in the source.
    // Null without a default, and when the definition is sensitive.
    public string? Default { get; init; }

    // Indexed only: each item of the default, formatted with the element's converter. Null like Default.
    // Splitting Default on the delimiter is wrong when an item contains it.
    public IReadOnlyList<string>? DefaultItems { get; init; }

    // The primitive, described once in ContractDescriptor.Primitives. For Indexed, the list primitive.
    public PrimitiveReference? Primitive { get; init; }
}
