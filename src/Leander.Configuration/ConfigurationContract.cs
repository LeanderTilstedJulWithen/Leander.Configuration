using System.Diagnostics.CodeAnalysis;
using Leander.Configuration.Internal;
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

    // Reads every definition from the source. Throws one exception listing every error.
    public ConfigurationSnapshot Read(IValueSource source) =>
        TryRead(source, out var snapshot, out var diagnostics)
            ? snapshot
            : throw new InvalidConfigurationException(diagnostics);

    // Reads every definition from the source. Returns false, and no snapshot, if any definition has an error.
    public bool TryRead(
        IValueSource source,
        [NotNullWhen(true)] out ConfigurationSnapshot? snapshot,
        out IReadOnlyList<ConfigurationDiagnostic> diagnostics)
    {
        var context = new ReadContext(this, source);
        var values = new Dictionary<ConfigurationDefinition, object?>();
        var failed = false;

        foreach (var definition in Definitions)
        {
            if (definition.TryRead(context, out var value))
            {
                values[definition] = value;
            }
            else
            {
                failed = true;
            }
        }

        diagnostics = context.Diagnostics;
        snapshot = failed ? null : new ConfigurationSnapshot(this, values, diagnostics);
        return !failed;
    }

    internal Primitive<T> GetPrimitive<T>(object owner) => (Primitive<T>)_resolved[owner];
}
