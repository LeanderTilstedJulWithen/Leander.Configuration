using Leander.Primitives;

namespace Leander.Configuration.Internal;

// Collects the problems of a contract while its definitions are checked.
// Primitive names must be unique per type: two different instances with the same type and name are a clash.
internal sealed class ContractChecker
{
    private readonly Dictionary<(Type Type, string Name), Primitive> _named = [];
    private readonly HashSet<(Type Type, string Name)> _clashes = [];

    // Name clashes, one per type and name, in order of discovery.
    public List<string> NameClashes { get; } = [];

    // Per definition: invalid presence and invalid Delimited() uses.
    public List<string> Failures { get; } = [];

    // Checks the primitive and its bases.
    public void Check(string key, Primitive primitive)
    {
        for (Primitive? current = primitive; current is not null; current = current.Base)
        {
            if (current.Name is not { } name)
            {
                continue;
            }

            var entry = (current.ValueType, name);
            if (!_named.TryAdd(entry, current) && !ReferenceEquals(_named[entry], current) && _clashes.Add(entry))
            {
                NameClashes.Add(
                    $"{key}: another primitive is named {current.DisplayName}. Use Primitive.DeriveFrom to add rules under a new name.");
            }
        }
    }
}
