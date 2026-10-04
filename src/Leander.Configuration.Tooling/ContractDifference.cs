namespace Leander.Configuration.Tooling;

/// <summary>
/// How a key differs between two contracts.
/// </summary>
public enum DifferenceKind
{
    /// <summary>
    /// The key is only in the right contract.
    /// </summary>
    Added,

    /// <summary>
    /// The key is only in the left contract.
    /// </summary>
    Removed,

    /// <summary>
    /// Both contracts have the key, and one aspect of it differs.
    /// </summary>
    Changed,
}

/// <summary>
/// What differs about a key both contracts have.
/// </summary>
public enum DifferenceAspect
{
    /// <summary>
    /// The key's spelling: keys match case-insensitively.
    /// </summary>
    Key,

    /// <summary>
    /// The value type.
    /// </summary>
    Type,

    /// <summary>
    /// Whether the value is required, has a default or is optional.
    /// </summary>
    Presence,

    /// <summary>
    /// The default.
    /// </summary>
    Default,

    /// <summary>
    /// Whether the value is scalar or indexed.
    /// </summary>
    Form,

    /// <summary>
    /// Whether the value is sensitive.
    /// </summary>
    Sensitive,

    /// <summary>
    /// The definition's description, or a primitive's.
    /// </summary>
    Description,

    /// <summary>
    /// The primitive the definition uses.
    /// </summary>
    Primitive,

    /// <summary>
    /// The primitive a primitive was derived from.
    /// </summary>
    Base,

    /// <summary>
    /// A list primitive's element.
    /// </summary>
    Element,

    /// <summary>
    /// The description of a primitive's converter: what text it reads and writes.
    /// </summary>
    Converter,

    /// <summary>
    /// A list primitive's delimiter.
    /// </summary>
    Delimiter,

    /// <summary>
    /// A primitive's normalizers.
    /// </summary>
    Normalizers,

    /// <summary>
    /// A primitive's validators.
    /// </summary>
    Validators,

    /// <summary>
    /// An enum primitive's values.
    /// </summary>
    Values,
}

/// <summary>
/// One difference between two contracts, for one key. A changed key has one difference per aspect that differs.
/// </summary>
/// <param name="Key">The key, as the left contract spells it when both have it.</param>
/// <param name="Kind">Whether the key was added, removed or changed.</param>
public sealed record ContractDifference(string Key, DifferenceKind Kind)
{
    /// <summary>
    /// What differs; <see langword="null"/> unless <see cref="Kind"/> is <see cref="DifferenceKind.Changed"/>.
    /// </summary>
    public DifferenceAspect? Aspect { get; init; }

    /// <summary>
    /// The left value as text; <see langword="null"/> when it has none, e.g. no default.
    /// </summary>
    public string? Left { get; init; }

    /// <summary>
    /// The primitive the aspect belongs to, e.g. "Int32 (Port)";
    /// <see langword="null"/> for an aspect of the definition itself.
    /// </summary>
    public string? Primitive { get; init; }

    /// <summary>
    /// The right value as text; <see langword="null"/> when it has none, e.g. no default.
    /// </summary>
    public string? Right { get; init; }

    /// <summary>
    /// Formats the difference for people, e.g.
    /// <c>Server:Port: validators of Int32 (Port): [must be between 1 and 65535] → [must be between 1024 and 65535]</c>.
    /// </summary>
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
