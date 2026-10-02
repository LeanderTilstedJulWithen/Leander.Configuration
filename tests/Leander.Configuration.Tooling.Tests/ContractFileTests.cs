using System.Text.Json;

namespace Leander.Configuration.Tooling.Tests;

public class ContractFileTests
{
    [Fact]
    public void RoundTrip_GivesTheSameFile()
    {
        var json = ContractFile.Write(Contracts.Server());

        Assert.Equal(json, ContractFile.Write(ContractFile.Read(json)));
    }

    [Fact]
    public void RoundTrip_GivesAnEqualContract()
    {
        var contract = Contracts.Server();

        Assert.Empty(ContractComparison.Compare(contract, ContractFile.Read(ContractFile.Write(contract))));
    }

    [Fact]
    public void Write_IsCamelCaseWithVersionAndWithoutNulls()
    {
        var json = ContractFile.Write(Contracts.Server());

        Assert.Contains($"\"formatVersion\": {ContractFile.FormatVersion}", json);
        Assert.Contains("\"presence\": \"required\"", json);
        Assert.Contains("\"type\": \"IReadOnlyList<Uri>\"", json);
        Assert.DoesNotContain("null", json);
    }

    [Fact]
    public void Read_OtherFormatVersion_Throws()
    {
        var json = ContractFile.Write(Contracts.Server()).Replace(
            $"\"formatVersion\": {ContractFile.FormatVersion}",
            "\"formatVersion\": 99");

        Assert.Throws<FormatException>(() => ContractFile.Read(json));
    }

    [Fact]
    public void Read_MalformedJson_Throws()
    {
        Assert.ThrowsAny<JsonException>(() => ContractFile.Read("{ not json"));
    }

    [Fact]
    public void Read_MissingRequiredProperty_Throws()
    {
        Assert.ThrowsAny<JsonException>(() => ContractFile.Read("{ \"formatVersion\": 1 }"));
    }

    [Fact]
    public void Read_MissingDescription_IsNull_ButMissingKeyThrows()
    {
        const string value = "\"type\": \"Int32\", \"presence\": \"required\", \"form\": \"scalar\"";

        var contract = ContractFile.Read($$"""{ "formatVersion": 1, "definitions": [ { "key": "Port", {{value}} } ], "primitives": [] }""");

        Assert.Null(Assert.Single(contract.Definitions).Description);
        Assert.ThrowsAny<JsonException>(() => ContractFile.Read(
            $$"""{ "formatVersion": 1, "definitions": [ { {{value}} } ], "primitives": [] }"""));
    }
}
