using Leander.Primitives;

namespace Leander.Configuration;

// The complete, resolved set of configuration definitions of an application.
public sealed class ConfigurationContract
{
    private readonly HashSet<ConfigurationDefinition> _definitionSet;
    private readonly IReadOnlyDictionary<object, Primitive> _resolved;

    internal ConfigurationContract(
        IReadOnlyList<ConfigurationDefinition> definitions,
        PrimitiveRegistry primitives,
        IReadOnlyDictionary<object, Primitive> resolved)
    {
        Definitions = definitions;
        Primitives = primitives;
        _definitionSet = [.. definitions];
        _resolved = resolved;
    }

    public IReadOnlyList<ConfigurationDefinition> Definitions { get; }

    public PrimitiveRegistry Primitives { get; }

    public bool Contains(ConfigurationDefinition definition) => _definitionSet.Contains(definition);

    // Reads every definition from the source and returns all diagnostics.
    public IReadOnlyList<ConfigurationDiagnostic> Validate(IValueSource source)
    {
        var reader = new ConfigurationReader(this, source);

        foreach (var definition in Definitions)
        {
            definition.Read(reader);
        }

        return reader.Diagnostics;
    }

    internal Primitive<T> GetPrimitive<T>(object owner) => (Primitive<T>)_resolved[owner];
}
