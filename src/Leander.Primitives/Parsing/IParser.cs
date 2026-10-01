namespace Leander.Primitives.Parsing;

/// <summary>
/// Parses strings into values of <typeparamref name="T"/>.
/// </summary>
public interface IParser<T>
{
    /// <summary>
    /// Parses <paramref name="input"/>; returns <see langword="false"/> if it isn't a valid value.
    /// </summary>
    public bool TryParse(string input, out T result);
}