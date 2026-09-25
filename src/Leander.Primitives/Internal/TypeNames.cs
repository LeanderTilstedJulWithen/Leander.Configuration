namespace Leander.Primitives.Internal;

// Short, readable type names for messages, e.g. IReadOnlyList<Int32>.
internal static class TypeNames
{
    public static string Get(Type type)
    {
        if (!type.IsGenericType)
        {
            return type.Name;
        }

        var name = type.Name[..type.Name.IndexOf('`')];
        return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(Get))}>";
    }
}
