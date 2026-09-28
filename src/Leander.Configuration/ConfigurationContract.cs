using System.Diagnostics.CodeAnalysis;
using Leander.Configuration.Descriptors;
using Leander.Configuration.Internal;

namespace Leander.Configuration;

// The complete, checked set of configuration definitions of an application.
public sealed class ConfigurationContract
{
    private readonly HashSet<ConfigurationDefinition> _definitionSet;

    internal ConfigurationContract(IReadOnlyList<ConfigurationDefinition> definitions)
    {
        Definitions = definitions;
        _definitionSet = [.. definitions];
    }

    public IReadOnlyList<ConfigurationDefinition> Definitions { get; }

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
            context.IsSensitive = definition.IsSensitive;

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

    // A text-only description of every definition and the named primitives they use,
    // for documentation and contract files. Defaults of sensitive definitions are left out.
    public ContractDescriptor CreateDescriptor()
    {
        var context = new DescriptorContext();
        var definitions = new List<DefinitionDescriptor>(Definitions.Count);

        foreach (var definition in Definitions)
        {
            context.IsSensitive = definition.IsSensitive;
            definitions.Add(new DefinitionDescriptor(
                definition.Key,
                definition.Description,
                definition.IsSensitive,
                definition.CreateValueDescriptor(context)));
        }

        return new ContractDescriptor(definitions, context.Primitives);
    }
}
