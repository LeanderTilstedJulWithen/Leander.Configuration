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
    public ConfigurationSnapshot Read(IValueSource source) => Read(source, ReadOptions.Default);

    public ConfigurationSnapshot Read(IValueSource source, ReadOptions options) =>
        TryRead(source, options, out var snapshot, out var diagnostics)
            ? snapshot
            : throw new InvalidConfigurationException(diagnostics);

    // Reads every definition from the source. Returns false, and no snapshot, if there is any error.
    public bool TryRead(
        IValueSource source,
        [NotNullWhen(true)] out ConfigurationSnapshot? snapshot,
        out IReadOnlyList<ConfigurationDiagnostic> diagnostics) =>
        TryRead(source, ReadOptions.Default, out snapshot, out diagnostics);

    public bool TryRead(
        IValueSource source,
        ReadOptions options,
        [NotNullWhen(true)] out ConfigurationSnapshot? snapshot,
        out IReadOnlyList<ConfigurationDiagnostic> diagnostics)
    {
        var context = new ReadContext(this, source, options);
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

        context.IsSensitive = false;
        ReportUnknownKeys(context);

        // With WarningsAsErrors, a warning fails the read even when every definition was read.
        failed |= context.HasErrors;
        diagnostics = context.Diagnostics;
        snapshot = failed ? null : new ConfigurationSnapshot(this, values, diagnostics);
        return !failed;
    }

    // Walks the checked sections and reports every value the contract doesn't define. A defined key isn't walked into:
    // its reader reports entries in the wrong form, e.g. child entries under a scalar.
    private void ReportUnknownKeys(ReadContext context)
    {
        if (context.Options.CheckedSections.Count == 0)
        {
            return;
        }

        var known = new HashSet<string>(Definitions.Select(definition => definition.Key), StringComparer.OrdinalIgnoreCase);

        foreach (var section in context.Options.CheckedSections)
        {
            Walk(section);
        }

        void Walk(string key)
        {
            if (known.Contains(key))
            {
                return;
            }

            if (context.Source.GetValue(key) is not null)
            {
                context.Report(DiagnosticSeverity.Warning, key, "is not in the configuration contract", null);
            }

            foreach (var name in context.Source.GetChildNames(key))
            {
                Walk($"{key}{ValueSource.KeySeparator}{name}");
            }
        }
    }

    // A text-only description of every definition and the primitives they use,
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
