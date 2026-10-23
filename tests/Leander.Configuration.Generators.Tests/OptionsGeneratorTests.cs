using System.Collections.Immutable;
using Leander.Primitives;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Leander.Configuration.Generators.Tests;

// Runs the generator on source text, for the generated code and the diagnostics.
public class OptionsGeneratorTests
{
    private const string Usings = """
        using Leander.Configuration;
        using Leander.Primitives;

        """;

    private const string Definitions = """
        public static class Definitions
        {
            public static readonly ConfigurationDefinition<string> Host = ConfigurationDefinition.Define("Host", Primitive.String);
            public static readonly ConfigurationDefinition<int> Port = ConfigurationDefinition.Define("Port", Primitive.Int32);
            public static readonly ConfigurationDefinition<string?> Name = ConfigurationDefinition.Define("Name", Primitive.String).Optional();
            public static readonly ConfigurationDefinition<int?> Count = ConfigurationDefinition.Define("Count", Primitive.Int32).Optional();
            public static readonly string Text = "";
            private static readonly ConfigurationDefinition<int> Hidden = ConfigurationDefinition.Define("Hidden", Primitive.Int32);
        }

        """;

    private static readonly ImmutableArray<MetadataReference> References =
    [
        .. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path)),
        MetadataReference.CreateFromFile(typeof(ConfigurationSnapshot).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Primitive).Assembly.Location),
    ];

    [Fact]
    public void Properties_GenerateAnObjectInitializer()
    {
        var result = Run("""
            namespace App
            {
                public sealed partial class Options
                {
                    [FromDefinition(nameof(Definitions.Host))]
                    public required string Host { get; init; }

                    [FromDefinition(nameof(Definitions.Port))]
                    public int Port { get; set; }

                    [GeneratedOptions(typeof(Definitions))]
                    public static partial Options FromSnapshot(ConfigurationSnapshot snapshot);
                }
            }
            """);

        Assert.Empty(result.Diagnostics);
        Assert.Contains("""
                    public static partial global::App.Options FromSnapshot(global::Leander.Configuration.ConfigurationSnapshot snapshot)
                    {
                        return new global::App.Options()
                        {
                            Host = snapshot.Get(global::Definitions.Host),
                            Port = snapshot.Get(global::Definitions.Port),
                        };
                    }
            """, result.Source);
    }

    [Fact]
    public void RecordParameters_GenerateConstructorArguments()
    {
        var result = Run("""
            namespace App
            {
                public sealed partial record Options(
                    [FromDefinition(nameof(Definitions.Host))] string Host,
                    [FromDefinition(nameof(Definitions.Port))] int Port)
                {
                    [GeneratedOptions(typeof(Definitions))]
                    internal static partial Options Create(ConfigurationSnapshot configuration);
                }
            }
            """);

        Assert.Empty(result.Diagnostics);
        Assert.Contains("partial record Options", result.Source);
        Assert.Contains("""
                    internal static partial global::App.Options Create(global::Leander.Configuration.ConfigurationSnapshot configuration)
                    {
                        return new global::App.Options(
                            configuration.Get(global::Definitions.Host),
                            configuration.Get(global::Definitions.Port));
                    }
            """, result.Source);
    }

    [Fact]
    public void NestedTypesAndGlobalNamespace_AreDeclaredPartial()
    {
        var result = Run("""
            public static partial class Outer
            {
                public partial record struct Options([FromDefinition(typeof(Definitions), nameof(Definitions.Port))] int Port)
                {
                    [GeneratedOptions]
                    public static partial Options FromSnapshot(ConfigurationSnapshot @class);
                }
            }
            """);

        Assert.Empty(result.Diagnostics);
        Assert.Contains("partial class Outer", result.Source);
        Assert.Contains("partial record struct Options", result.Source);
        Assert.Contains("@class.Get(global::Definitions.Port)", result.Source);
        Assert.DoesNotContain("namespace", result.Source);
    }

    [Fact]
    public void KeywordNames_AreEscaped()
    {
        var result = Run("""
            public static class Keywords
            {
                public static readonly ConfigurationDefinition<int> @event = ConfigurationDefinition.Define("Event", Primitive.Int32);
            }

            public sealed partial class Options
            {
                [FromDefinition(typeof(Keywords), "event")]
                public int @class { get; init; }

                [GeneratedOptions]
                public static partial Options FromSnapshot(ConfigurationSnapshot snapshot);
            }
            """);

        Assert.Empty(result.Diagnostics);
        Assert.Contains("@class = snapshot.Get(global::Keywords.@event),", result.Source);
    }

    [Fact]
    public void ImplicitConversion_IsAllowed()
    {
        var result = Run("""
            public sealed partial class Options
            {
                [FromDefinition(typeof(Definitions), nameof(Definitions.Port))]
                public long Port { get; init; }

                [FromDefinition(typeof(Definitions), nameof(Definitions.Host))]
                public string? Host { get; init; }

                [GeneratedOptions]
                public static partial Options FromSnapshot(ConfigurationSnapshot snapshot);
            }
            """);

        Assert.Empty(result.Diagnostics);
    }

    // Diagnostics

    [Theory]
    [InlineData("public partial Options FromSnapshot(ConfigurationSnapshot snapshot);")]
    [InlineData("public static partial Options FromSnapshot();")]
    [InlineData("public static partial Options FromSnapshot(string snapshot);")]
    [InlineData("public static partial Options FromSnapshot<T>(ConfigurationSnapshot snapshot);")]
    [InlineData("public static partial void FromSnapshot(ConfigurationSnapshot snapshot);")]
    [InlineData("public static Options FromSnapshot(ConfigurationSnapshot snapshot) => new();")]
    public void InvalidMethod_IsReported(string method)
    {
        var result = Run($$"""
            public sealed partial class Options
            {
                [GeneratedOptions(typeof(Definitions))]
                {{method}}
            }
            """);

        Assert.Equal("LCFG001", Assert.Single(result.Diagnostics).Id);
    }

    [Fact]
    public void ContainingTypeNotPartial_IsReported()
    {
        var result = Run("""
            public static class Outer
            {
                public sealed partial class Options
                {
                    [GeneratedOptions(typeof(Definitions))]
                    public static partial Options FromSnapshot(ConfigurationSnapshot snapshot);
                }
            }
            """);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("LCFG002", diagnostic.Id);
        Assert.Equal("'Outer' must be partial, because it contains a GeneratedOptions method", diagnostic.GetMessage());
    }

    [Fact]
    public void SeveralMarkedConstructors_AreReported()
    {
        var result = Run("""
            public sealed partial class Options
            {
                public Options([FromDefinition(nameof(Definitions.Host))] string host) { }
                public Options([FromDefinition(nameof(Definitions.Port))] int port) { }

                [GeneratedOptions(typeof(Definitions))]
                public static partial Options FromSnapshot(ConfigurationSnapshot snapshot);
            }
            """);

        Assert.Equal("LCFG003", Assert.Single(result.Diagnostics).Id);
    }

    [Fact]
    public void UnmarkedConstructorParameter_IsReported()
    {
        var result = Run("""
            public sealed partial record Options([FromDefinition(nameof(Definitions.Host))] string Host, int Port)
            {
                [GeneratedOptions(typeof(Definitions))]
                public static partial Options FromSnapshot(ConfigurationSnapshot snapshot);
            }
            """);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("LCFG004", diagnostic.Id);
        Assert.Equal("Parameter 'Port' of the constructor of 'Options' needs FromDefinition, like the other parameters", diagnostic.GetMessage());
    }

    [Fact]
    public void NoUsableConstructor_IsReported()
    {
        var result = Run("""
            public sealed partial class Options(string host)
            {
                public string Host { get; } = host;

                [GeneratedOptions(typeof(Definitions))]
                public static partial Options FromSnapshot(ConfigurationSnapshot snapshot);
            }
            """);

        Assert.Equal("LCFG005", Assert.Single(result.Diagnostics).Id);
    }

    [Fact]
    public void ReadOnlyProperty_IsReported()
    {
        var result = Run("""
            public sealed partial class Options
            {
                [FromDefinition(nameof(Definitions.Port))]
                public int Port { get; }

                [GeneratedOptions(typeof(Definitions))]
                public static partial Options FromSnapshot(ConfigurationSnapshot snapshot);
            }
            """);

        Assert.Equal("LCFG006", Assert.Single(result.Diagnostics).Id);
    }

    [Theory]
    [InlineData("\"Missing\"", "LCFG007", "'Definitions' has no static field or property named 'Missing'")]
    [InlineData("nameof(Definitions.Text)", "LCFG008", "'Definitions.Text' is a string, not a ConfigurationDefinition<T>")]
    [InlineData("\"Hidden\"", "LCFG009", "'Definitions.Hidden' is not accessible from 'Options'")]
    [InlineData("nameof(Definitions.Host)", "LCFG010", "'Definitions.Host' reads a string, which can't be assigned to 'Port' of type int")]
    [InlineData("nameof(Definitions.Count)", "LCFG010", "'Definitions.Count' reads a int?, which can't be assigned to 'Port' of type int")]
    public void UnusableDefinition_IsReported(string member, string id, string message)
    {
        var result = Run($$"""
            public sealed partial class Options
            {
                [FromDefinition({{member}})]
                public int Port { get; init; }

                [GeneratedOptions(typeof(Definitions))]
                public static partial Options FromSnapshot(ConfigurationSnapshot snapshot);
            }
            """);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(id, diagnostic.Id);
        Assert.Equal(message, diagnostic.GetMessage());
    }

    [Fact]
    public void OptionalDefinitionForNonNullableReference_IsAWarning()
    {
        var result = Run("""
            public sealed partial class Options
            {
                [FromDefinition(nameof(Definitions.Name))]
                public string Name { get; init; } = "";

                [GeneratedOptions(typeof(Definitions))]
                public static partial Options FromSnapshot(ConfigurationSnapshot snapshot);
            }
            """);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("LCFG011", diagnostic.Id);
        Assert.Equal(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("Name = snapshot.Get(global::Definitions.Name)!,", result.Source);
    }

    [Fact]
    public void EveryProblem_IsReportedAtOnce()
    {
        var result = Run("""
            public sealed partial class Options
            {
                [FromDefinition("Missing")]
                public int A { get; init; }

                [FromDefinition(nameof(Definitions.Text))]
                public int B { get; init; }

                [GeneratedOptions(typeof(Definitions))]
                public static partial Options FromSnapshot(ConfigurationSnapshot snapshot);
            }
            """);

        Assert.Equal(["LCFG007", "LCFG008"], result.Diagnostics.Select(diagnostic => diagnostic.Id));
        Assert.Null(result.Source);
    }

    // The generator's diagnostics, then the compiler's errors and warnings on the generated compilation, except for
    // the missing body that follows from a generator error.
    private static (List<Diagnostic> Diagnostics, string? Source) Run(string source)
    {
        var compilation = CSharpCompilation.Create(
            "App",
            [CSharpSyntaxTree.ParseText(Usings + Definitions + source, new CSharpParseOptions(LanguageVersion.Latest))],
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        CSharpGeneratorDriver.Create(new OptionsGenerator())
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out var generatorDiagnostics);

        var compilerDiagnostics = output.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity >= Microsoft.CodeAnalysis.DiagnosticSeverity.Warning)
            .Where(diagnostic => !(generatorDiagnostics.Any(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error) && diagnostic.Id == "CS8795"));

        var generated = output.SyntaxTrees.Skip(1).SingleOrDefault()?.ToString();
        return ([.. generatorDiagnostics, .. compilerDiagnostics], generated);
    }
}
