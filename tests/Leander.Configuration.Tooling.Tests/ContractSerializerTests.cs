using System.Text.Json;

namespace Leander.Configuration.Tooling.Tests;

public class ContractSerializerTests
{
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
