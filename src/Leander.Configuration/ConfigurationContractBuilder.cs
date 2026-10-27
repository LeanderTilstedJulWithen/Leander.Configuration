using System.Diagnostics.CodeAnalysis;
using Leander.Configuration.Internal;

namespace Leander.Configuration;

/// <summary>
/// Collects configuration definitions and checks them into a <see cref="ConfigurationContract"/>.
/// </summary>
public sealed class ConfigurationContractBuilder
{
    private readonly List<ConfigurationDefinition> _definitions = [];
    private readonly List<string> _registrationFailures = [];

    /// <summary>
    /// Checks the definitions and builds the contract.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The definitions are invalid; lists every failure: definitions <c>RegisterFrom</c> could not read, duplicate keys, primitive name clashes,
    /// types with the same full name from different assemblies, and invalid presence (a null default, or a default with <c>Optional()</c>).
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

        var typeNames = new ContractTypeNames(checker.Types);

        List<string> failures = [.. _registrationFailures, .. duplicates, .. checker.NameClashes, .. typeNames.Failures, .. checker.Failures];
        if (failures.Count > 0)
        {
            throw new InvalidOperationException(
                "Configuration contract could not be built:" + Environment.NewLine + string.Join(Environment.NewLine, failures));
        }

        return new ConfigurationContract([.. _definitions], typeNames);
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
    /// Adds every definition in <paramref name="type"/>, which is marked with <see cref="RegisterConfigurationAttribute"/>:
    /// its static fields and properties whose type is a <see cref="ConfigurationDefinition"/>, of any accessibility, found through reflection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Fields come first, then properties, each in declaration order. A definition that is already registered is skipped,
    /// so explicit and attribute registration can be mixed.
    /// </para>
    /// <para>
    /// Reading a member runs the type's static initializer. Nothing throws here: a type without the attribute, an open generic type,
    /// and a member that can't be read or is null are reported by <see cref="Build"/>.
    /// </para>
    /// </remarks>
    public ConfigurationContractBuilder RegisterFrom([DynamicallyAccessedMembers(AttributeRegistration.Members)] Type type)
    {
        List<ConfigurationDefinition> found = [];
        AttributeRegistration.FindIn(type, found, _registrationFailures);

        var registered = new HashSet<ConfigurationDefinition>(_definitions, ReferenceEqualityComparer.Instance);
        _definitions.AddRange(found.Where(registered.Add));
        return this;
    }
}
