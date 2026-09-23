namespace Leander.Configuration;

public sealed class NormalizerRegistryBuilder
{
    private readonly Dictionary<(Type Type, string Key), object> _normalizers = [];

    public NormalizerRegistryBuilder Register<T>(INormalizer<T> normalizer, string key)
    {
        _normalizers[(typeof(T), key)] = normalizer;
        return this;
    }

    public NormalizerRegistry Build() => new(new Dictionary<(Type Type, string Key), object>(_normalizers));
}
