namespace Leander.Primitives.Normalization;

public interface INormalizer<T>
{
    public string Description { get; }

    public T Normalize(T value);
}
