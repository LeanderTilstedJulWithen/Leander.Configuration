using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
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
        TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { NullableIsOptional } },
    };

    public static string Write(ContractDescriptor contract) =>
        JsonSerializer.Serialize(new Document(FormatVersion, contract.Definitions, contract.Primitives), Options);

    // Throws JsonException for malformed JSON or a missing required property, and FormatException for an
    // unsupported format version. The version is read first, because another version may have another shape.
    public static ContractDescriptor Read(string json)
    {
        var header = JsonSerializer.Deserialize<Header>(json, Options)
            ?? throw new FormatException("The contract file is empty.");

        if (header.FormatVersion != FormatVersion)
        {
            throw new FormatException($"Contract file format version {header.FormatVersion} is not supported. Expected {FormatVersion}.");
        }

        var document = JsonSerializer.Deserialize<Document>(json, Options)!;
        return new ContractDescriptor(document.Definitions, document.Primitives);
    }

    // Write leaves nulls out, so a missing nullable constructor parameter, e.g. a Description, reads as null.
    // Missing non-nullable parameters, e.g. a Key, are still an error.
    private static void NullableIsOptional(JsonTypeInfo typeInfo)
    {
        foreach (var property in typeInfo.Properties)
        {
            if (property.AssociatedParameter is { IsNullable: true })
            {
                property.IsRequired = false;
            }
        }
    }

    private sealed record Header(int FormatVersion);

    private sealed record Document(
        int FormatVersion,
        IReadOnlyList<DefinitionDescriptor> Definitions,
        IReadOnlyList<PrimitiveDescriptor> Primitives);
}
