namespace Leander.Configuration.Descriptors;

// A resolved primitive. The rules include those inherited from the default for its type.
// Name is null for the default primitive of a type.
public sealed record PrimitiveDescriptor(string Type, string? Name, string? Description)
{
    public IReadOnlyList<string> Normalizers { get; init; } = [];

    public IReadOnlyList<string> Validators { get; init; } = [];

    // The names of an enum type. Null for other types.
    public IReadOnlyList<string>? Values { get; init; }
}
