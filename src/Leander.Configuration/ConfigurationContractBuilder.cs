using Leander.Configuration.Internal;

namespace Leander.Configuration;

/// <summary>
/// Collects configuration definitions and checks them into a <see cref="ConfigurationContract"/>.
/// </summary>
public sealed class ConfigurationContractBuilder
{
    private readonly List<ConfigurationDefinition> _definitions = [];
    private readonly List<SectionDefinition> _sections = [];

    /// <summary>
    /// Checks the definitions and builds the contract.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The definitions are invalid; lists every failure: duplicate keys, primitive name clashes,
    /// types with the same full name from different assemblies, keys and owned sections with an empty section (e.g. <c>Server:</c>),
    /// and invalid presence (a null default, or a default with <c>Optional()</c>).
    /// </exception>
    // Primitives are complete, so nothing is resolved.
    public ConfigurationContract Build()
    {
        var checker = new ContractChecker();
        var duplicates = _definitions
            .GroupBy(d => d.Key, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key}: defined more than once.");

        foreach (var definition in _definitions)
        {
            definition.Check(checker);
        }

        // Owning a section twice owns it once.
        var sections = _sections.DistinctBy(section => section.Key, StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var section in sections)
        {
            checker.CheckKey(section.Key);
        }

        var typeNames = new ContractTypeNames(checker.Types);

        List<string> failures = [.. duplicates, .. checker.NameClashes, .. typeNames.Failures, .. checker.Failures];
        if (failures.Count > 0)
        {
            throw new InvalidOperationException(
                "Configuration contract could not be built:" + Environment.NewLine + string.Join(Environment.NewLine, failures));
        }

        return new ConfigurationContract([.. _definitions], sections, typeNames);
    }

    /// <summary>
    /// Adds a definition to the contract.
    /// </summary>
    public ConfigurationContractBuilder Register(ConfigurationDefinition definition)
    {
        _definitions.Add(definition);
        return this;
    }

    /// <summary>
    /// Declares that the application owns <paramref name="section"/>: every read checks it for keys the contract
    /// doesn't define, like <see cref="ReadOptions.CheckedSections"/>.
    /// </summary>
    /// <remarks>
    /// Defining keys in a section doesn't make it owned, because a section can be shared, e.g. <c>Logging</c>.
    /// </remarks>
    public ConfigurationContractBuilder Register(SectionDefinition section)
    {
        _sections.Add(section);
        return this;
    }
}
