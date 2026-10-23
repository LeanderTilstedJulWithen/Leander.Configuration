using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Leander.Configuration.Internal;

// Finds the definitions marked with RegisterConfiguration. Reading a member runs the type's static initializer,
// so its exceptions are reported as failures, like every other problem the contract build finds.
internal static class AttributeRegistration
{
    public const DynamicallyAccessedMemberTypes Members =
        DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields |
        DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties |
        DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods;

    private const BindingFlags Declared =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    // Types ordered by full name, so the registration order doesn't depend on the order files were compiled in.
    [RequiresUnreferencedCode("Types and members marked with RegisterConfiguration may be trimmed.")]
    public static void FindIn(Assembly assembly, List<ConfigurationDefinition> definitions, List<string> failures)
    {
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            failures.Add($"{assembly.GetName().Name}: types could not be loaded: {exception.LoaderExceptions.FirstOrDefault()?.Message}");
            types = [.. exception.Types.OfType<Type>()];
        }

        var marked = types
            .Where(IsMarked)
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToList();

        if (marked.Count == 0)
        {
            failures.Add($"{assembly.GetName().Name}: no types or members are marked with RegisterConfiguration.");
        }

        foreach (var type in marked)
        {
            FindIn(type, definitions, failures);
        }
    }

    // Fields, then properties, then methods, each in declaration order. A definition found twice, e.g. through a property returning a field, counts once.
    public static void FindIn([DynamicallyAccessedMembers(Members)] Type type, List<ConfigurationDefinition> definitions, List<string> failures)
    {
        if (!IsMarked(type))
        {
            failures.Add($"{Name(type)}: neither the type nor any of its members is marked with RegisterConfiguration.");
            return;
        }

        if (type.ContainsGenericParameters)
        {
            failures.Add($"{Name(type)}: RegisterConfiguration can't be used in an open generic type.");
            return;
        }

        var wholeType = type.IsDefined(typeof(RegisterConfigurationAttribute), inherit: false);
        var members = type.GetMembers(Declared)
            .Where(member => member is not Type
                && (member.IsDefined(typeof(RegisterConfigurationAttribute), inherit: false) || (wholeType && HoldsDefinition(member))))
            .OrderBy(member => member switch { FieldInfo => 0, PropertyInfo => 1, _ => 2 })
            .ThenBy(member => member.MetadataToken);
        var found = new HashSet<ConfigurationDefinition>(ReferenceEqualityComparer.Instance);

        foreach (var member in members)
        {
            foreach (var definition in Read(type, member, failures))
            {
                if (found.Add(definition))
                {
                    definitions.Add(definition);
                }
            }
        }
    }

    // On the type itself, only static fields and properties of a definition type. Backing fields are found through their property.
    private static bool HoldsDefinition(MemberInfo member) => member switch
    {
        FieldInfo field => field.IsStatic
            && typeof(ConfigurationDefinition).IsAssignableFrom(field.FieldType)
            && !field.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false),
        PropertyInfo property => property.GetMethod is { IsStatic: true }
            && property.GetIndexParameters().Length == 0
            && typeof(ConfigurationDefinition).IsAssignableFrom(property.PropertyType),
        _ => false,
    };

    private static bool IsMarked(Type type) =>
        type.IsDefined(typeof(RegisterConfigurationAttribute), inherit: false)
        || type.GetMembers(Declared).Any(member => member is not Type && member.IsDefined(typeof(RegisterConfigurationAttribute), inherit: false));

    private static string Name(Type type) => (type.FullName ?? type.Name).Replace('+', '.');

    private static IEnumerable<ConfigurationDefinition> Read(Type type, MemberInfo member, List<string> failures)
    {
        var name = $"{Name(type)}.{member.Name}";
        Func<object?> read;

        switch (member)
        {
            case FieldInfo { IsStatic: true } field:
                read = () => field.GetValue(null);
                break;
            case PropertyInfo { GetMethod: { IsStatic: true } getter } property when property.GetIndexParameters().Length == 0:
                read = () => getter.Invoke(null, null);
                break;
            case MethodInfo { IsStatic: true } method when method.GetParameters().Length == 0 && !method.ContainsGenericParameters:
                read = () => method.Invoke(null, null);
                break;
            case FieldInfo or PropertyInfo { GetMethod: not null } or MethodInfo when !IsStatic(member):
                failures.Add($"{name}: RegisterConfiguration needs a static member.");
                return [];
            default:
                failures.Add($"{name}: RegisterConfiguration needs a field, a readable property or a parameterless, non-generic method.");
                return [];
        }

        object? value;
        try
        {
            value = read();
        }
        catch (Exception exception) when (exception is TargetInvocationException or TypeInitializationException)
        {
            failures.Add($"{name}: could not be read: {Innermost(exception).Message}");
            return [];
        }

        switch (value)
        {
            case ConfigurationDefinition definition:
                return [definition];
            case IEnumerable<ConfigurationDefinition> enumerable:
                var definitions = enumerable.ToList();
                if (definitions.Any(definition => definition is null))
                {
                    failures.Add($"{name}: contains null.");
                }

                return definitions.OfType<ConfigurationDefinition>();
            case null:
                failures.Add($"{name}: is null.");
                return [];
            default:
                failures.Add($"{name}: is a {value.GetType().Name}, not a ConfigurationDefinition or IEnumerable<ConfigurationDefinition>.");
                return [];
        }
    }

    private static Exception Innermost(Exception exception)
    {
        while (exception is TargetInvocationException or TypeInitializationException && exception.InnerException is not null)
        {
            exception = exception.InnerException;
        }

        return exception;
    }

    private static bool IsStatic(MemberInfo member) => member switch
    {
        FieldInfo field => field.IsStatic,
        PropertyInfo property => property.GetMethod?.IsStatic ?? false,
        MethodBase method => method.IsStatic,
        _ => false,
    };
}
