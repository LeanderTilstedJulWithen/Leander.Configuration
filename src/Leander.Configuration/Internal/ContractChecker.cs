using Leander.Primitives;
using Leander.Primitives.Internal;

namespace Leander.Configuration.Internal;

// Collects the problems of a contract while its definitions are checked.
// Primitive names must be unique per type: two different instances with the same type and name are a clash.
internal sealed class ContractChecker
{
    private readonly HashSet<(Type Type, string Name)> _clashes = [];
    private readonly Dictionary<(Type Type, string Name), Primitive> _named = [];

    // Per definition: invalid presence.
    public List<string> Failures { get; } = [];

    // Name clashes, one per type and name, in order of discovery.
    public List<string> NameClashes { get; } = [];

    // Checks the primitive, its bases, and the elements of list primitives.
    public void Check(string key, Primitive primitive)
    {
        for (Primitive? current = primitive; current is not null; current = current.Base)
        {
            var entry = (current.ValueType, current.Name);
            if (!_named.TryAdd(entry, current) && !ReferenceEquals(_named[entry], current) && _clashes.Add(entry))
            {
                NameClashes.Add($"{key}: another primitive is named {current.DisplayName}. A primitive with different rules needs a name of its own.");
            }

            if (current is IListPrimitive list)
            {
                Check(key, list.Element);
            }
        }
    }
}
