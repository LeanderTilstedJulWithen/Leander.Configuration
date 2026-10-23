namespace Leander.Configuration;

/// <summary>
/// Names the configuration definition a constructor parameter or property of an options type is set from,
/// in a method marked with <see cref="GeneratedOptionsAttribute"/>.
/// </summary>
/// <remarks>
/// The definition is a static field or property of type <see cref="ConfigurationDefinition{T}"/>, of any accessibility the generated
/// method can reach. Name it with <c>nameof</c>, so a renamed definition doesn't compile instead of quietly breaking.
/// </remarks>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, Inherited = false)]
public sealed class FromDefinitionAttribute : Attribute
{
    /// <summary>
    /// The definition <paramref name="member"/> of the type named by <see cref="GeneratedOptionsAttribute"/>.
    /// </summary>
    public FromDefinitionAttribute(string member)
    {
        Member = member;
    }

    /// <summary>
    /// The definition <paramref name="member"/> of <paramref name="definitions"/>.
    /// </summary>
    public FromDefinitionAttribute(Type definitions, string member)
    {
        Definitions = definitions;
        Member = member;
    }

    /// <summary>
    /// The type that holds the definition, or <see langword="null"/> for the one named by <see cref="GeneratedOptionsAttribute"/>.
    /// </summary>
    public Type? Definitions { get; }

    /// <summary>
    /// The name of the static field or property that holds the definition.
    /// </summary>
    public string Member { get; }
}
