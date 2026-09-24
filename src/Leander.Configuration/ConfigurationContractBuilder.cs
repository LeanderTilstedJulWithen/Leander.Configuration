using Leander.Configuration.Internal;
using Leander.Primitives;

namespace Leander.Configuration;

public sealed class ConfigurationContractBuilder
{
    private readonly PrimitiveRegistryBuilder _primitives = new();
    private readonly List<ConfigurationDefinition> _definitions = [];

    public ConfigurationContractBuilder RegisterDefaultPrimitives()
    {
        _primitives.RegisterDefaults();
        return this;
    }

    public ConfigurationContractBuilder Register<T>(PrimitiveDefinition<T> primitive)
    {
        _primitives.Register(primitive);
        return this;
    }

    public ConfigurationContractBuilder RegisterFallback(IPrimitiveFallback fallback)
    {
        _primitives.RegisterFallback(fallback);
        return this;
    }

    public ConfigurationContractBuilder Register(ConfigurationDefinition definition)
    {
        _definitions.Add(definition);
        return this;
    }

    // Builds the primitives first, then resolves the primitive of every definition against them.
    // Throws if a key is defined more than once or a primitive cannot be resolved.
    public ConfigurationContract Build()
    {
        var primitives = _primitives.Build();
        var resolver = new ContractResolver(primitives);

        foreach (var duplicate in _definitions.GroupBy(d => d.Key, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
        {
            resolver.Failures.Add($"{duplicate.Key}: defined more than once.");
        }

        foreach (var definition in _definitions)
        {
            definition.Resolve(resolver);
        }

        if (resolver.Failures.Count > 0)
        {
            throw new InvalidOperationException(
                "Configuration contract could not be built:" + Environment.NewLine + string.Join(Environment.NewLine, resolver.Failures));
        }

        return new ConfigurationContract([.. _definitions], primitives, resolver.Resolved);
    }
}
