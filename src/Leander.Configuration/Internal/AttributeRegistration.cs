using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Leander.Configuration.Internal;

// Finds the definitions marked with RegisterConfiguration in a type: all of them when the type is marked, otherwise the marked members.
// Reading a member runs the type's static initializer, so its exceptions are reported as failures, like every other problem
// the contract build finds.
internal static class AttributeRegistration
{
    public const DynamicallyAccessedMemberTypes Members =
        DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields |
        DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties |
        DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods;

    private const BindingFlags Declared =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    // Fields, then properties, then methods, each in declaration order. A definition found twice, e.g. through a property
    // returning a field, counts once.
    public static void FindIn([DynamicallyAccessedMembers(Members)] Type type, List<ConfigurationDefinition> definitions, List<string> failures)
    {
        var name = (type.FullName ?? type.Name).Replace('+', '.');
        var wholeType = type.IsDefined(typeof(RegisterConfigurationAttribute), inherit: false);
        var members = type.GetFields(Declared)
            .Concat<MemberInfo>(type.GetProperties(Declared))
            .Concat(type.GetMethods(Declared))
            .Where(member => IsMarked(member) || (wholeType && HoldsDefinition(member)))
            .OrderBy(member => member switch { FieldInfo => 0, PropertyInfo => 1, _ => 2 })
            .ThenBy(member => member.MetadataToken)
            .ToList();

        if (!wholeType && members.Count == 0)
        {
            failures.Add($"{name}: neither the type nor any of its members is marked with RegisterConfiguration.");
            return;
        }

        if (type.ContainsGenericParameters)
        {
            failures.Add($"{name}: RegisterConfiguration can't be used on an open generic type.");
            return;
        }

        var found = new HashSet<ConfigurationDefinition>(ReferenceEqualityComparer.Instance);

        foreach (var member in members)
        {
            foreach (var definition in Read($"{name}.{member.Name}", member, failures))
            {
                if (found.Add(definition))
                {
                    definitions.Add(definition);
                }
            }
        }
    }

    // On a marked type, only static fields and properties of a definition type. Backing fields are found through their property.
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

    private static Exception Innermost(Exception exception)
    {
        while (exception is TargetInvocationException or TypeInitializationException && exception.InnerException is not null)
        {
            exception = exception.InnerException;
        }

        return exception;
    }

    private static bool IsMarked(MemberInfo member) => member.IsDefined(typeof(RegisterConfigurationAttribute), inherit: false);

    private static bool IsStatic(MemberInfo member) => member switch
    {
        FieldInfo field => field.IsStatic,
        PropertyInfo property => property.GetMethod?.IsStatic ?? false,
        MethodInfo method => method.IsStatic,
        _ => false,
    };

    // A definition, or the definitions of an IEnumerable. Failures are reported, and the definitions that could be read are kept.
    private static IEnumerable<ConfigurationDefinition> Read(string name, MemberInfo member, List<string> failures)
    {
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
}
