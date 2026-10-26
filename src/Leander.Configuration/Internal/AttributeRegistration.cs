using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Leander.Configuration.Internal;

// Finds the definitions in a type marked with RegisterConfiguration. Reading a member runs the type's static initializer,
// so its exceptions are reported as failures, like every other problem the contract build finds.
internal static class AttributeRegistration
{
    public const DynamicallyAccessedMemberTypes Members =
        DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields |
        DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties;

    private const BindingFlags Static = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly;

    // Fields, then properties, each in declaration order. A definition found twice, e.g. through a property returning a field, counts once.
    public static void FindIn([DynamicallyAccessedMembers(Members)] Type type, List<ConfigurationDefinition> definitions, List<string> failures)
    {
        var name = (type.FullName ?? type.Name).Replace('+', '.');

        if (!type.IsDefined(typeof(RegisterConfigurationAttribute), inherit: false))
        {
            failures.Add($"{name}: is not marked with RegisterConfiguration.");
            return;
        }

        if (type.ContainsGenericParameters)
        {
            failures.Add($"{name}: RegisterConfiguration can't be used on an open generic type.");
            return;
        }

        // Backing fields of auto-properties are found through their property.
        var fields = type.GetFields(Static)
            .Where(field => typeof(ConfigurationDefinition).IsAssignableFrom(field.FieldType)
                && !field.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
            .OrderBy(field => field.MetadataToken)
            .Select(field => (field.Name, Read: (Func<object?>)(() => field.GetValue(null))));
        var properties = type.GetProperties(Static)
            .Where(property => property.GetMethod is not null
                && property.GetIndexParameters().Length == 0
                && typeof(ConfigurationDefinition).IsAssignableFrom(property.PropertyType))
            .OrderBy(property => property.MetadataToken)
            .Select(property => (property.Name, Read: (Func<object?>)(() => property.GetMethod!.Invoke(null, null))));
        var found = new HashSet<ConfigurationDefinition>(ReferenceEqualityComparer.Instance);

        foreach (var (member, read) in fields.Concat(properties))
        {
            object? value;
            try
            {
                value = read();
            }
            catch (Exception exception) when (exception is TargetInvocationException or TypeInitializationException)
            {
                failures.Add($"{name}.{member}: could not be read: {Innermost(exception).Message}");
                continue;
            }

            if (value is not ConfigurationDefinition definition)
            {
                failures.Add($"{name}.{member}: is null.");
            }
            else if (found.Add(definition))
            {
                definitions.Add(definition);
            }
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
}
