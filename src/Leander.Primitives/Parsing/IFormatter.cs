namespace Leander.Primitives.Parsing;

/// <summary>
/// Formats values of <typeparamref name="T"/> as strings.
/// </summary>
public interface IFormatter<in T>
{
    /// <summary>
    /// Formats <paramref name="value"/> as a string.
    /// </summary>
    public string Format(T value);
}