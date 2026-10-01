using Leander.Configuration.Internal;

namespace Leander.Configuration;

/// <summary>
/// <c>Optional()</c> for definitions of value types.
/// </summary>
// C# does not overload on constraints alone, so reference types get their own class.
public static class OptionalValueExtensions
{
    /// <summary>
    /// Returns a copy whose missing value is <see langword="null"/> instead of an error.
    /// </summary>
    /// <remarks>
    /// <typeparamref name="T"/> becomes <see cref="Nullable{T}"/>.
    /// A present value still goes through the primitive. Can't be combined with a default.
    /// </remarks>
    public static ConfigurationDefinition<T?> Optional<T>(this ConfigurationDefinition<T> definition)
        where T : struct =>
        definition.ToOptional(new OptionalValueReader<T>(definition));
}
