namespace Leander.Primitives.Parsing;

/// <summary>
/// Parses strings into values of <typeparamref name="T"/> and formats them back.
/// </summary>
public interface IConverter<T> : IParser<T>, IFormatter<T>
{
}
