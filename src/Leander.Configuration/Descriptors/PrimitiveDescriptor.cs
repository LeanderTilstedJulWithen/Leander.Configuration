namespace Leander.Configuration.Descriptors;

// A primitive with its own rules. A derived primitive refers to its base, which has the base's rules.
public sealed record PrimitiveDescriptor(string Type, string Name, string? Description)
{
    // The primitive this one was derived from. Null otherwise.
    public PrimitiveReference? Base { get; init; }

    public IReadOnlyList<string> Normalizers { get; init; } = [];

    public IReadOnlyList<string> Validators { get; init; } = [];

    // The names of an enum type. Null for other types.
    public IReadOnlyList<string>? Values { get; init; }
}
