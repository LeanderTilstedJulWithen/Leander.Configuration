using System.Diagnostics.CodeAnalysis;

namespace Leander.Configuration;

public sealed class NormalizerRegistry
{
    private readonly IReadOnlyDictionary<(Type Type, string Key), object> _normalizers;

    internal NormalizerRegistry(IReadOnlyDictionary<(Type Type, string Key), object> normalizers)
    {
        _normalizers = normalizers;
    }

    public INormalizer<T> GetNormalizer<T>(string key) =>
        TryGetNormalizer<T>(key, out var normalizer)
            ? normalizer
            : throw new KeyNotFoundException($"No normalizer is registered for {typeof(T)} with key '{key}'.");

    public bool TryGetNormalizer<T>(string key, [NotNullWhen(true)] out INormalizer<T>? normalizer)
    {
        normalizer = _normalizers.TryGetValue((typeof(T), key), out var value) ? (INormalizer<T>)value : null;
        return normalizer is not null;
    }
}
