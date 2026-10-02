namespace Leander.Primitives.Parsing;

/// <summary>
/// Parses strings into values of <typeparamref name="T"/> and formats them back.
/// </summary>
public interface IConverter<T> : IParser<T>, IFormatter<T>
{
    /// <summary>
    /// What text the converter reads and writes, in one sentence for the documentation, e.g. "A 32-bit integer.";
    /// <see langword="null"/> when the type says enough.
    /// </summary>
    public string? Description => null;
}
