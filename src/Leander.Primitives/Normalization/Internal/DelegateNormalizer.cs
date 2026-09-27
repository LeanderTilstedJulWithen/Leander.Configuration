namespace Leander.Primitives.Normalization.Internal;

internal sealed class DelegateNormalizer<T>(string description, Func<T, T> normalize) : INormalizer<T>
{
    private readonly string _description = description;
    private readonly Func<T, T> _normalize = normalize;

    public string Description => _description;

    public T Normalize(T value) => _normalize(value);
}
