namespace Leander.Configuration;

public interface INormalizer<T>
{
    public string Description { get; }

    public T Normalize(T value);
}
