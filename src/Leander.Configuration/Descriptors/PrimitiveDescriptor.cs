namespace Leander.Configuration.Descriptors;

/// <summary>
/// A text-only description of a primitive with its own rules.
/// </summary>
/// <remarks>
/// A derived primitive refers to its base, which has the base's rules.
/// </remarks>
/// <param name="Type">The value type's name, e.g. <c>Int32</c>.</param>
/// <param name="Name">The primitive's name, unique per type within a contract.</param>
/// <param name="Description">What the values are, or <see langword="null"/>.</param>
public sealed record PrimitiveDescriptor(string Type, string Name, string? Description)
{
    /// <summary>
    /// The primitive this one was derived from, or <see langword="null"/>.
    /// </summary>
    public PrimitiveReference? Base { get; init; }

    /// <summary>
    /// A list primitive's element, or <see langword="null"/>.
    /// </summary>
    public PrimitiveReference? Element { get; init; }

    /// <summary>
    /// A list primitive's delimiter, or <see langword="null"/>.
    /// </summary>
    public char? Delimiter { get; init; }

    /// <summary>
    /// What text its converter reads and writes, or <see langword="null"/> when the converter has no description.
    /// A derived primitive's is on its base, so it is <see langword="null"/> here.
    /// </summary>
    public string? Converter { get; init; }

    /// <summary>
    /// The descriptions of its own normalizers, in order.
    /// </summary>
    public IReadOnlyList<string> Normalizers { get; init; } = [];

    /// <summary>
    /// The descriptions of its own validators, in order.
    /// </summary>
    public IReadOnlyList<string> Validators { get; init; } = [];

    /// <summary>
    /// The names of an enum type's values; <see langword="null"/> for other types.
    /// </summary>
    public IReadOnlyList<string>? Values { get; init; }
}
