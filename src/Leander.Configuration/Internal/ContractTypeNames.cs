namespace Leander.Configuration.Internal;

// Names the types of one contract as briefly as the contract allows: the short name, with only as many namespace levels
// (and declaring types) as it takes to tell it apart from the other types with that short name, e.g. Billing.Status and
// Shipping.Status. Generic arguments are named the same way, each on its own. Types with the same full name can't be told
// apart at all; they are failures.
internal sealed class ContractTypeNames
{
    private readonly List<string> _failures = [];
    private readonly Dictionary<Type, string> _names = [];

    public ContractTypeNames(IEnumerable<Type> types)
    {
        var named = new HashSet<Type>();
        foreach (var type in types)
        {
            Collect(type, named);
        }

        foreach (var group in named.GroupBy(type => Path(type)[^1]))
        {
            var members = group.Select(type => (Type: type, Path: Path(type))).ToList();
            foreach (var (type, path) in members)
            {
                _names[type] = string.Join('.', path[^Depth(path, members.Where(other => other.Type != type).Select(other => other.Path))..]);
            }

            foreach (var same in members.GroupBy(member => string.Join('.', member.Path)).Where(same => same.Count() > 1))
            {
                var assemblies = string.Join(", ", same.Select(member => member.Type.Assembly.GetName().Name));
                _failures.Add($"{same.Key}: types with this name come from different assemblies ({assemblies}), so they can't be told apart.");
            }
        }
    }

    // Types with the same full name, one failure per name.
    public IReadOnlyList<string> Failures => _failures;

    // E.g. "Int32", "Billing.Status", "IReadOnlyList<Billing.Status>". A type outside the contract gets its short name.
    public string Get(Type type)
    {
        if (type.IsArray)
        {
            return $"{Get(type.GetElementType()!)}[]";
        }

        if (type.IsGenericType)
        {
            return $"{Name(type.GetGenericTypeDefinition())}<{string.Join(", ", type.GetGenericArguments().Select(Get))}>";
        }

        return Name(type);
    }

    // The types that get a name of their own: non-generic types and generic type definitions, not arrays or constructed types.
    private static void Collect(Type type, HashSet<Type> named)
    {
        if (type.IsArray)
        {
            Collect(type.GetElementType()!, named);
            return;
        }

        if (type.IsGenericType)
        {
            named.Add(type.GetGenericTypeDefinition());
            foreach (var argument in type.GetGenericArguments())
            {
                Collect(argument, named);
            }

            return;
        }

        named.Add(type);
    }

    // The fewest trailing segments of path that no other path ends with; all of them when another path is the same.
    private static int Depth(string[] path, IEnumerable<string[]> others)
    {
        var depth = 1;
        foreach (var other in others)
        {
            while (depth < path.Length && path[^depth..].AsSpan().SequenceEqual(other.Length >= depth ? other[^depth..] : other))
            {
                depth++;
            }
        }

        return depth;
    }

    private string Name(Type type) => _names.TryGetValue(type, out var name) ? name : ShortName(type);

    // Namespace segments, declaring types, then the type itself, without generic arity: ["MyApp", "Outer", "Status"].
    private static string[] Path(Type type)
    {
        var names = new List<string>();
        for (var current = type; current is not null; current = current.DeclaringType)
        {
            names.Insert(0, ShortName(current));
        }

        return [.. (type.Namespace?.Split('.') ?? []), .. names];
    }

    private static string ShortName(Type type) => type.Name.IndexOf('`') is var tick and >= 0 ? type.Name[..tick] : type.Name;
}
