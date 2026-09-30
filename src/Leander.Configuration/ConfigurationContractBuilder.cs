using Leander.Configuration.Internal;

namespace Leander.Configuration;

public sealed class ConfigurationContractBuilder
{
    private readonly List<ConfigurationDefinition> _definitions = [];

    public ConfigurationContractBuilder Register(ConfigurationDefinition definition)
    {
        _definitions.Add(definition);
        return this;
    }

    // Checks the definitions; primitives are complete, so nothing is resolved.
    // Throws one exception listing every failure: duplicate keys, primitive name clashes,
    // and invalid presence (a null default, or a default with Optional()).
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

        List<string> failures = [.. duplicates, .. checker.NameClashes, .. checker.Failures];
        if (failures.Count > 0)
        {
            throw new InvalidOperationException(
                "Configuration contract could not be built:" + Environment.NewLine + string.Join(Environment.NewLine, failures));
        }

        return new ConfigurationContract([.. _definitions]);
    }
}
