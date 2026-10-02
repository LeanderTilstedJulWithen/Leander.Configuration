using System.Text.Json.Nodes;
using Leander.Primitives;

namespace Leander.Configuration.Tooling.Tests;

public class ConfigurationGeneratorTests
{
    private static JsonNode Example(params ConfigurationDefinition[] definitions) =>
        JsonNode.Parse(ConfigurationGenerator.WriteJson(Contracts.Describe(definitions)))!;

    [Fact]
    public void Server_RendersEveryKey()
    {
        var json = ConfigurationGenerator.WriteJson(Contracts.Server());

        Assert.Equal(
            """
            {
              "Server": {
                "Host": "localhost",
                "Port": "<Port>",
                "MaxConnections": "<optional Int32>",
                "AllowedOrigins": [
                  "<Uri>"
                ],
                "Features": ""
              },
              "Database": {
                "Password": "<secret>"
              }
            }
            """.ReplaceLineEndings("\n"),
            json);
    }

    [Fact]
    public void Values_AreStringsAsFormatted()
    {
        var json = Example(
            ConfigurationDefinition.Define("Port", Primitive.Int32).Default(8080),
            ConfigurationDefinition.Define("Enabled", Primitive.Boolean).Default(true),
            ConfigurationDefinition.Define("Timeout", Primitive.TimeSpan).Default(TimeSpan.FromSeconds(30)));

        Assert.Equal("8080", json["Port"]!.GetValue<string>());
        Assert.Equal("True", json["Enabled"]!.GetValue<string>());
        Assert.Equal("00:00:30", json["Timeout"]!.GetValue<string>());
    }

    [Fact]
    public void IndexedDefault_IsAnArrayOfItems()
    {
        var rows = new ListPrimitive<IReadOnlyList<int>>("Rows", new ListPrimitive<int>("Row", Primitive.Int32));

        var json = Example(ConfigurationDefinition.Indexed("Rows", rows).Default([[1, 2], [3]]));

        Assert.Equal(["1,2", "3"], json["Rows"]!.AsArray().Select(item => item!.GetValue<string>()));
    }

    [Fact]
    public void Optional_IndexedAndRequired_Placeholders()
    {
        var json = Example(
            ConfigurationDefinition.Define("Port", Contracts.Port).Optional(),
            ConfigurationDefinition.Indexed("Origins", Contracts.Origins).Optional());

        Assert.Equal("<optional Port>", json["Port"]!.GetValue<string>());
        Assert.Equal("<optional Uri>", json["Origins"]![0]!.GetValue<string>());
    }

    [Fact]
    public void Sensitive_IsAlwaysSecret()
    {
        var json = Example(
            ConfigurationDefinition.Define("Key", Primitive.String).Sensitive(),
            ConfigurationDefinition.Indexed("Keys", new ListPrimitive<string>("Keys", Primitive.String)).Default(["a"]).Sensitive());

        Assert.Equal("<secret>", json["Key"]!.GetValue<string>());
        Assert.Equal("<secret>", json["Keys"]![0]!.GetValue<string>());
    }

    [Fact]
    public void Sections_MergeCaseInsensitively_KeepingFirstSpelling()
    {
        var json = ConfigurationGenerator.WriteJson(Contracts.Describe(
            ConfigurationDefinition.Define("Server:Host", Primitive.String).Default("a"),
            ConfigurationDefinition.Define("server:Port", Primitive.String).Default("b")));

        Assert.Contains("\"Server\"", json);
        Assert.DoesNotContain("\"server\"", json);
        Assert.Contains("\"Port\": \"b\"", json);
    }

    [Fact]
    public void KeyThatIsAlsoASection_Throws()
    {
        var contract = Contracts.Describe(
            ConfigurationDefinition.Define("Server", Primitive.String),
            ConfigurationDefinition.Define("Server:Port", Primitive.String));
        var reversed = Contracts.Describe(
            ConfigurationDefinition.Define("Server:Port", Primitive.String),
            ConfigurationDefinition.Define("Server", Primitive.String));

        Assert.Throws<ArgumentException>(() => ConfigurationGenerator.WriteJson(contract));
        Assert.Throws<ArgumentException>(() => ConfigurationGenerator.WriteJson(reversed));
    }
}
