using Leander.Primitives;

namespace Leander.Configuration.Tests;

public class ReadOptionsTests
{
    private static readonly ReadOptions CheckServer = new() { CheckedSections = ["Server"] };

    private static readonly ConfigurationDefinition<IReadOnlyList<string>> Hosts =
        ConfigurationDefinition.Indexed("Server:Hosts", new ListPrimitive<string>("Hosts", Primitive.String)).Default([]);

    private static readonly ConfigurationDefinition<int> Port =
        ConfigurationDefinition.Define("Server:Port", Primitive.Int32).Default(80);

    // After Hosts and Port, which it needs when initialized.
    private static readonly ConfigurationContract Contract =
        new ConfigurationContractBuilder().Register(Port).Register(Hosts).Build();

    [Fact]
    public void Default_ChecksNothingAndKeepsWarnings()
    {
        Assert.Empty(ReadOptions.Default.CheckedSections);
        Assert.False(ReadOptions.Default.WarningsAsErrors);
        Assert.Empty(Contract.Read(Source(("Server:Prot", "8080"))).Diagnostics);
    }

    [Fact]
    public void CheckedSections_UnknownKey_IsWarningWithoutDefinition()
    {
        var snapshot = Contract.Read(Source(("Server:Prot", "8080")), CheckServer);

        var warning = Assert.Single(snapshot.Diagnostics);
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        Assert.Equal("Server:Prot", warning.Key);
        Assert.Equal("is not in the configuration contract", warning.Message);
        Assert.Null(warning.Definition);
        Assert.Equal(80, snapshot.Get(Port));
    }

    [Fact]
    public void CheckedSections_NestedUnknownKey_IsReported()
    {
        var snapshot = Contract.Read(Source(("Server:Tls:Certificate", "cert.pfx")), CheckServer);

        Assert.Equal("Server:Tls:Certificate", Assert.Single(snapshot.Diagnostics).Key);
    }

    [Fact]
    public void CheckedSections_KeysOutsideTheSections_AreNotChecked()
    {
        var snapshot = Contract.Read(Source(("Logging:LogLevel:Default", "Information"), ("AllowedHosts", "*")), CheckServer);

        Assert.Empty(snapshot.Diagnostics);
    }

    [Fact]
    public void CheckedSections_DefinedKeys_AreNotReported()
    {
        var snapshot = Contract.Read(
            Source(("Server:Port", "8080"), ("Server:Hosts:0", "a"), ("Server:Hosts:1", "b")),
            CheckServer);

        Assert.Empty(snapshot.Diagnostics);
    }

    [Fact]
    public void CheckedSections_KeysMatchCaseInsensitively()
    {
        var snapshot = Contract.Read(Source(("server:PORT", "8080")), CheckServer);

        Assert.Empty(snapshot.Diagnostics);
        Assert.Equal(8080, snapshot.Get(Port));
    }

    [Fact]
    public void CheckedSections_MissingSection_ReportsNothing()
    {
        var snapshot = Contract.Read(Source(), new ReadOptions { CheckedSections = ["Admin"] });

        Assert.Empty(snapshot.Diagnostics);
    }

    [Fact]
    public void WarningsAsErrors_UnknownKey_FailsTheRead()
    {
        var options = CheckServer with { WarningsAsErrors = true };

        Assert.False(Contract.TryRead(Source(("Server:Prot", "8080")), options, out var snapshot, out var diagnostics));
        Assert.Null(snapshot);
        Assert.Equal(DiagnosticSeverity.Error, Assert.Single(diagnostics).Severity);

        var exception = Assert.Throws<InvalidConfigurationException>(() => Contract.Read(Source(("Server:Prot", "8080")), options));
        Assert.Contains("Server:Prot  is not in the configuration contract", exception.Message);
    }

    [Fact]
    public void WarningsAsErrors_ReaderWarning_FailsTheRead()
    {
        var options = new ReadOptions { WarningsAsErrors = true };

        Assert.False(Contract.TryRead(Source(("Server:Hosts:0", "a"), ("Server:Hosts:2", "c")), options, out _, out var diagnostics));

        var error = Assert.Single(diagnostics);
        Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        Assert.Equal("indices are not contiguous from 0", error.Message);
    }

    private static IValueSource Source(params (string Key, string? Value)[] values) =>
        ValueSource.FromPairs(values.Select(pair => KeyValuePair.Create(pair.Key, pair.Value)));
}
