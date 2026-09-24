namespace Leander.Configuration.Tests;

public class ValueSourceTests
{
    private static IValueSource Source(params (string Key, string? Value)[] values) =>
        ValueSource.FromPairs(values.Select(pair => KeyValuePair.Create(pair.Key, pair.Value)));

    [Fact]
    public void GetValue_ReturnsValueForKey()
    {
        var source = Source(("Port", "8080"));

        Assert.Equal("8080", source.GetValue("Port"));
    }

    [Fact]
    public void GetValue_IsCaseInsensitive()
    {
        var source = Source(("Server:Port", "8080"));

        Assert.Equal("8080", source.GetValue("server:PORT"));
    }

    [Fact]
    public void GetValue_MissingKey_ReturnsNull()
    {
        var source = Source(("Port", "8080"));

        Assert.Null(source.GetValue("Host"));
    }

    [Fact]
    public void GetValue_NullValue_ReturnsNull()
    {
        var source = Source(("Port", null));

        Assert.Null(source.GetValue("Port"));
    }

    [Fact]
    public void GetValue_EmptyValue_IsKept()
    {
        var source = Source(("Port", ""));

        Assert.Equal("", source.GetValue("Port"));
    }

    [Fact]
    public void GetValue_ParentOfNestedKeys_ReturnsNull()
    {
        var source = Source(("Hosts:0", "a"));

        Assert.Null(source.GetValue("Hosts"));
    }

    [Fact]
    public void FromPairs_KeysDifferingOnlyInCase_LastOneWins()
    {
        var source = Source(("Port", "8080"), ("PORT", "9090"));

        Assert.Equal("9090", source.GetValue("Port"));
        Assert.Equal("9090", source.GetValue("port"));
    }

    [Fact]
    public void FromPairs_CopiesValues()
    {
        var values = new Dictionary<string, string?> { ["Port"] = "8080" };
        var source = ValueSource.FromPairs(values);

        values["Port"] = "9090";
        values["Host"] = "localhost";

        Assert.Equal("8080", source.GetValue("Port"));
        Assert.Null(source.GetValue("Host"));
    }

    [Fact]
    public void FromDictionary_UsesDictionaryAsIs()
    {
        var values = new Dictionary<string, string?> { ["Port"] = "8080" };
        var source = ValueSource.FromDictionary(values, StringComparer.Ordinal);

        values["Port"] = "9090";
        values["Hosts:0"] = "a";

        Assert.Equal("9090", source.GetValue("Port"));
        Assert.Equal(["0"], source.GetChildNames("Hosts"));
    }

    [Fact]
    public void FromDictionary_CaseSensitive_TreatsCasingsAsDifferentKeys()
    {
        var values = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["Port"] = "8080",
            ["PORT"] = "9090",
            ["Hosts:a"] = "1",
            ["Hosts:A"] = "2",
            ["HOSTS:b"] = "3",
        };
        var source = ValueSource.FromDictionary(values, StringComparer.Ordinal);

        Assert.Equal("8080", source.GetValue("Port"));
        Assert.Equal("9090", source.GetValue("PORT"));
        Assert.Null(source.GetValue("port"));
        Assert.Equal(["A", "a"], source.GetChildNames("Hosts").Order(StringComparer.Ordinal));
        Assert.Equal(["b"], source.GetChildNames("HOSTS"));
        Assert.Empty(source.GetChildNames("hosts"));
    }

    [Fact]
    public void FromDictionary_CaseInsensitive_MatchesChildrenIgnoringCase()
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Hosts:a"] = "1",
            ["HOSTS:A:Port"] = "2",
            ["hosts:b"] = "3",
        };
        var source = ValueSource.FromDictionary(values, StringComparer.OrdinalIgnoreCase);

        Assert.Equal("1", source.GetValue("HOSTS:A"));
        Assert.Equal(["a", "b"], source.GetChildNames("Hosts").Order(StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetChildNames_ReturnsDirectChildren()
    {
        var source = Source(("Hosts:0", "a"), ("Hosts:1", "b"), ("Hosts:2", "c"));

        Assert.Equal(["0", "1", "2"], source.GetChildNames("Hosts").Order());
    }

    [Fact]
    public void GetChildNames_ReturnsEachNestedChildOnce()
    {
        var source = Source(
            ("Servers:0:Host", "a"),
            ("Servers:0:Port", "1"),
            ("Servers:1:Host", "b"));

        Assert.Equal(["0", "1"], source.GetChildNames("Servers").Order());
    }

    [Fact]
    public void GetChildNames_OfNestedKey()
    {
        var source = Source(("Servers:0:Host", "a"), ("Servers:0:Port", "1"));

        Assert.Equal(["Host", "Port"], source.GetChildNames("Servers:0").Order());
    }

    [Fact]
    public void GetChildNames_IsCaseInsensitive()
    {
        var source = Source(("Hosts:Primary", "a"), ("HOSTS:primary:Port", "1"), ("hosts:Secondary", "b"));

        var names = source.GetChildNames("hosts");

        Assert.Equal(2, names.Count);
        Assert.Contains("Primary", names, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Secondary", names, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetChildNames_IgnoresKeysThatOnlySharePrefix()
    {
        var source = Source(("Hosts:0", "a"), ("HostsBackup:0", "b"), ("Hosts", "c"));

        Assert.Equal(["0"], source.GetChildNames("Hosts"));
    }

    [Fact]
    public void GetChildNames_WithoutChildren_ReturnsEmpty()
    {
        var source = Source(("Port", "8080"));

        Assert.Empty(source.GetChildNames("Port"));
        Assert.Empty(source.GetChildNames("Missing"));
    }

    [Fact]
    public void KeySeparator_IsColon() => Assert.Equal(':', ValueSource.KeySeparator);
}
