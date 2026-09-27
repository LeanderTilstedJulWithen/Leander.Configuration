using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Leander.Primitives;

// Stores resolved primitives per (type, name). The primitive registered under (type, null) is the default for that type.
// Types without a registered default are offered to the fallbacks; their results are cached.
public sealed class PrimitiveRegistry
{
    private readonly IReadOnlyDictionary<(Type Type, string? Name), Primitive> _primitives;
    private readonly IReadOnlyDictionary<PrimitiveDefinition, Primitive> _definitions;
    private readonly HashSet<Primitive> _registered;
    private readonly IReadOnlyList<IPrimitiveFallback> _fallbacks;
    private readonly ConcurrentDictionary<Type, Primitive?> _fallbackDefaults = new();

    // definitions maps each registered definition instance to its primitive.
    internal PrimitiveRegistry(
        IReadOnlyDictionary<(Type Type, string? Name), Primitive> primitives,
        IReadOnlyDictionary<PrimitiveDefinition, Primitive> definitions,
        IReadOnlyList<IPrimitiveFallback> fallbacks)
    {
        _primitives = primitives;
        _definitions = definitions;
        _registered = new HashSet<Primitive>(primitives.Values, ReferenceEqualityComparer.Instance);
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

    // Resolves a definition referenced directly, e.g. by a configuration definition.
    // A registered definition gives the registered primitive, and a definition that adds nothing gives the default for T.
    // Anything else, e.g. a derived definition, gives a new primitive that is not registered.
    internal bool TryResolve<T>(PrimitiveDefinition<T> definition, [NotNullWhen(true)] out Primitive<T>? primitive)
    {
        if (_definitions.TryGetValue(definition, out var registered))
        {
            primitive = (Primitive<T>)registered;
            return true;
        }

        TryGet<T>(null, out var defaultPrimitive);
        if (defaultPrimitive is not null && definition.IsEmpty)
        {
            primitive = defaultPrimitive;
            return true;
        }

        primitive = definition.Resolve(defaultPrimitive);
        return primitive is not null;
    }

    // Whether the primitive is registered, or the default from a fallback, rather than resolved from an unregistered definition.
    internal bool IsRegistered(Primitive primitive) =>
        _registered.Contains(primitive) || _fallbackDefaults.Values.Contains(primitive);

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
