namespace Leander.Primitives.Normalization;

public interface INormalizer<T>
{
    // What the rule does, in one line, e.g. for documentation.
    public IFormattableText<T> Description { get; }

    public T Normalize(T value);
}
