using Leander.Primitives;
using Leander.Primitives.Normalization;
using Leander.Primitives.Parsing;
using Leander.Primitives.Validation;

namespace Leander.Configuration.Tests;

public class ConfigurationContractTests
{
    private static readonly ListPrimitive<string> Hosts = new("Hosts", Primitive.String)
    {
        Validators = [Validators.Collections.NotEmpty<string>()],
    };

    private static readonly Primitive<int> Port = new("Port", Converters.Int32)
    {
        Validators = [Validators.InRange(1, 65535)],
    };

    // Scalars

    [Fact]
    public void Read_Scalar_ParsesValue()
    {
        var port = ConfigurationDefinition.Define("Port", Port);

        var snapshot = Contract(port).Read(Source(("Port", "8080")));

        Assert.Equal(8080, snapshot.Get(port));
        Assert.Empty(snapshot.Diagnostics);
    }

    [Fact]
    public void Read_Scalar_InvalidValue_IsErrorWithKeyAndDefinition()
    {
        var port = ConfigurationDefinition.Define("Port", Port);

        var error = Single(ReadFailure(Contract(port), Source(("Port", "abc"))), DiagnosticSeverity.Error);

        Assert.Equal("Port", error.Key);
        Assert.Equal("'abc' is not a valid Int32 (Port)", error.Message);
        Assert.Same(port, error.Definition);
    }

    [Fact]
    public void Read_Scalar_AppliesPrimitiveRules()
    {
        var name = ConfigurationDefinition.Define("Name", new Primitive<string>("Name", Converters.String)
        {
            Normalizers = [Normalizers.Trim],
            Validators = [Validators.NotEmpty],
        });
        var contract = Contract(name);

        Assert.Equal("abc", contract.Read(Source(("Name", "  abc  "))).Get(name));
        Assert.Equal("must not be empty", Single(ReadFailure(contract, Source(("Name", "  "))), DiagnosticSeverity.Error).Message);
    }

    [Fact]
    public void Read_Scalar_EmptyString_IsAValue()
    {
        var name = ConfigurationDefinition.Define("Name", Primitive.String).Default("fallback");

        Assert.Equal("", Contract(name).Read(Source(("Name", ""))).Get(name));
    }

    [Fact]
    public void Read_Scalar_WithChildEntries_WarnsAndIsMissing()
    {
        var port = ConfigurationDefinition.Define("Port", Port).Default(80);

        var snapshot = Contract(port).Read(Source(("Port:0", "8080")));

        Assert.Equal(80, snapshot.Get(port));
        Assert.Equal("Port", Single(snapshot.Diagnostics, DiagnosticSeverity.Warning).Key);
    }

    [Fact]
    public void Read_ReadsEveryDefinition_AndReportsEveryError()
    {
        var port = ConfigurationDefinition.Define("Port", Port);
        var host = ConfigurationDefinition.Define("Host", Primitive.String);
        var count = ConfigurationDefinition.Define("Count", Primitive.Int32);

        var diagnostics = ReadFailure(Contract(port, host, count), Source(("Port", "0"), ("Count", "x")));

        Assert.Equal(["Port", "Host", "Count"], diagnostics.Select(d => d.Key));
    }

    // Presence

    [Fact]
    public void Read_MissingWithoutDefault_IsRequired()
    {
        var port = ConfigurationDefinition.Define("Port", Port);

        var error = Single(ReadFailure(Contract(port), Source()), DiagnosticSeverity.Error);

        Assert.True(port.IsRequired);
        Assert.Equal("value is required", error.Message);
    }

    [Fact]
    public void Read_MissingWithDefault_IsDefault()
    {
        var port = ConfigurationDefinition.Define("Port", Port).Default(8080);

        Assert.False(port.IsRequired);
        Assert.True(port.HasDefault);
        Assert.Equal(8080, Contract(port).Read(Source()).Get(port));
    }

    [Fact]
    public void Read_Default_GoesThroughPrimitiveRules()
    {
        var port = ConfigurationDefinition.Define("Port", Port).Default(0);

        var error = Single(ReadFailure(Contract(port), Source()), DiagnosticSeverity.Error);

        Assert.Equal("must be between 1 and 65535", error.Message);
    }

    [Fact]
    public void Read_DefaultOfValueType_IsAValue()
    {
        var count = ConfigurationDefinition.Define("Count", Primitive.Int32).Default(default);

        Assert.Equal(0, Contract(count).Read(Source()).Get(count));
    }

    [Fact]
    public void Read_OptionalValueType_MissingIsNull()
    {
        var port = ConfigurationDefinition.Define("Port", Port).Optional();
        var contract = Contract(port);

        Assert.True(port.IsOptional);
        Assert.False(port.IsRequired);
        Assert.Equal(typeof(int?), port.ValueType);
        Assert.Null(contract.Read(Source()).Get(port));
        Assert.Equal(8080, contract.Read(Source(("Port", "8080"))).Get(port));
    }

    [Fact]
    public void Read_OptionalReferenceType_MissingIsNull()
    {
        var name = ConfigurationDefinition.Define("Name", Primitive.String).Optional();
        var contract = Contract(name);

        Assert.True(name.IsOptional);
        Assert.Null(contract.Read(Source()).Get(name));
        Assert.Equal("a", contract.Read(Source(("Name", "a"))).Get(name));
    }

    [Fact]
    public void Read_Optional_PresentValueGoesThroughPrimitive()
    {
        var port = ConfigurationDefinition.Define("Port", Port).Optional();

        var error = Single(ReadFailure(Contract(port), Source(("Port", "0"))), DiagnosticSeverity.Error);

        Assert.Equal("must be between 1 and 65535", error.Message);
    }

    [Fact]
    public void Describe_And_Sensitive_CarryOverToOptional()
    {
        var key = ConfigurationDefinition.Define("Key", Primitive.String).Describe("A key.").Sensitive().Optional();

        Assert.Equal("A key.", key.Description);
        Assert.True(key.IsSensitive);
    }

    // Sensitive values

    [Fact]
    public void Read_Sensitive_LeavesValueOutOfDiagnostics()
    {
        var key = ConfigurationDefinition.Define("Key", Port).Sensitive();

        var error = Single(ReadFailure(Contract(key), Source(("Key", "secret"))), DiagnosticSeverity.Error);

        Assert.Equal("value is not a valid Int32 (Port)", error.Message);
    }

    [Fact]
    public void Read_Sensitive_LeavesExceptionMessagesOut()
    {
        var key = ConfigurationDefinition.Define("Key", new Primitive<string>("Key", Converters.String)
        {
            Validators = [Validators.Create<string>("boom", value => throw new InvalidOperationException(value))],
        }).Sensitive();

        var error = Single(ReadFailure(Contract(key), Source(("Key", "secret"))), DiagnosticSeverity.Error);

        Assert.Equal("validator 'boom' failed", error.Message);
    }

    [Fact]
    public void Read_Sensitive_HidesEveryValueInRuleTexts_BoundsIncluded()
    {
        var key = ConfigurationDefinition.Define("Key", Port).Sensitive();

        var error = Single(ReadFailure(Contract(key), Source(("Key", "0"))), DiagnosticSeverity.Error);

        Assert.Equal("must be between (hidden) and (hidden)", error.Message);
    }

    [Fact]
    public void Read_SensitiveIndexed_LeavesItemValuesOut()
    {
        var hosts = ConfigurationDefinition.Indexed("Hosts", new ListPrimitive<int>("Ports", Primitive.Int32)).Sensitive();

        var error = Single(ReadFailure(Contract(hosts), Source(("Hosts:0", "secret"))), DiagnosticSeverity.Error);

        Assert.Equal("Hosts:0", error.Key);
        Assert.Equal("value is not a valid Int32", error.Message);
    }

    [Fact]
    public void Read_Sensitive_HidesInputInConverterReasons()
    {
        var numbers = new Primitive<IReadOnlyList<int>>("Numbers", Converters.List(Converters.Int32));
        var key = ConfigurationDefinition.Define("Key", numbers).Sensitive();

        var error = Single(ReadFailure(Contract(key), Source(("Key", "1,secret"))), DiagnosticSeverity.Error);

        Assert.Equal("value is not a valid IReadOnlyList<Int32> (Numbers): item 1: (hidden) is not a valid Int32", error.Message);
    }

    [Fact]
    public void Read_Sensitive_HidesValuesInWrappedPrimitiveErrors()
    {
        var ports = new Primitive<IReadOnlyList<int>>("PortList", Converters.List(Port.AsConverter()));
        var key = ConfigurationDefinition.Define("Key", ports).Sensitive();

        var errors = ReadFailure(Contract(key), Source(("Key", "80,0,secret")));

        Assert.Equal(
            [
                "value is not a valid IReadOnlyList<Int32> (PortList): item 1: must be between (hidden) and (hidden)",
                "value is not a valid IReadOnlyList<Int32> (PortList): item 2: value is not a valid Int32 (Port)",
            ],
            errors.Select(error => error.Message));
    }

    [Fact]
    public void Read_NotSensitive_ShowsWrappedPrimitiveErrors()
    {
        var ports = new Primitive<IReadOnlyList<int>>("PortList", Converters.List(Port.AsConverter()));
        var key = ConfigurationDefinition.Define("Key", ports);

        var errors = ReadFailure(Contract(key), Source(("Key", "80,0,abc")));

        Assert.Equal(
            [
                "'80,0,abc' is not a valid IReadOnlyList<Int32> (PortList): item 1: must be between 1 and 65535",
                "'80,0,abc' is not a valid IReadOnlyList<Int32> (PortList): item 2: 'abc' is not a valid Int32 (Port)",
            ],
            errors.Select(error => error.Message));
    }

    // Delimited lists

    [Fact]
    public void Read_DelimitedList_ReadsOneEntry()
    {
        var hosts = ConfigurationDefinition.Define("Hosts", Hosts);

        Assert.Equal(["a", "b"], Contract(hosts).Read(Source(("Hosts", "a, b"))).Get(hosts));
    }

    [Fact]
    public void Read_DelimitedList_ItemErrorsAreUnderTheKey()
    {
        var ports = ConfigurationDefinition.Define("Ports", new ListPrimitive<int>("Ports", Port));

        var error = Single(ReadFailure(Contract(ports), Source(("Ports", "80,x"))), DiagnosticSeverity.Error);

        Assert.Equal("Ports", error.Key);
        Assert.Equal("item 1: 'x' is not a valid Int32 (Port)", error.Message);
    }

    [Fact]
    public void Read_DelimitedList_DefaultGoesThroughListRules()
    {
        var hosts = ConfigurationDefinition.Define("Hosts", Hosts).Default([]);

        var error = Single(ReadFailure(Contract(hosts), Source()), DiagnosticSeverity.Error);

        Assert.Equal("must not be empty", error.Message);
    }

    // Indexed lists

    [Fact]
    public void Read_Indexed_ReadsOneEntryPerItem()
    {
        var hosts = ConfigurationDefinition.Indexed("Hosts", Hosts);

        var snapshot = Contract(hosts).Read(Source(("Hosts:0", "a"), ("Hosts:1", "b, c")));

        Assert.Equal(["a", "b, c"], snapshot.Get(hosts));
    }

    [Fact]
    public void Read_Indexed_OrdersIndicesNumerically()
    {
        var hosts = ConfigurationDefinition.Indexed("Hosts", Hosts);
        var source = Source(Enumerable.Range(0, 11).Reverse().Select(i => ($"Hosts:{i}", (string?)$"h{i}")).ToArray());

        Assert.Equal(Enumerable.Range(0, 11).Select(i => $"h{i}"), Contract(hosts).Read(source).Get(hosts));
    }

    [Fact]
    public void Read_Indexed_Missing_IsRequired()
    {
        var hosts = ConfigurationDefinition.Indexed("Hosts", Hosts);

        Assert.Equal("value is required", Single(ReadFailure(Contract(hosts), Source()), DiagnosticSeverity.Error).Message);
    }

    [Fact]
    public void Read_Indexed_DefaultAndOptional()
    {
        var withDefault = ConfigurationDefinition.Indexed("A", Hosts).Default(["x"]);
        var optional = ConfigurationDefinition.Indexed("B", Hosts).Optional();

        var snapshot = Contract(withDefault, optional).Read(Source());

        Assert.Equal(["x"], snapshot.Get(withDefault));
        Assert.Null(snapshot.Get(optional));
    }

    [Fact]
    public void Read_Indexed_ItemErrorsAreUnderTheItemKey()
    {
        var ports = ConfigurationDefinition.Indexed("Ports", new ListPrimitive<int>("Ports", Port));

        var diagnostics = ReadFailure(Contract(ports), Source(("Ports:0", "x"), ("Ports:1", "0")));

        Assert.Equal(["Ports:0", "Ports:1"], diagnostics.Select(d => d.Key));
        Assert.Equal(["'x' is not a valid Int32 (Port)", "must be between 1 and 65535"], diagnostics.Select(d => d.Message));
    }

    [Fact]
    public void Read_Indexed_ListRulesRunOnTheList()
    {
        var hosts = ConfigurationDefinition.Indexed("Hosts", new ListPrimitive<string>("Hosts", Primitive.String)
        {
            Validators = [Validators.Create<IReadOnlyList<string>>("at most one", list => list.Count <= 1)],
        });

        var error = Single(ReadFailure(Contract(hosts), Source(("Hosts:0", "a"), ("Hosts:1", "b"))), DiagnosticSeverity.Error);

        Assert.Equal("Hosts", error.Key);
        Assert.Equal("at most one", error.Message);
    }

    [Fact]
    public void Read_Indexed_NonIntegerIndex_IsError()
    {
        var hosts = ConfigurationDefinition.Indexed("Hosts", Hosts);

        var error = Single(ReadFailure(Contract(hosts), Source(("Hosts:0", "a"), ("Hosts:x", "b"))), DiagnosticSeverity.Error);

        Assert.Equal("Hosts:x", error.Key);
        Assert.Equal("'x' is not a valid index", error.Message);
    }

    [Fact]
    public void Read_Indexed_DuplicateIndex_IsError()
    {
        var hosts = ConfigurationDefinition.Indexed("Hosts", Hosts);

        var error = Single(ReadFailure(Contract(hosts), Source(("Hosts:1", "a"), ("Hosts:01", "b"))), DiagnosticSeverity.Error);

        Assert.Equal("index 1 is defined more than once", error.Message);
    }

    [Fact]
    public void Read_Indexed_Gap_WarnsAndCompacts()
    {
        var hosts = ConfigurationDefinition.Indexed("Hosts", Hosts);

        var snapshot = Contract(hosts).Read(Source(("Hosts:0", "a"), ("Hosts:2", "c")));

        Assert.Equal(["a", "c"], snapshot.Get(hosts));
        Assert.Equal("indices are not contiguous from 0", Single(snapshot.Diagnostics, DiagnosticSeverity.Warning).Message);
    }

    [Fact]
    public void Read_Indexed_SingleValue_WarnsAndIsMissing()
    {
        var hosts = ConfigurationDefinition.Indexed("Hosts", Hosts).Default(["x"]);

        var snapshot = Contract(hosts).Read(Source(("Hosts", "a,b")));

        Assert.Equal(["x"], snapshot.Get(hosts));
        Assert.Equal("Hosts", Single(snapshot.Diagnostics, DiagnosticSeverity.Warning).Key);
    }

    [Fact]
    public void Read_Indexed_ElementIsAList_EachEntryIsDelimited()
    {
        var rows = ConfigurationDefinition.Indexed(
            "Rows",
            new ListPrimitive<IReadOnlyList<int>>("Rows", new ListPrimitive<int>("Row", Primitive.Int32)));

        var value = Contract(rows).Read(Source(("Rows:0", "1,2"), ("Rows:1", "3"))).Get(rows);

        Assert.Equal([1, 2], value[0]);
        Assert.Equal([3], value[1]);
    }

    // Snapshot and exception

    [Fact]
    public void Get_DefinitionOutsideContract_Throws()
    {
        var snapshot = Contract().Read(Source());

        Assert.Throws<ArgumentException>(() => snapshot.Get(ConfigurationDefinition.Define("Port", Port)));
    }

    [Fact]
    public void Read_Invalid_ThrowsWithEveryErrorInTheMessage()
    {
        var port = ConfigurationDefinition.Define("Server:Port", Port);
        var host = ConfigurationDefinition.Define("Host", Primitive.String);

        var exception = Assert.Throws<InvalidConfigurationException>(
            () => Contract(port, host).Read(Source(("Server:Port", "0"))));

        Assert.Equal(
            string.Join(
                Environment.NewLine,
                "Configuration is invalid:",
                "",
                "  Server:Port  must be between 1 and 65535",
                "  Host         value is required"),
            exception.Message);
        Assert.Equal(2, exception.Diagnostics.Count);
    }

    private static ConfigurationContract Contract(params ConfigurationDefinition[] definitions)
    {
        var builder = new ConfigurationContractBuilder();
        foreach (var definition in definitions)
        {
            builder.Register(definition);
        }

        return builder.Build();
    }

    private static IReadOnlyList<ConfigurationDiagnostic> ReadFailure(ConfigurationContract contract, IValueSource source)
    {
        Assert.False(contract.TryRead(source, out var snapshot, out var diagnostics));
        Assert.Null(snapshot);
        return diagnostics;
    }

    private static ConfigurationDiagnostic Single(IReadOnlyList<ConfigurationDiagnostic> diagnostics, DiagnosticSeverity severity) =>
        Assert.Single(diagnostics, d => d.Severity == severity);

    private static IValueSource Source(params (string Key, string? Value)[] values) =>
        ValueSource.FromPairs(values.Select(pair => KeyValuePair.Create(pair.Key, pair.Value)));
}
