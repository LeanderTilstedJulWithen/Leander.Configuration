using Leander.Configuration.Internal;

namespace Leander.Configuration;

/// <summary>
/// <c>Optional()</c> for definitions of reference types.
/// </summary>
public static class OptionalReferenceExtensions
{
    /// <summary>
    /// Returns a copy whose missing value is <see langword="null"/> instead of an error.
    /// </summary>
    /// <remarks>
    /// <typeparamref name="T"/> becomes the annotated <c>T?</c>. At runtime <c>T?</c> is <typeparamref name="T"/>,
    /// so <see cref="ConfigurationDefinition.IsOptional"/> is what tells them apart.
    /// A present value still goes through the primitive. Can't be combined with a default.
    /// </remarks>
    public static ConfigurationDefinition<T?> Optional<T>(this ConfigurationDefinition<T> definition)
        where T : class =>
        definition.ToOptional(new OptionalReferenceReader<T>(definition));
}
