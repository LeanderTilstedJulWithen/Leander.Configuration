using Leander.Primitives;

namespace Leander.Configuration.Internal;

// Resolves the primitive of every scalar reader in a contract, keyed by the reader instance.
internal sealed class ContractResolver(PrimitiveRegistry registry)
{
    private readonly Dictionary<object, Primitive> _resolved = new(ReferenceEqualityComparer.Instance);

    public IReadOnlyDictionary<object, Primitive> Resolved => _resolved;

    public List<string> Failures { get; } = [];

    public void Resolve<T>(object owner, string key, PrimitiveDefinition<T> definition)
    {
        if (registry.TryResolve(definition, out var primitive))
        {
            _resolved[owner] = primitive;
        }
        else
        {
            Failures.Add($"{key}: no converter is available for {TypeNames.Get(typeof(T))}.");
        }
    }

    public void Resolve<T>(object owner, string key, string primitiveName)
    {
        if (registry.TryGet<T>(primitiveName, out var primitive))
        {
            _resolved[owner] = primitive;
        }
        else
        {
            Failures.Add($"{key}: no primitive named '{primitiveName}' is registered for {TypeNames.Get(typeof(T))}.");
        }
    }
}
