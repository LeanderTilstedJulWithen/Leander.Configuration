using Microsoft.Extensions.Configuration;

namespace Leander.Configuration.MicrosoftExtensions.Tests;

public class ConfigurationExtensionsTests
{
    [Fact]
    public void GetValue_ReturnsValueForKey()
    {
        var source = Configuration(("Server:Port", "8080")).AsValueSource();

        Assert.Equal("8080", source.GetValue("Server:Port"));
    }

    [Fact]
    public void GetValue_IsCaseInsensitive()
    {
        var source = Configuration(("Server:Port", "8080")).AsValueSource();

        Assert.Equal("8080", source.GetValue("server:PORT"));
    }

    [Fact]
    public void GetValue_MissingKey_ReturnsNull()
    {
        var source = Configuration(("Port", "8080")).AsValueSource();

        Assert.Null(source.GetValue("Host"));
    }

    [Fact]
    public void GetValue_ParentOfNestedKeys_ReturnsNull()
    {
        var source = Configuration(("Hosts:0", "a")).AsValueSource();

        Assert.Null(source.GetValue("Hosts"));
    }

    [Fact]
    public void GetValue_ReadsLive()
    {
        var configuration = Configuration(("Port", "8080"));
        var source = configuration.AsValueSource();

        configuration["Port"] = "9090";

        Assert.Equal("9090", source.GetValue("Port"));
    }

    [Fact]
    public void GetChildNames_ReturnsEachNestedChildOnce()
    {
        var source = Configuration(
            ("Servers:0:Host", "a"),
            ("Servers:0:Port", "1"),
            ("Servers:1:Host", "b")).AsValueSource();

        Assert.Equal(["0", "1"], source.GetChildNames("Servers").Order());
    }

    [Fact]
    public void GetChildNames_OfNestedKey()
    {
        var source = Configuration(("Servers:0:Host", "a"), ("Servers:0:Port", "1")).AsValueSource();

        Assert.Equal(["Host", "Port"], source.GetChildNames("Servers:0").Order());
    }

    [Fact]
    public void GetChildNames_WithoutChildren_ReturnsEmpty()
    {
        var source = Configuration(("Port", "8080")).AsValueSource();

        Assert.Empty(source.GetChildNames("Port"));
        Assert.Empty(source.GetChildNames("Missing"));
    }

    [Fact]
    public void Section_ReadsKeysRelativeToTheSection()
    {
        var source = Configuration(("Server:Port", "8080"), ("Server:Hosts:0", "a")).GetSection("Server").AsValueSource();

        Assert.Equal("8080", source.GetValue("Port"));
        Assert.Equal(["0"], source.GetChildNames("Hosts"));
        Assert.Null(source.GetValue("Server:Port"));
    }

    private static IConfigurationRoot Configuration(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(pair => KeyValuePair.Create(pair.Key, pair.Value)))
            .Build();
}
