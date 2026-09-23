namespace Leander.Configuration.Internal;

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
