namespace Leander.Primitives.Parsing;

public interface IFormatter<in T>
{
    public string Format(T value);
}