namespace Leander.Configuration.Descriptors;

/// <summary>
/// A text-only description of how a value is read.
/// </summary>
/// <remarks>
/// Values are flat: a list is described by its list primitive, which refers to its element.
/// </remarks>
/// <param name="Type">The value type's name, without <see cref="Nullable{T}"/>: an optional value shows in <paramref name="Presence"/>.</param>
/// <param name="Presence">Whether the value must be present.</param>
/// <param name="Form">Where the value lives in the source.</param>
public sealed record ValueDescriptor(string Type, ValuePresence Presence, ValueForm Form)
{
    /// <summary>
    /// The default, formatted with the primitive's converter, as it would be written in the source.
    /// <see langword="null"/> without a default, and when the definition is sensitive.
    /// </summary>
    public string? Default { get; init; }

    /// <summary>
    /// For <see cref="ValueForm.Indexed"/> only: each item of the default, formatted with the element's converter.
    /// <see langword="null"/> like <see cref="Default"/>.
    /// </summary>
    // Splitting Default on the delimiter is wrong when an item contains it.
    public IReadOnlyList<string>? DefaultItems { get; init; }

    /// <summary>
    /// The primitive, described once in <see cref="ContractDescriptor.Primitives"/>.
    /// For <see cref="ValueForm.Indexed"/>, the list primitive.
    /// </summary>
    public PrimitiveReference? Primitive { get; init; }
}
