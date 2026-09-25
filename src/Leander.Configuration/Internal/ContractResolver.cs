using Leander.Primitives;
using Leander.Primitives.Internal;

namespace Leander.Configuration.Internal;

// Resolves the primitive of every scalar reader in a contract, keyed by the reader instance.
// Definitions that depend on a registered primitive that failed to resolve name that primitive as the cause.
internal sealed class ContractResolver(PrimitiveRegistry registry, IReadOnlyList<PrimitiveFailure> primitiveFailures)
{
    private readonly Dictionary<object, Primitive> _resolved = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<(Type Type, string? Name)> _failedPrimitives = [.. primitiveFailures.Select(f => (f.Type, f.Name))];

    public IReadOnlyDictionary<object, Primitive> Resolved => _resolved;

    public List<string> Failures { get; } = [.. primitiveFailures.Select(f => f.Message)];

    public void Resolve<T>(object owner, string key, PrimitiveDefinition<T> definition)
    {
        if (registry.TryResolve(definition, out var primitive))
        {
            _resolved[owner] = primitive;
        }
        else if (_failedPrimitives.Contains((typeof(T), null)))
        {
            Failures.Add($"{key}: the default primitive for {TypeNames.Get(typeof(T))} could not be resolved.");
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
        else if (_failedPrimitives.Contains((typeof(T), primitiveName)))
        {
            Failures.Add($"{key}: primitive '{primitiveName}' for {TypeNames.Get(typeof(T))} could not be resolved.");
        }
        else
        {
            Failures.Add($"{key}: no primitive named '{primitiveName}' is registered for {TypeNames.Get(typeof(T))}.");
        }
    }
}
