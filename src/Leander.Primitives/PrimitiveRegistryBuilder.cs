using Leander.Primitives.Internal;
using Leander.Primitives.Parsing;

namespace Leander.Primitives;

public sealed class PrimitiveRegistryBuilder
{
    private readonly Dictionary<(Type Type, string? Name), PrimitiveDefinition> _definitions = [];
    private readonly List<IPrimitiveFallback> _fallbacks = [];

    // Registers a default primitive for each built-in type, plus the named variants from Leander.Primitives.Parsing.
    // Enums are covered by a fallback.
    public PrimitiveRegistryBuilder RegisterDefaults()
    {
        return Register(PrimitiveDefinition.Define(Converters.String))
            .Register(PrimitiveDefinition.Define(Converters.Boolean))
            .Register(PrimitiveDefinition.Define(Converters.Byte))
            .Register(PrimitiveDefinition.Define(Converters.SByte))
            .Register(PrimitiveDefinition.Define(Converters.Int16))
            .Register(PrimitiveDefinition.Define(Converters.UInt16))
            .Register(PrimitiveDefinition.Define(Converters.Int32))
            .Register(PrimitiveDefinition.Define("Hex", Converters.Int32Hex))
            .Register(PrimitiveDefinition.Define(Converters.UInt32))
            .Register(PrimitiveDefinition.Define("Hex", Converters.UInt32Hex))
            .Register(PrimitiveDefinition.Define(Converters.Int64))
            .Register(PrimitiveDefinition.Define(Converters.UInt64))
            .Register(PrimitiveDefinition.Define(Converters.Single))
            .Register(PrimitiveDefinition.Define(Converters.Double))
            .Register(PrimitiveDefinition.Define(Converters.Decimal))
            .Register(PrimitiveDefinition.Define(Converters.Guid))
            .Register(PrimitiveDefinition.Define(Converters.Uri))
            .Register(PrimitiveDefinition.Define(Converters.TimeSpan))
            .Register(PrimitiveDefinition.Define(Converters.DateTimeUtc))
            .Register(PrimitiveDefinition.Define("Local", Converters.DateTimeLocal))
            .Register(PrimitiveDefinition.Define(Converters.DateTimeOffset))
            .RegisterFallback(new EnumPrimitiveFallback());
    }

    // A definition without a name becomes the default for its type.
    public PrimitiveRegistryBuilder Register<T>(PrimitiveDefinition<T> definition)
    {
        _definitions[(typeof(T), definition.Name)] = definition;
        return this;
    }

    public PrimitiveRegistryBuilder RegisterFallback(IPrimitiveFallback fallback)
    {
        _fallbacks.Add(fallback);
        return this;
    }

    // Resolves defaults first, then named definitions against a registry holding only those defaults (and the fallbacks).
    // Throws if any definition has no converter and no default converter for its type.
    public PrimitiveRegistry Build()
    {
        var fallbacks = _fallbacks.ToList();
        var defaults = new Dictionary<(Type Type, string? Name), Primitive>();
        var failures = new List<string>();

        foreach (var ((type, name), definition) in _definitions.Where(entry => entry.Key.Name is null))
        {
            if (definition.ResolveIn(registry: null) is { } primitive)
            {
                defaults[(type, name)] = primitive;
            }
            else
            {
                failures.Add($"The default primitive for {type} has no converter.");
            }
        }

        var defaultsRegistry = new PrimitiveRegistry(defaults, fallbacks);
        var primitives = new Dictionary<(Type Type, string? Name), Primitive>(defaults);

        foreach (var ((type, name), definition) in _definitions.Where(entry => entry.Key.Name is not null))
        {
            if (definition.ResolveIn(defaultsRegistry) is { } primitive)
            {
                primitives[(type, name)] = primitive;
            }
            else
            {
                failures.Add($"The primitive '{name}' for {type} has no converter, and no default converter is registered for {type}.");
            }
        }

        if (failures.Count > 0)
        {
            throw new InvalidOperationException(
                "Primitives could not be resolved:" + Environment.NewLine + string.Join(Environment.NewLine, failures));
        }

        return new PrimitiveRegistry(primitives, fallbacks);
    }
}
