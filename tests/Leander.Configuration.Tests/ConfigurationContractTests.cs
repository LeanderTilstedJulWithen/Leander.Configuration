using Leander.Primitives;
using Leander.Primitives.Normalization;
using Leander.Primitives.Validation;

namespace Leander.Configuration.Tests;

public class ConfigurationContractTests
{
    private static ConfigurationContract Contract(params ConfigurationDefinition[] definitions)
    {
        var builder = new ConfigurationContractBuilder().RegisterDefaultPrimitives();
        foreach (var definition in definitions)
        {
            builder.Register(definition);
        }

        return builder.Build();
    }

    private static IValueSource Source(params (string Key, string? Value)[] values) =>
        ValueSource.FromPairs(values.Select(pair => KeyValuePair.Create(pair.Key, pair.Value)));

    private static ConfigurationDiagnostic Single(IReadOnlyList<ConfigurationDiagnostic> diagnostics, DiagnosticSeverity severity) =>
        Assert.Single(diagnostics, d => d.Severity == severity);

    // Scalars

    [Fact]
    public void Read_Scalar_ParsesValue()
    {
        var port = ConfigurationDefinition.Define<int>("Port");

        var snapshot = Contract(port).Read(Source(("Port", "8080")));

        Assert.Equal(8080, snapshot.Get(port));
        Assert.Empty(snapshot.Diagnostics);
    }

    [Fact]
    public void Read_Scalar_InvalidValue_IsError()
    {
        var port = ConfigurationDefinition.Define<int>("Port");

        Contract(port).TryRead(Source(("Port", "abc")), out _, out var diagnostics);

        var error = Single(diagnostics, DiagnosticSeverity.Error);
        Assert.Equal("Port", error.Key);
        Assert.Equal("'abc' is not a valid Int32", error.Message);
        Assert.Same(port, error.Definition);
    }

    [Fact]
    public void Read_Scalar_EmptyValue_IsPassedToParser()
    {
        var name = ConfigurationDefinition.Define<string>("Name");
        var port = ConfigurationDefinition.Define<int>("Port");

        var success = Contract(name, port).TryRead(Source(("Name", ""), ("Port", "")), out _, out var diagnostics);

        Assert.False(success);
        Assert.Equal("Port", Single(diagnostics, DiagnosticSeverity.Error).Key);
    }

    [Fact]
    public void Read_Scalar_EmptyString_IsValue()
    {
        var name = ConfigurationDefinition.Define<string>("Name").Default("fallback");

        var snapshot = Contract(name).Read(Source(("Name", "")));

        Assert.Equal("", snapshot.Get(name));
    }

    [Fact]
    public void Read_Scalar_WithChildEntries_WarnsAndIsMissing()
    {
        var port = ConfigurationDefinition.Define<int>("Port").Default(80);

        var snapshot = Contract(port).Read(Source(("Port:0", "8080")));

        Assert.Equal(80, snapshot.Get(port));
        Assert.Equal("Port", Single(snapshot.Diagnostics, DiagnosticSeverity.Warning).Key);
    }

    [Fact]
    public void Read_Scalar_AppliesPrimitiveNormalizersThenValidators()
    {
        var primitive = PrimitiveDefinition.Define<string>()
            .Normalize(Normalizers.Trim)
            .Validate(Validators.NotEmpty);
        var name = ConfigurationDefinition.Define("Name", primitive);
        var contract = Contract(name);

        Assert.Equal("abc", contract.Read(Source(("Name", "  abc  "))).Get(name));

        contract.TryRead(Source(("Name", "   ")), out _, out var diagnostics);
        Assert.Equal("must not be empty", Single(diagnostics, DiagnosticSeverity.Error).Message);
    }

    [Fact]
    public void Read_Scalar_ReportsEveryFailingValidator()
    {
        var primitive = PrimitiveDefinition.Define<int>()
            .Validate(Validators.GreaterThan(10))
            .Validate(Validators.GreaterThan(20));
        var value = ConfigurationDefinition.Define("Value", primitive);

        Contract(value).TryRead(Source(("Value", "5")), out _, out var diagnostics);

        Assert.Equal(
            ["must be greater than 10", "must be greater than 20"],
            diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.Message));
    }

    [Fact]
    public void Read_Scalar_ThrowingNormalizer_IsErrorAndSkipsValidation()
    {
        var primitive = PrimitiveDefinition.Define<string>()
            .Normalize(Normalizers.Create<string>("boom", _ => throw new InvalidOperationException("failed hard")))
            .Validate(Validators.NotEmpty);
        var name = ConfigurationDefinition.Define("Name", primitive);

        Contract(name).TryRead(Source(("Name", "")), out _, out var diagnostics);

        Assert.Equal("normalizer 'boom' failed: failed hard", Single(diagnostics, DiagnosticSeverity.Error).Message);
    }

    [Fact]
    public void Read_Scalar_ThrowingValidator_IsError()
    {
        var primitive = PrimitiveDefinition.Define<string>()
            .Validate(Validators.Create<string>("boom", _ => throw new InvalidOperationException("failed hard")));
        var name = ConfigurationDefinition.Define("Name", primitive);

        Contract(name).TryRead(Source(("Name", "abc")), out _, out var diagnostics);

        Assert.Equal("validator 'boom' failed: failed hard", Single(diagnostics, DiagnosticSeverity.Error).Message);
    }

    [Fact]
    public void Read_Scalar_ByPrimitiveName_UsesRegisteredPrimitive()
    {
        var primitive = PrimitiveDefinition.Define<int>("Port").Validate(Validators.InRange(1, 65535));
        var port = ConfigurationDefinition.Define<int>("Port", "Port");
        var contract = new ConfigurationContractBuilder()
            .RegisterDefaultPrimitives()
            .Register(primitive)
            .Register(port)
            .Build();

        var success = contract.TryRead(Source(("Port", "0")), out _, out var diagnostics);

        Assert.False(success);
        Assert.Equal("must be between 1 and 65535", Single(diagnostics, DiagnosticSeverity.Error).Message);
    }

    // Presence and defaults

    [Fact]
    public void Read_Missing_WithoutDefault_IsRequired()
    {
        var port = ConfigurationDefinition.Define<int>("Port");

        Contract(port).TryRead(Source(), out _, out var diagnostics);

        var error = Single(diagnostics, DiagnosticSeverity.Error);
        Assert.Equal("Port", error.Key);
        Assert.Equal("value is required", error.Message);
    }

    [Fact]
    public void Read_NullValue_IsMissing()
    {
        var port = ConfigurationDefinition.Define<int>("Port").Default(80);

        var snapshot = Contract(port).Read(Source(("Port", null)));

        Assert.Equal(80, snapshot.Get(port));
    }

    [Fact]
    public void Read_Missing_WithDefault_UsesDefault()
    {
        var port = ConfigurationDefinition.Define<int>("Port").Default(80);

        var snapshot = Contract(port).Read(Source());

        Assert.Equal(80, snapshot.Get(port));
        Assert.Empty(snapshot.Diagnostics);
    }

    [Fact]
    public void Read_Present_IgnoresDefault()
    {
        var port = ConfigurationDefinition.Define<int>("Port").Default(80);

        Assert.Equal(8080, Contract(port).Read(Source(("Port", "8080"))).Get(port));
    }

    [Fact]
    public void Read_Default_GoesThroughPrimitiveNormalizers()
    {
        var primitive = PrimitiveDefinition.Define<string>().Normalize(Normalizers.Trim);
        var name = ConfigurationDefinition.Define("Name", primitive).Default("  abc  ");

        Assert.Equal("abc", Contract(name).Read(Source()).Get(name));
    }

    [Fact]
    public void Read_Default_GoesThroughPrimitiveValidators()
    {
        var primitive = PrimitiveDefinition.Define<int>().Validate(Validators.GreaterThan(0));
        var port = ConfigurationDefinition.Define("Port", primitive).Default(0);

        var success = Contract(port).TryRead(Source(), out _, out var diagnostics);

        Assert.False(success);
        Assert.Equal("must be greater than 0", Single(diagnostics, DiagnosticSeverity.Error).Message);
    }

    [Fact]
    public void Read_DefaultOfDefault_IsZero()
    {
        var port = ConfigurationDefinition.Define<int>("Port").Default(default);

        Assert.Equal(0, Contract(port).Read(Source()).Get(port));
    }

    // Documents the gap in DESIGN.md: without rules that reject it, a null default passes on a non-nullable type.
    [Fact]
    public void Read_NullDefault_WithoutRules_IsNull()
    {
        var name = ConfigurationDefinition.Define<string>("Name").Default(null!);

        Assert.Null(Contract(name).Read(Source()).Get(name));
    }

    [Fact]
    public void Read_NullDefault_RejectedByNormalizer_IsError()
    {
        var primitive = PrimitiveDefinition.Define<string>().Normalize(Normalizers.Trim);
        var name = ConfigurationDefinition.Define("Name", primitive).Default(null!);

        var success = Contract(name).TryRead(Source(), out _, out var diagnostics);

        Assert.False(success);
        Assert.StartsWith("normalizer 'trim whitespace' failed", Single(diagnostics, DiagnosticSeverity.Error).Message);
    }

    [Fact]
    public void Read_NullDefault_RejectedByValidator_IsError()
    {
        var primitive = PrimitiveDefinition.Define<string>().Validate(Validators.NotEmpty);
        var name = ConfigurationDefinition.Define("Name", primitive).Default(null!);

        var success = Contract(name).TryRead(Source(), out _, out var diagnostics);

        Assert.False(success);
        Assert.Equal("must not be empty", Single(diagnostics, DiagnosticSeverity.Error).Message);
    }

    // Indexed collections

    [Fact]
    public void Read_Indexed_ReadsElementsInNumericOrder()
    {
        var ports = ConfigurationDefinition.Define<int>("Ports").Indexed();

        var snapshot = Contract(ports).Read(Source(("Ports:2", "3"), ("Ports:10", "11"), ("Ports:0", "1"), ("Ports:1", "2")));

        Assert.Equal([1, 2, 3, 11], snapshot.Get(ports));
    }

    [Fact]
    public void Read_Indexed_Gap_Warns()
    {
        var ports = ConfigurationDefinition.Define<int>("Ports").Indexed();

        var snapshot = Contract(ports).Read(Source(("Ports:0", "1"), ("Ports:2", "3")));

        Assert.Equal([1, 3], snapshot.Get(ports));
        Assert.Equal("indices are not contiguous from 0", Single(snapshot.Diagnostics, DiagnosticSeverity.Warning).Message);
    }

    [Fact]
    public void Read_Indexed_NonIntegerIndex_IsError()
    {
        var ports = ConfigurationDefinition.Define<int>("Ports").Indexed();

        Contract(ports).TryRead(Source(("Ports:0", "1"), ("Ports:x", "2")), out _, out var diagnostics);

        var error = Single(diagnostics, DiagnosticSeverity.Error);
        Assert.Equal("Ports:x", error.Key);
        Assert.Equal("'x' is not a valid index", error.Message);
    }

    [Fact]
    public void Read_Indexed_DuplicateIndex_IsError()
    {
        var ports = ConfigurationDefinition.Define<int>("Ports").Indexed();

        Contract(ports).TryRead(Source(("Ports:0", "1"), ("Ports:1", "2"), ("Ports:01", "3")), out _, out var diagnostics);

        Assert.Equal("index 1 is defined more than once", Single(diagnostics, DiagnosticSeverity.Error).Message);
    }

    [Fact]
    public void Read_Indexed_InvalidElement_IsErrorAtElementKey()
    {
        var ports = ConfigurationDefinition.Define<int>("Ports").Indexed();

        Contract(ports).TryRead(Source(("Ports:0", "1"), ("Ports:1", "x")), out _, out var diagnostics);

        Assert.Equal("Ports:1", Single(diagnostics, DiagnosticSeverity.Error).Key);
    }

    [Fact]
    public void Read_Indexed_Missing_IsRequired()
    {
        var ports = ConfigurationDefinition.Define<int>("Ports").Indexed();

        Contract(ports).TryRead(Source(), out _, out var diagnostics);

        Assert.Equal("value is required", Single(diagnostics, DiagnosticSeverity.Error).Message);
    }

    [Fact]
    public void Read_Indexed_Missing_WithDefault_UsesDefault()
    {
        var ports = ConfigurationDefinition.Define<int>("Ports").Indexed().Default([80]);

        Assert.Equal([80], Contract(ports).Read(Source()).Get(ports));
    }

    [Fact]
    public void Read_Indexed_SingleValue_WarnsAndIsMissing()
    {
        var ports = ConfigurationDefinition.Define<int>("Ports").Indexed().Default([]);

        var snapshot = Contract(ports).Read(Source(("Ports", "1")));

        Assert.Empty(snapshot.Get(ports));
        Assert.Equal("Ports", Single(snapshot.Diagnostics, DiagnosticSeverity.Warning).Key);
    }

    [Fact]
    public void Read_Indexed_SingleValueNextToEntries_WarnsAndIsIgnored()
    {
        var ports = ConfigurationDefinition.Define<int>("Ports").Indexed();

        var snapshot = Contract(ports).Read(Source(("Ports", "9"), ("Ports:0", "1")));

        Assert.Equal([1], snapshot.Get(ports));
        Assert.Equal("has a single value that is ignored, because indexed entries are expected", Single(snapshot.Diagnostics, DiagnosticSeverity.Warning).Message);
    }

    [Fact]
    public void Read_IndexedOfIndexed_ReadsNestedEntries()
    {
        var grid = ConfigurationDefinition.Define<int>("Grid").Indexed().Indexed();

        var snapshot = Contract(grid).Read(Source(("Grid:0:0", "1"), ("Grid:0:1", "2"), ("Grid:1:0", "3")));

        var rows = snapshot.Get(grid);
        Assert.Equal(2, rows.Count);
        Assert.Equal([1, 2], rows[0]);
        Assert.Equal([3], rows[1]);
    }

    // Delimited collections

    [Fact]
    public void Read_Delimited_SplitsValue()
    {
        var ports = ConfigurationDefinition.Define<int>("Ports").Delimited();

        Assert.Equal([1, 2, 3], Contract(ports).Read(Source(("Ports", "1,2,3"))).Get(ports));
    }

    [Fact]
    public void Read_Delimited_CustomDelimiter()
    {
        var ports = ConfigurationDefinition.Define<int>("Ports").Delimited(';');

        Assert.Equal([1, 2], Contract(ports).Read(Source(("Ports", "1;2"))).Get(ports));
    }

    [Fact]
    public void Read_Delimited_InvalidItem_IsError()
    {
        var ports = ConfigurationDefinition.Define<int>("Ports").Delimited();

        var success = Contract(ports).TryRead(Source(("Ports", "1,x")), out _, out var diagnostics);

        Assert.False(success);
        Assert.Equal("Ports", Single(diagnostics, DiagnosticSeverity.Error).Key);
    }

    [Fact]
    public void Read_Delimited_ItemFailingValidation_IsErrorAtItemKey()
    {
        var primitive = PrimitiveDefinition.Define<int>().Validate(Validators.GreaterThan(0));
        var ports = ConfigurationDefinition.Define("Ports", primitive).Delimited();

        Contract(ports).TryRead(Source(("Ports", "1,0,2")), out _, out var diagnostics);

        Assert.Equal("Ports[1]", Single(diagnostics, DiagnosticSeverity.Error).Key);
    }

    [Fact]
    public void Read_Delimited_WithChildEntries_WarnsAndIsMissing()
    {
        var ports = ConfigurationDefinition.Define<int>("Ports").Delimited().Default([]);

        var snapshot = Contract(ports).Read(Source(("Ports:0", "1")));

        Assert.Empty(snapshot.Get(ports));
        Assert.Equal("Ports", Single(snapshot.Diagnostics, DiagnosticSeverity.Warning).Key);
    }

    // List-level rules

    [Fact]
    public void Read_ListValidator_AppliesToList()
    {
        var atMostOne = Validators.Create<IReadOnlyList<int>>("must have at most one element", list => list.Count <= 1);
        var ports = ConfigurationDefinition.Define<int>("Ports").Indexed().Validate(atMostOne);

        Contract(ports).TryRead(Source(("Ports:0", "1"), ("Ports:1", "2")), out _, out var diagnostics);

        var error = Single(diagnostics, DiagnosticSeverity.Error);
        Assert.Equal("Ports", error.Key);
        Assert.Equal("must have at most one element", error.Message);
    }

    [Fact]
    public void Read_ListValidator_SkippedWhenElementsFail()
    {
        var ports = ConfigurationDefinition.Define<int>("Ports").Indexed().Validate(Validators.Collections.NotEmpty);

        var success = Contract(ports).TryRead(Source(("Ports:x", "1")), out _, out var diagnostics);

        Assert.False(success);
        Assert.DoesNotContain(diagnostics, d => d.Message == "must not be empty");
    }

    [Fact]
    public void Read_ListValidator_AppliesToDefault()
    {
        var ports = ConfigurationDefinition.Define<int>("Ports").Delimited().Default([]).Validate(Validators.Collections.NotEmpty);

        Contract(ports).TryRead(Source(), out _, out var diagnostics);

        Assert.Equal("must not be empty", Single(diagnostics, DiagnosticSeverity.Error).Message);
    }

    [Fact]
    public void Read_ListNormalizer_AppliesToList()
    {
        var sorted = Normalizers.Create<IReadOnlyList<int>>("sort", list => [.. list.Order()]);
        var ports = ConfigurationDefinition.Define<int>("Ports").Delimited().Normalize(sorted);

        Assert.Equal([1, 2, 3], Contract(ports).Read(Source(("Ports", "3,1,2"))).Get(ports));
    }

    // Reading a whole contract

    [Fact]
    public void TryRead_Success_ReturnsSnapshotWithWarnings()
    {
        var port = ConfigurationDefinition.Define<int>("Port").Default(80);

        var success = Contract(port).TryRead(Source(("Port:0", "1")), out var snapshot, out var diagnostics);

        Assert.True(success);
        Assert.NotNull(snapshot);
        Assert.Equal(diagnostics, snapshot.Diagnostics);
        Assert.Single(diagnostics);
    }

    [Fact]
    public void TryRead_Failure_ReturnsNoSnapshot()
    {
        var port = ConfigurationDefinition.Define<int>("Port");

        var success = Contract(port).TryRead(Source(), out var snapshot, out _);

        Assert.False(success);
        Assert.Null(snapshot);
    }

    [Fact]
    public void TryRead_ReportsEveryDefinitionAtOnce()
    {
        var host = ConfigurationDefinition.Define<string>("Host");
        var port = ConfigurationDefinition.Define<int>("Port");
        var timeout = ConfigurationDefinition.Define<TimeSpan>("Timeout");

        Contract(host, port, timeout).TryRead(Source(("Port", "x")), out _, out var diagnostics);

        Assert.Equal(["Host", "Port", "Timeout"], diagnostics.Select(d => d.Key));
    }

    [Fact]
    public void Read_Failure_ThrowsWithAllDiagnostics()
    {
        var host = ConfigurationDefinition.Define<string>("Host");
        var port = ConfigurationDefinition.Define<int>("Port").Default(80);

        var exception = Assert.Throws<InvalidConfigurationException>(() => Contract(host, port).Read(Source(("Port:0", "1"))));

        Assert.Equal([DiagnosticSeverity.Warning, DiagnosticSeverity.Error], exception.Diagnostics.Select(d => d.Severity).Order());
    }

    [Fact]
    public void Read_Failure_MessageListsErrorsOnlyWithAlignedKeys()
    {
        var a = ConfigurationDefinition.Define<string>("A");
        var server = ConfigurationDefinition.Define<int>("Server").Default(80);
        var longKey = ConfigurationDefinition.Define<int>("LongKey");

        var exception = Assert.Throws<InvalidConfigurationException>(
            () => Contract(a, server, longKey).Read(Source(("Server:0", "1"), ("LongKey", "x"))));

        var expected = string.Join(
            Environment.NewLine,
            "Configuration is invalid:",
            "",
            "  A        value is required",
            "  LongKey  'x' is not a valid Int32");
        Assert.Equal(expected, exception.Message);
    }

    // Snapshot

    [Fact]
    public void Snapshot_ExposesContract()
    {
        var contract = Contract(ConfigurationDefinition.Define<int>("Port").Default(80));

        Assert.Same(contract, contract.Read(Source()).Contract);
    }

    [Fact]
    public void Snapshot_Get_DefinitionOutsideContract_Throws()
    {
        var port = ConfigurationDefinition.Define<int>("Port").Default(80);
        var other = ConfigurationDefinition.Define<int>("Port").Default(80);
        var snapshot = Contract(port).Read(Source());

        var exception = Assert.Throws<ArgumentException>(() => snapshot.Get(other));

        Assert.StartsWith("Port: definition is not part of the configuration contract.", exception.Message);
    }

    [Fact]
    public void Snapshot_Get_DerivedDefinition_IsOutsideContract()
    {
        var port = ConfigurationDefinition.Define<int>("Port").Default(80);
        var described = port.Describe("The port.");
        var snapshot = Contract(port).Read(Source());

        Assert.Throws<ArgumentException>(() => snapshot.Get(described));
    }
}
