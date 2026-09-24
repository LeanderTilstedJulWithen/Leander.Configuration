using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Leander.Primitives;

// Stores resolved primitives per (type, name). The primitive registered under (type, null) is the default for that type.
// Types without a registered default are offered to the fallbacks; their results are cached.
public sealed class PrimitiveRegistry
{
    private readonly IReadOnlyDictionary<(Type Type, string? Name), Primitive> _primitives;
    private readonly IReadOnlyList<IPrimitiveFallback> _fallbacks;
    private readonly ConcurrentDictionary<Type, Primitive?> _fallbackDefaults = new();

    internal PrimitiveRegistry(
        IReadOnlyDictionary<(Type Type, string? Name), Primitive> primitives,
        IReadOnlyList<IPrimitiveFallback> fallbacks)
    {
        _primitives = primitives;
        _fallbacks = fallbacks;
    }

    public Primitive<T> Get<T>(string? name = null) =>
        TryGet<T>(name, out var primitive)
            ? primitive
            : throw new KeyNotFoundException(name is null
                ? $"No default primitive is registered for {typeof(T)}."
                : $"No primitive named '{name}' is registered for {typeof(T)}.");

    public bool TryGet<T>(string? name, [NotNullWhen(true)] out Primitive<T>? primitive)
    {
        if (_primitives.TryGetValue((typeof(T), name), out var value))
        {
            primitive = (Primitive<T>)value;
            return true;
        }

        primitive = name is null ? (Primitive<T>?)_fallbackDefaults.GetOrAdd(typeof(T), _ => ResolveFallback<T>()) : null;
        return primitive is not null;
    }

    // Resolves a definition that is not registered, e.g. one referenced directly by a configuration definition.
    internal bool TryResolve<T>(PrimitiveDefinition<T> definition, [NotNullWhen(true)] out Primitive<T>? primitive)
    {
        primitive = definition.Resolve(TryGet<T>(null, out var defaultPrimitive) ? defaultPrimitive : null);
        return primitive is not null;
    }

    private Primitive<T>? ResolveFallback<T>()
    {
        foreach (var fallback in _fallbacks)
        {
            if (fallback.Define<T>() is { } definition)
            {
                return definition.Resolve(defaultPrimitive: null);
            }
        }

        return null;
    }
}
