namespace Leander.Primitives.Parsing;

public interface IConverter<T> : IParser<T>, IFormatter<T>
{
}
