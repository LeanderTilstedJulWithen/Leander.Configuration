using System.Collections;

namespace Leander.Configuration;

/// <summary>
/// The values of every definition in a contract, read from a source.
/// </summary>
/// <remarks>
/// A snapshot only exists when the source satisfies the contract: every value is present or defaulted,
/// parsed, normalized and valid. Getting a value cannot fail.
/// </remarks>
public sealed class ConfigurationSnapshot
{
    private readonly IReadOnlyDictionary<ConfigurationDefinition, object?> _values;

    internal ConfigurationSnapshot(
        ConfigurationContract contract,
        IReadOnlyDictionary<ConfigurationDefinition, object?> values,
        IReadOnlyList<ConfigurationDiagnostic> diagnostics)
    {
        Contract = contract;
        _values = values;
        Diagnostics = diagnostics;
    }

    /// <summary>
    /// The contract the snapshot was read with.
    /// </summary>
    public ConfigurationContract Contract { get; }

    /// <summary>
    /// Warnings and traces produced while reading. Never contains errors.
    /// </summary>
    public IReadOnlyList<ConfigurationDiagnostic> Diagnostics { get; }

    /// <summary>
    /// Gets the value of <paramref name="definition"/>.
    /// </summary>
    /// <exception cref="ArgumentException">The definition is not part of the contract.</exception>
    public T Get<T>(ConfigurationDefinition<T> definition) =>
        _values.TryGetValue(definition, out var value)
            ? (T)value!
            : throw new ArgumentException($"{definition.Key}: definition is not part of the configuration contract.", nameof(definition));

    // Whether other was read with the same contract and holds equal values: collections item by item, the rest by Equals.
    // Diagnostics don't count. Values that can't be compared, e.g. dictionaries of lists, count as different.
    internal bool HasSameValues(ConfigurationSnapshot other) =>
        ReferenceEquals(Contract, other.Contract)
        && Contract.Definitions.All(definition => ValueEquals(_values[definition], other._values[definition]));

    private static bool ItemsEqual(IEnumerable left, IEnumerable right)
    {
        var rightItems = right.GetEnumerator();

        foreach (var item in left)
        {
            if (!rightItems.MoveNext() || !ValueEquals(item, rightItems.Current))
            {
                return false;
            }
        }

        return !rightItems.MoveNext();
    }

    private static bool ValueEquals(object? left, object? right) => (left, right) switch
    {
        (string, _) or (_, string) => Equals(left, right),
        (IEnumerable leftItems, IEnumerable rightItems) => ItemsEqual(leftItems, rightItems),
        _ => Equals(left, right),
    };
}
