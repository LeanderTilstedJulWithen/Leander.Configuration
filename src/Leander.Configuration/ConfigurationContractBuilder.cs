using System.Diagnostics.CodeAnalysis;
using System.Reflection;
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
    /// Adds the definitions marked with <see cref="RegisterConfigurationAttribute"/> in every type of <paramref name="assembly"/>,
    /// found through reflection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Types are taken in order of their full name. In each, fields come first, then properties, then methods, each in declaration order. A definition that is already registered
    /// is skipped, so explicit and attribute registration can be mixed.
    /// </para>
    /// <para>
    /// Reading a member runs its type's static initializer. Nothing throws here: a member that can't be read or isn't a definition,
    /// and an assembly without any marked type or member, are reported by <see cref="Build"/>.
    /// </para>
    /// </remarks>
    [RequiresUnreferencedCode("Finds definitions through reflection: types and members marked with RegisterConfiguration may be trimmed. Use Register or RegisterFrom(Type).")]
    public ConfigurationContractBuilder RegisterFrom(Assembly assembly)
    {
        List<ConfigurationDefinition> found = [];
        AttributeRegistration.FindIn(assembly, found, _registrationFailures);
        return RegisterNew(found);
    }

    /// <summary>
    /// Adds the definitions marked with <see cref="RegisterConfigurationAttribute"/> in <paramref name="type"/>, found through reflection:
    /// all of them when the type is marked, otherwise the marked members.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Fields come first, then properties, then methods, each in declaration order. A definition that is already registered is skipped, so explicit and attribute
    /// registration can be mixed. Nested types are not searched.
    /// </para>
    /// <para>
    /// Reading a member runs the type's static initializer. Nothing throws here: a member that can't be read or isn't a definition,
    /// and a type without the attribute, are reported by <see cref="Build"/>.
    /// </para>
    /// </remarks>
    public ConfigurationContractBuilder RegisterFrom([DynamicallyAccessedMembers(AttributeRegistration.Members)] Type type)
    {
        List<ConfigurationDefinition> found = [];
        AttributeRegistration.FindIn(type, found, _registrationFailures);
        return RegisterNew(found);
    }

    private ConfigurationContractBuilder RegisterNew(List<ConfigurationDefinition> found)
    {
        var registered = new HashSet<ConfigurationDefinition>(_definitions, ReferenceEqualityComparer.Instance);
        _definitions.AddRange(found.Where(registered.Add));
        return this;
    }
}
