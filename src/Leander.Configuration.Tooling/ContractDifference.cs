namespace Leander.Configuration.Tooling;

public enum DifferenceKind
{
    // The key is only in the right contract.
    Added,

    // The key is only in the left contract.
    Removed,

    // Both contracts have the key, and one aspect of it differs.
    Changed,
}

// What differs about a key both contracts have. Description covers both the definition's and a primitive's.
public enum DifferenceAspect
{
    Key,
    Type,
    Presence,
    Default,
    Form,
    Sensitive,
    Description,
    Primitive,
    Base,
    Element,
    Delimiter,
    Normalizers,
    Validators,
    Values,
}

// One difference between two contracts, for one key. A changed key has one difference per aspect that differs.
public sealed record ContractDifference(string Key, DifferenceKind Kind)
{
    // Changed only.
    public DifferenceAspect? Aspect { get; init; }

    // The primitive the aspect belongs to, e.g. "Int32 (Port)". Null for an aspect of the definition itself.
    public string? Primitive { get; init; }

    // The left and right values as text. Null when that side has none, e.g. no default.
    public string? Left { get; init; }

    public string? Right { get; init; }

    // "Server:Port: validators of Int32 (Port): [must be between 1 and 65535] → [must be between 1024 and 65535]"
    public override string ToString() => Kind switch
    {
        DifferenceKind.Added => $"{Key}: added",
        DifferenceKind.Removed => $"{Key}: removed",
        _ => $"{Key}: {AspectText()}: {Left ?? "none"} → {Right ?? "none"}",
    };

    private string AspectText()
    {
        var aspect = Aspect?.ToString().ToLowerInvariant();
        return Primitive is null ? $"{aspect}" : $"{aspect} of {Primitive}";
    }
}
