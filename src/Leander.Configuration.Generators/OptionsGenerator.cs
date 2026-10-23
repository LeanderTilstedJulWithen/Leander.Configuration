using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Leander.Configuration.Generators;

/// <summary>
/// Writes the body of every <c>static partial</c> method marked with <c>GeneratedOptions</c>: the explicit construction of the
/// options type, with each constructor parameter and property marked with <c>FromDefinition</c> read from the snapshot.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class OptionsGenerator : IIncrementalGenerator
{
    private const string DefinitionName = "Leander.Configuration.ConfigurationDefinition`1";
    private const string FromDefinitionName = "Leander.Configuration.FromDefinitionAttribute";
    private const string GeneratedOptionsName = "Leander.Configuration.GeneratedOptionsAttribute";
    private const string SnapshotName = "Leander.Configuration.ConfigurationSnapshot";

    private static readonly SymbolDisplayFormat Qualified =
        SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var results = context.SyntaxProvider.ForAttributeWithMetadataName(
            GeneratedOptionsName,
            static (node, _) => node is MethodDeclarationSyntax,
            static (attributeContext, cancellationToken) => Generate(attributeContext, cancellationToken));

        context.RegisterSourceOutput(results, static (output, result) =>
        {
            foreach (var diagnostic in result.Diagnostics)
            {
                output.ReportDiagnostic(diagnostic.ToDiagnostic());
            }

            if (result.HintName is not null && result.Source is not null)
            {
                output.AddSource(result.HintName, result.Source);
            }
        });
    }

    private static OptionsResult Generate(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        var syntax = (MethodDeclarationSyntax)context.TargetNode;
        var method = (IMethodSymbol)context.TargetSymbol;
        var compilation = context.SemanticModel.Compilation;
        var diagnostics = new List<DiagnosticInfo>();
        OptionsResult Failed() => new(null, null, new([.. diagnostics]));

        var snapshot = compilation.GetTypeByMetadataName(SnapshotName);
        var definition = compilation.GetTypeByMetadataName(DefinitionName);
        var fromDefinition = compilation.GetTypeByMetadataName(FromDefinitionName);
        if (snapshot is null || definition is null || fromDefinition is null)
        {
            return Failed();
        }

        if (!method.IsStatic
            || method.ReturnsVoid
            || !method.IsPartialDefinition
            || method.PartialImplementationPart is not null
            || method.IsGenericMethod
            || method.Parameters.Length != 1
            || !SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, snapshot)
            || method.ReturnType is not INamedTypeSymbol { TypeKind: TypeKind.Class or TypeKind.Struct, IsAbstract: false } options)
        {
            diagnostics.Add(DiagnosticInfo.Create(Diagnostics.InvalidMethod, syntax.Identifier.GetLocation(), method.Name));
            return Failed();
        }

        var containers = new List<TypeDeclarationSyntax>();
        for (var type = method.ContainingType; type is not null; type = type.ContainingType)
        {
            var declaration = type.DeclaringSyntaxReferences
                .Select(reference => reference.GetSyntax(cancellationToken))
                .OfType<TypeDeclarationSyntax>()
                .FirstOrDefault();

            if (declaration is null || !declaration.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                diagnostics.Add(DiagnosticInfo.Create(Diagnostics.NotPartial, type.Locations.FirstOrDefault(), type.Name));
                return Failed();
            }

            containers.Insert(0, declaration);
        }

        var attributeArguments = context.Attributes[0].ConstructorArguments;
        var definitions = attributeArguments.Length == 1 && attributeArguments[0].Value is INamedTypeSymbol named ? named : method.ContainingType;
        var resolver = new Resolver(compilation, method, definition, fromDefinition, definitions, diagnostics);

        var arguments = ConstructorArguments(options, resolver, diagnostics);
        var properties = PropertyAssignments(options, resolver, diagnostics);
        if (arguments is null || diagnostics.Any(diagnostic => diagnostic.Descriptor.DefaultSeverity == DiagnosticSeverity.Error))
        {
            return Failed();
        }

        var source = Write(syntax, method, options, containers, arguments, properties);
        var hintName = $"{method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "")}.{method.Name}.g.cs"
            .Replace('<', '[')
            .Replace('>', ']');
        return new OptionsResult(hintName, source, new([.. diagnostics]));
    }

    // The constructor whose parameters are marked, or the parameterless one. Null when there is no constructor to use.
    private static List<string>? ConstructorArguments(INamedTypeSymbol options, Resolver resolver, List<DiagnosticInfo> diagnostics)
    {
        var constructors = options.InstanceConstructors.Where(constructor => !IsCopyConstructor(options, constructor)).ToList();
        var marked = constructors.Where(constructor => constructor.Parameters.Any(parameter => resolver.FindAttribute(parameter) is not null)).ToList();

        if (marked.Count > 1)
        {
            diagnostics.Add(DiagnosticInfo.Create(Diagnostics.SeveralConstructors, options.Locations.FirstOrDefault(), options.Name));
            return null;
        }

        if (marked.Count == 0)
        {
            if (!constructors.Any(constructor => constructor.Parameters.IsEmpty))
            {
                diagnostics.Add(DiagnosticInfo.Create(Diagnostics.NoConstructor, options.Locations.FirstOrDefault(), options.Name));
                return null;
            }

            return [];
        }

        var arguments = new List<string>();
        foreach (var parameter in marked[0].Parameters)
        {
            if (resolver.FindAttribute(parameter) is { } attribute)
            {
                arguments.Add(resolver.Read(attribute, parameter.Type, parameter.Name, parameter.Locations.FirstOrDefault()) ?? "default!");
            }
            else
            {
                diagnostics.Add(DiagnosticInfo.Create(Diagnostics.UnmarkedParameter, parameter.Locations.FirstOrDefault(), parameter.Name, options.Name));
            }
        }

        return arguments;
    }

    // A record's copy constructor takes the record itself.
    private static bool IsCopyConstructor(INamedTypeSymbol options, IMethodSymbol constructor) =>
        options.IsRecord
        && constructor.Parameters.Length == 1
        && SymbolEqualityComparer.Default.Equals(constructor.Parameters[0].Type, options);

    private static string Literal(string name) =>
        SyntaxFacts.GetKeywordKind(name) == SyntaxKind.None && SyntaxFacts.GetContextualKeywordKind(name) == SyntaxKind.None ? name : "@" + name;

    // Marked properties of the type and its bases, the most derived first; an overridden property counts once.
    private static List<(string Name, string Value)> PropertyAssignments(INamedTypeSymbol options, Resolver resolver, List<DiagnosticInfo> diagnostics)
    {
        var assignments = new List<(string Name, string Value)>();
        var seen = new HashSet<string>();

        for (var type = options; type is not null; type = type.BaseType)
        {
            foreach (var property in type.GetMembers().OfType<IPropertySymbol>())
            {
                if (property.IsStatic || !seen.Add(property.Name) || resolver.FindAttribute(property) is not { } attribute)
                {
                    continue;
                }

                if (property.SetMethod is null)
                {
                    diagnostics.Add(DiagnosticInfo.Create(Diagnostics.ReadOnlyProperty, property.Locations.FirstOrDefault(), property.Name));
                    continue;
                }

                if (resolver.Read(attribute, property.Type, property.Name, property.Locations.FirstOrDefault()) is { } value)
                {
                    assignments.Add((property.Name, value));
                }
            }
        }

        return assignments;
    }

    private static string Write(
        MethodDeclarationSyntax syntax,
        IMethodSymbol method,
        INamedTypeSymbol options,
        List<TypeDeclarationSyntax> containers,
        List<string> arguments,
        List<(string Name, string Value)> properties)
    {
        var source = new StringBuilder();
        source.AppendLine("// <auto-generated/>");
        source.AppendLine("#nullable enable");
        source.AppendLine();

        var indent = "";
        var ns = method.ContainingNamespace;
        if (!ns.IsGlobalNamespace)
        {
            source.AppendLine($"namespace {ns.ToDisplayString()}");
            source.AppendLine("{");
            indent = "    ";
        }

        foreach (var container in containers)
        {
            var keyword = container is RecordDeclarationSyntax record && record.ClassOrStructKeyword.Text is { Length: > 0 } kind
                ? $"record {kind}"
                : container.Keyword.Text;
            source.AppendLine($"{indent}partial {keyword} {container.Identifier.Text}{container.TypeParameterList}");
            source.AppendLine($"{indent}{{");
            indent += "    ";
        }

        var modifiers = string.Join(" ", syntax.Modifiers.Select(modifier => modifier.Text));
        var parameter = Literal(method.Parameters[0].Name);
        var created = options.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        source.AppendLine($"{indent}[global::System.CodeDom.Compiler.GeneratedCode(\"Leander.Configuration.Generators\", \"{typeof(OptionsGenerator).Assembly.GetName().Version}\")]");
        source.AppendLine($"{indent}{modifiers} {method.ReturnType.ToDisplayString(Qualified)} {method.Name}({method.Parameters[0].Type.ToDisplayString(Qualified)} {parameter})");
        source.AppendLine($"{indent}{{");
        source.Append($"{indent}    return new {created}(");

        for (var i = 0; i < arguments.Count; i++)
        {
            source.AppendLine(i == 0 ? "" : ",");
            source.Append($"{indent}        {string.Format(arguments[i], parameter)}");
        }

        if (properties.Count == 0)
        {
            source.AppendLine(");");
        }
        else
        {
            source.AppendLine(")");
            source.AppendLine($"{indent}    {{");
            foreach (var (name, value) in properties)
            {
                source.AppendLine($"{indent}        {Literal(name)} = {string.Format(value, parameter)},");
            }

            source.AppendLine($"{indent}    }};");
        }

        source.AppendLine($"{indent}}}");

        while (indent.Length > 0)
        {
            indent = indent.Substring(4);
            source.AppendLine($"{indent}}}");
        }

        return source.ToString();
    }

    // Finds and checks the definition a FromDefinition attribute names, and writes the read from the snapshot.
    private sealed class Resolver(
        Compilation compilation,
        IMethodSymbol method,
        INamedTypeSymbol definition,
        INamedTypeSymbol fromDefinition,
        INamedTypeSymbol definitions,
        List<DiagnosticInfo> diagnostics)
    {
        public AttributeData? FindAttribute(ISymbol symbol) =>
            symbol.GetAttributes().FirstOrDefault(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, fromDefinition));

        // "{0}" in the result is the snapshot parameter. Null when the definition can't be used; the reason is in the diagnostics.
        public string? Read(AttributeData attribute, ITypeSymbol target, string targetName, Location? targetLocation)
        {
            var location = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? targetLocation;
            var arguments = attribute.ConstructorArguments;
            var (holder, name) = arguments.Length switch
            {
                1 => (definitions, arguments[0].Value as string),
                2 => (arguments[0].Value as INamedTypeSymbol, arguments[1].Value as string),
                _ => (null, null),
            };

            if (holder is null || name is null)
            {
                return null;   // The compiler reports a malformed attribute.
            }

            var member = holder.GetMembers(name).FirstOrDefault(candidate => candidate is IFieldSymbol { IsStatic: true } or IPropertySymbol { IsStatic: true });
            if (member is null)
            {
                diagnostics.Add(DiagnosticInfo.Create(Diagnostics.DefinitionNotFound, location, holder.ToDisplayString(), name));
                return null;
            }

            var memberType = member is IFieldSymbol field ? field.Type : ((IPropertySymbol)member).Type;
            if (memberType is not INamedTypeSymbol named || !SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, definition))
            {
                diagnostics.Add(DiagnosticInfo.Create(Diagnostics.NotADefinition, location, holder.ToDisplayString(), name, memberType.ToDisplayString()));
                return null;
            }

            if (!compilation.IsSymbolAccessibleWithin(member, method.ContainingType))
            {
                diagnostics.Add(DiagnosticInfo.Create(Diagnostics.Inaccessible, location, holder.ToDisplayString(), name, method.ContainingType.ToDisplayString()));
                return null;
            }

            var value = named.TypeArguments[0];
            if (!compilation.ClassifyConversion(value, target).IsImplicit)
            {
                diagnostics.Add(DiagnosticInfo.Create(
                    Diagnostics.TypeMismatch, location, holder.ToDisplayString(), name, value.ToDisplayString(), targetName, target.ToDisplayString()));
                return null;
            }

            var read = $"{{0}}.Get({holder.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{Literal(name)})";

            // Reported here, on the attribute, rather than by the compiler in generated code; suppressing it accepts the null.
            if (value.IsReferenceType
                && named.TypeArgumentNullableAnnotations[0] == NullableAnnotation.Annotated
                && target.NullableAnnotation == NullableAnnotation.NotAnnotated)
            {
                diagnostics.Add(DiagnosticInfo.Create(
                    Diagnostics.NullableMismatch, location, holder.ToDisplayString(), name, value.ToDisplayString(), targetName, target.ToDisplayString()));
                return read + "!";
            }

            return read;
        }
    }
}
