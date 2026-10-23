namespace Leander.Configuration;

/// <summary>
/// Marks a <c>static partial</c> method that builds an options object from a <see cref="ConfigurationSnapshot"/>.
/// The Leander.Configuration.Generators source generator writes its body.
/// </summary>
/// <remarks>
/// <para>
/// The method takes one <see cref="ConfigurationSnapshot"/> and returns the options type. Every constructor parameter and property
/// of the options type that is marked with <see cref="FromDefinitionAttribute"/> is set from the snapshot, and nothing else:
/// the generated code is the explicit construction you would write by hand, never a mapping by name.
/// </para>
/// <code>
/// public sealed partial record ServerOptions(
///     [FromDefinition(nameof(ServerConfiguration.Host))] string Host,
///     [FromDefinition(nameof(ServerConfiguration.Port))] int Port)
/// {
///     [GeneratedOptions(typeof(ServerConfiguration))]
///     public static partial ServerOptions FromSnapshot(ConfigurationSnapshot snapshot);
/// }
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class GeneratedOptionsAttribute : Attribute
{
    /// <summary>
    /// Definitions are looked up in the type that declares the method.
    /// </summary>
    public GeneratedOptionsAttribute()
    {
    }

    /// <summary>
    /// Definitions are looked up in <paramref name="definitions"/>, unless a <see cref="FromDefinitionAttribute"/> names another type.
    /// </summary>
    public GeneratedOptionsAttribute(Type definitions)
    {
        Definitions = definitions;
    }

    /// <summary>
    /// The type that holds the definitions, or <see langword="null"/> for the type that declares the method.
    /// </summary>
    public Type? Definitions { get; }
}
