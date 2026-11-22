using System.Text.Json;

namespace Leander.Configuration.Tooling.Tests;

public class ContractSerializerTests
{
    // The whole file, so that any change to the format fails here and has to be made on purpose.
    // A change that breaks reading older files also needs a new FormatVersion.
    private static readonly string ServerFile = """
        {
          "formatVersion": 1,
          "definitions": [
            {
              "key": "Server:Host",
              "description": "The host name.",
              "type": "String",
              "primitive": "String",
              "presence": "default",
              "default": "localhost",
              "form": "scalar"
            },
            {
              "key": "Server:Port",
              "description": "The port to listen on.",
              "type": "Int32",
              "primitive": "Port",
              "presence": "required",
              "form": "scalar"
            },
            {
              "key": "Server:MaxConnections",
              "type": "Int32",
              "primitive": "Int32",
              "presence": "optional",
              "form": "scalar"
            },
            {
              "key": "Server:AllowedOrigins",
              "type": "IReadOnlyList<Uri>",
              "primitive": "Origins",
              "presence": "required",
              "form": "indexed"
            },
            {
              "key": "Server:Features",
              "type": "IReadOnlyList<String>",
              "primitive": "Features",
              "presence": "default",
              "default": "",
              "form": "scalar"
            },
            {
              "key": "Database:Password",
              "type": "String",
              "primitive": "String",
              "presence": "default",
              "form": "scalar",
              "sensitive": true
            }
          ],
          "primitives": [
            {
              "type": "String",
              "name": "String",
              "converter": "Any text."
            },
            {
              "type": "Int32",
              "name": "Port",
              "description": "A TCP port.",
              "converter": "A 32-bit integer.",
              "validators": [
                "must be between 1 and 65535"
              ]
            },
            {
              "type": "Int32",
              "name": "Int32",
              "converter": "A 32-bit integer."
            },
            {
              "type": "IReadOnlyList<Uri>",
              "name": "Origins",
              "converter": "A list separated by ','. Each item: Uri.",
              "element": {
                "type": "Uri",
                "name": "Uri"
              },
              "delimiter": ",",
              "validators": [
                "must not be empty"
              ]
            },
            {
              "type": "Uri",
              "name": "Uri",
              "converter": "An absolute URI, e.g. https://example.com."
            },
            {
              "type": "IReadOnlyList<String>",
              "name": "Features",
              "converter": "A list separated by ','. Each item: String.",
              "element": {
                "type": "String",
                "name": "String"
              },
              "delimiter": ","
            }
          ]
        }
        """.ReplaceLineEndings("\n");

    [Fact]
    public void Write_Server_GivesTheExpectedFile()
    {
        Assert.Equal(ServerFile, ContractSerializer.WriteJson(Contracts.Server()));
    }

    [Fact]
    public void Read_TheExpectedFile_GivesTheCurrentContract()
    {
        Assert.Empty(ContractDiff.Compare(ContractSerializer.ReadJson(ServerFile), Contracts.Server()));
    }

    [Fact]
    public void Write_LinesEndInNewlineOnly()
    {
        Assert.DoesNotContain("\r", ContractSerializer.WriteJson(Contracts.Server()));
    }

    [Fact]
    public void RoundTrip_GivesTheSameFile()
    {
        var json = ContractSerializer.WriteJson(Contracts.Server());

        Assert.Equal(json, ContractSerializer.WriteJson(ContractSerializer.ReadJson(json)));
    }

    [Fact]
    public void RoundTrip_GivesAnEqualContract()
    {
        var contract = Contracts.Server();

        Assert.Empty(ContractDiff.Compare(contract, ContractSerializer.ReadJson(ContractSerializer.WriteJson(contract))));
    }

    [Fact]
    public void Write_IsCamelCaseWithVersionAndWithoutNulls()
    {
        var json = ContractSerializer.WriteJson(Contracts.Server());

        Assert.Contains($"\"formatVersion\": {ContractSerializer.FormatVersion}", json);
        Assert.Contains("\"presence\": \"required\"", json);
        Assert.Contains("\"type\": \"IReadOnlyList<Uri>\"", json);
        Assert.DoesNotContain("null", json);
    }

    [Fact]
    public void Read_OtherFormatVersion_Throws()
    {
        var json = ContractSerializer.WriteJson(Contracts.Server()).Replace(
            $"\"formatVersion\": {ContractSerializer.FormatVersion}",
            "\"formatVersion\": 99");

        Assert.Throws<FormatException>(() => ContractSerializer.ReadJson(json));
    }

    [Fact]
    public void Read_MalformedJson_Throws()
    {
        Assert.ThrowsAny<JsonException>(() => ContractSerializer.ReadJson("{ not json"));
    }

    [Fact]
    public void Read_MissingRequiredProperty_Throws()
    {
        Assert.ThrowsAny<JsonException>(() => ContractSerializer.ReadJson("{ \"formatVersion\": 1 }"));
    }

    [Fact]
    public void Read_MissingDescription_IsNull_ButMissingKeyThrows()
    {
        const string value = "\"type\": \"Int32\", \"presence\": \"required\", \"form\": \"scalar\"";

        var contract = ContractSerializer.ReadJson($$"""{ "formatVersion": 1, "definitions": [ { "key": "Port", {{value}} } ], "primitives": [] }""");

        Assert.Null(Assert.Single(contract.Definitions).Description);
        Assert.ThrowsAny<JsonException>(() => ContractSerializer.ReadJson(
            $$"""{ "formatVersion": 1, "definitions": [ { {{value}} } ], "primitives": [] }"""));
    }
}
