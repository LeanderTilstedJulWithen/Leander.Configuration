using System.Diagnostics.CodeAnalysis;

namespace Leander.Configuration;

public sealed class ValidatorRegistry
{
    private readonly IReadOnlyDictionary<(Type Type, string Key), object> _validators;

    internal ValidatorRegistry(IReadOnlyDictionary<(Type Type, string Key), object> validators)
    {
        _validators = validators;
    }

    public IValidator<T> GetValidator<T>(string key) =>
        TryGetValidator<T>(key, out var validator)
            ? validator
            : throw new KeyNotFoundException($"No validator is registered for {typeof(T)} with key '{key}'.");

    public bool TryGetValidator<T>(string key, [NotNullWhen(true)] out IValidator<T>? validator)
    {
        validator = _validators.TryGetValue((typeof(T), key), out var value) ? (IValidator<T>)value : null;
        return validator is not null;
    }
}
