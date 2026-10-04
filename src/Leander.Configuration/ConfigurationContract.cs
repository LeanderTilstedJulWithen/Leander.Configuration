using System.Diagnostics.CodeAnalysis;
using Leander.Configuration.Descriptors;
using Leander.Configuration.Internal;

namespace Leander.Configuration;

/// <summary>
/// The complete, checked set of configuration definitions of an application.
/// Build one with <see cref="ConfigurationContractBuilder"/>.
/// </summary>
public sealed class ConfigurationContract
{
    private readonly HashSet<ConfigurationDefinition> _definitionSet;
    private readonly ContractTypeNames _typeNames;

    internal ConfigurationContract(IReadOnlyList<ConfigurationDefinition> definitions, ContractTypeNames typeNames)
    {
        Definitions = definitions;
        _definitionSet = [.. definitions];
        _typeNames = typeNames;
    }

    /// <summary>
    /// The definitions, in the order they were registered.
    /// </summary>
    public IReadOnlyList<ConfigurationDefinition> Definitions { get; }

    /// <summary>
    /// Whether <paramref name="definition"/> is part of the contract.
    /// </summary>
    public bool Contains(ConfigurationDefinition definition) => _definitionSet.Contains(definition);

    /// <summary>
    /// Creates a text-only description of every definition and the primitives they use,
    /// for documentation and contract files.
    /// </summary>
    /// <remarks>
    /// Defaults of sensitive definitions are left out. Types are named as briefly as the contract allows, e.g. <c>Billing.Status</c>
    /// next to <c>Shipping.Status</c>, and just <c>Int32</c> without a collision.
    /// </remarks>
    public ContractDescriptor CreateDescriptor()
    {
        var context = new DescriptorContext(_typeNames);
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

    /// <summary>
    /// Reads every definition from <paramref name="source"/>.
    /// </summary>
    /// <exception cref="InvalidConfigurationException">The source doesn't satisfy the contract; lists every error.</exception>
    public ConfigurationSnapshot Read(IValueSource source) => Read(source, ReadOptions.Default);

    /// <summary>
    /// Reads every definition from <paramref name="source"/>, with <paramref name="options"/>.
    /// </summary>
    /// <exception cref="InvalidConfigurationException">The source doesn't satisfy the contract; lists every error.</exception>
    public ConfigurationSnapshot Read(IValueSource source, ReadOptions options) =>
        TryRead(source, options, out var snapshot, out var diagnostics)
            ? snapshot
            : throw new InvalidConfigurationException(diagnostics);

    /// <summary>
    /// Reads every definition from <paramref name="source"/>.
    /// Returns <see langword="false"/>, and no snapshot, if there is any error.
    /// </summary>
    /// <param name="source">The values to read.</param>
    /// <param name="snapshot">The values, when the read succeeds.</param>
    /// <param name="diagnostics">Every diagnostic, whether the read succeeds or not.</param>
    public bool TryRead(
        IValueSource source,
        [NotNullWhen(true)] out ConfigurationSnapshot? snapshot,
        out IReadOnlyList<ConfigurationDiagnostic> diagnostics) =>
        TryRead(source, ReadOptions.Default, out snapshot, out diagnostics);

    /// <summary>
    /// Reads every definition from <paramref name="source"/>, with <paramref name="options"/>.
    /// Returns <see langword="false"/>, and no snapshot, if there is any error.
    /// </summary>
    /// <param name="source">The values to read.</param>
    /// <param name="options">How to read.</param>
    /// <param name="snapshot">The values, when the read succeeds.</param>
    /// <param name="diagnostics">Every diagnostic, whether the read succeeds or not.</param>
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
}
