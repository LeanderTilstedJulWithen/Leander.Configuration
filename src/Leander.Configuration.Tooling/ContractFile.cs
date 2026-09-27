using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Leander.Configuration.Descriptors;

namespace Leander.Configuration.Tooling;

// The contract descriptor as JSON. The file is descriptive: it can be committed and compared, but never run,
// because normalizers and validators are code.
public static class ContractFile
{
    public const int FormatVersion = 1;

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        // The file is read by people and diffed, not embedded in HTML, so IReadOnlyList<Uri> stays readable.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public static string Write(ContractDescriptor contract) =>
        JsonSerializer.Serialize(new Document(FormatVersion, contract.Definitions, contract.Primitives), Options);

    // Throws JsonException for malformed JSON, and FormatException for an unsupported format version.
    public static ContractDescriptor Read(string json)
    {
        var document = JsonSerializer.Deserialize<Document>(json, Options)
            ?? throw new FormatException("The contract file is empty.");

        if (document.FormatVersion != FormatVersion)
        {
            throw new FormatException($"Contract file format version {document.FormatVersion} is not supported. Expected {FormatVersion}.");
        }

        return new ContractDescriptor(document.Definitions, document.Primitives);
    }

    private sealed record Document(
        int FormatVersion,
        IReadOnlyList<DefinitionDescriptor> Definitions,
        IReadOnlyList<PrimitiveDescriptor> Primitives);
}
