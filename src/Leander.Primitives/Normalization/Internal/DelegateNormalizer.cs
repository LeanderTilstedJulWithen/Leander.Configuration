namespace Leander.Primitives.Normalization.Internal;

internal sealed class DelegateNormalizer<T>(IFormattableText<T> description, Func<T, T> normalize) : INormalizer<T>
{
    private readonly Func<T, T> _normalize = normalize;

    public IFormattableText<T> Description { get; } = description;

    public T Normalize(T value) => _normalize(value);
}
