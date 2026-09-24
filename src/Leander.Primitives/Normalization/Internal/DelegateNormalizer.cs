namespace Leander.Primitives.Normalization.Internal;

internal sealed class DelegateNormalizer<T>(string description, Func<T, T> normalize) : INormalizer<T>
{
    public string Description => description;

    public T Normalize(T value) => normalize(value);
}
