using Microsoft.CodeAnalysis;

namespace Leander.Configuration.Generators;

// Every problem the generator reports. Errors stop generation for that method; the compiler then reports the missing body too.
internal static class Diagnostics
{
    private const string Category = "Leander.Configuration";

    public static readonly DiagnosticDescriptor InvalidMethod = Error(
        "LCFG001",
        "Invalid options method",
        "'{0}' must be a non-generic static partial method without a body, with one ConfigurationSnapshot parameter, returning a class or struct");

    public static readonly DiagnosticDescriptor NotPartial = Error(
        "LCFG002",
        "Containing type is not partial",
        "'{0}' must be partial, because it contains a GeneratedOptions method");

    public static readonly DiagnosticDescriptor SeveralConstructors = Error(
        "LCFG003",
        "Several constructors with FromDefinition parameters",
        "'{0}' has several constructors with FromDefinition parameters; mark the parameters of one");

    public static readonly DiagnosticDescriptor UnmarkedParameter = Error(
        "LCFG004",
        "Constructor parameter without FromDefinition",
        "Parameter '{0}' of the constructor of '{1}' needs FromDefinition, like the other parameters");

    public static readonly DiagnosticDescriptor NoConstructor = Error(
        "LCFG005",
        "No constructor to use",
        "'{0}' has no parameterless constructor; mark the parameters of a constructor with FromDefinition");

    public static readonly DiagnosticDescriptor ReadOnlyProperty = Error(
        "LCFG006",
        "Property can't be set",
        "Property '{0}' has FromDefinition but no set or init accessor");

    public static readonly DiagnosticDescriptor DefinitionNotFound = Error(
        "LCFG007",
        "Definition not found",
        "'{0}' has no static field or property named '{1}'");

    public static readonly DiagnosticDescriptor NotADefinition = Error(
        "LCFG008",
        "Not a configuration definition",
        "'{0}.{1}' is a {2}, not a ConfigurationDefinition<T>");

    public static readonly DiagnosticDescriptor Inaccessible = Error(
        "LCFG009",
        "Definition is inaccessible",
        "'{0}.{1}' is not accessible from '{2}'");

    public static readonly DiagnosticDescriptor TypeMismatch = Error(
        "LCFG010",
        "Definition type doesn't match",
        "'{0}.{1}' reads a {2}, which can't be assigned to '{3}' of type {4}");

    public static readonly DiagnosticDescriptor NullableMismatch = new(
        "LCFG011",
        "Optional definition for a non-nullable member",
        "'{0}.{1}' reads a {2}, which may be null, but '{3}' is {4}",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static DiagnosticDescriptor Error(string id, string title, string message) =>
        new(id, title, message, Category, DiagnosticSeverity.Error, isEnabledByDefault: true);
}
