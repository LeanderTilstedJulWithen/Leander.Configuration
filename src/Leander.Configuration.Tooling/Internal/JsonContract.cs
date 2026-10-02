using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Leander.Configuration.Descriptors;

namespace Leander.Configuration.Tooling.Internal;

// The contract as JSON. The nested Json types define the format, and nothing else does:
// renaming a descriptor property doesn't change the file.
internal static class JsonContract
{
    private const int FormatVersion = ContractSerializer.FormatVersion;

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        // The file is read by people and diffed, not embedded in HTML, so IReadOnlyList<Uri> stays readable.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        RespectNullableAnnotations = true,
    };

    // The version is read first, because another version may have another shape.
    public static ContractDescriptor Read(string json)
    {
        var header = JsonSerializer.Deserialize<HeaderJson>(json, Options)
            ?? throw new FormatException("The contract file is empty.");

        if (header.FormatVersion != FormatVersion)
        {
            throw new FormatException($"Contract file format version {header.FormatVersion} is not supported. Expected {FormatVersion}.");
        }

        var document = JsonSerializer.Deserialize<DocumentJson>(json, Options)!;
        return new ContractDescriptor(
            [.. document.Definitions.Select(ToDescriptor)],
            [.. document.Primitives.Select(ToDescriptor)]);
    }

    public static string Write(ContractDescriptor contract) =>
        JsonSerializer.Serialize(
            new DocumentJson
            {
                FormatVersion = FormatVersion,
                Definitions = [.. contract.Definitions.Select(ToJson)],
                Primitives = [.. contract.Primitives.Select(ToJson)],
            },
            Options);

    private static string FormatForm(ValueForm form) => form switch
    {
        ValueForm.Indexed => "indexed",
        ValueForm.Scalar => "scalar",
        _ => throw new ArgumentOutOfRangeException(nameof(form), form, null),
    };

    private static string FormatPresence(ValuePresence presence) => presence switch
    {
        ValuePresence.Default => "default",
        ValuePresence.Optional => "optional",
        ValuePresence.Required => "required",
        _ => throw new ArgumentOutOfRangeException(nameof(presence), presence, null),
    };

    // Empty lists are left out, like other defaults.
    private static IReadOnlyList<string>? NullIfEmpty(IReadOnlyList<string> list) => list.Count > 0 ? list : null;

    private static ValueForm ParseForm(string form) => form switch
    {
        "indexed" => ValueForm.Indexed,
        "scalar" => ValueForm.Scalar,
        _ => throw new JsonException($"Unknown form '{form}'."),
    };

    private static ValuePresence ParsePresence(string presence) => presence switch
    {
        "default" => ValuePresence.Default,
        "optional" => ValuePresence.Optional,
        "required" => ValuePresence.Required,
        _ => throw new JsonException($"Unknown presence '{presence}'."),
    };

    // A value's primitive has the value's type, so the file only names it.
    private static DefinitionDescriptor ToDescriptor(DefinitionJson definition) =>
        new(
            definition.Key,
            definition.Description,
            definition.Sensitive ?? false,
            new ValueDescriptor(definition.Type, ParsePresence(definition.Presence), ParseForm(definition.Form))
            {
                Default = definition.Default is JsonValue value ? ToText(value) : null,
                DefaultItems = definition.Default is JsonArray items ? [.. items.Select(ToText)] : null,
                Primitive = definition.Primitive is { } name ? new PrimitiveReference(definition.Type, name) : null,
            });

    // A base has the derived primitive's type, so the file only names it.
    private static PrimitiveDescriptor ToDescriptor(PrimitiveJson primitive) =>
        new(primitive.Type, primitive.Name, primitive.Description)
        {
            Base = primitive.Base is { } name ? new PrimitiveReference(primitive.Type, name) : null,
            Element = primitive.Element is { } element ? new PrimitiveReference(element.Type, element.Name) : null,
            Delimiter = primitive.Delimiter,
            Normalizers = primitive.Normalizers ?? [],
            Validators = primitive.Validators ?? [],
            Values = primitive.Values,
        };

    private static DefinitionJson ToJson(DefinitionDescriptor definition) =>
        new()
        {
            Key = definition.Key,
            Description = definition.Description,
            Type = definition.Value.Type,
            Primitive = definition.Value.Primitive?.Name,
            Presence = FormatPresence(definition.Value.Presence),
            Default = definition.Value.DefaultItems is { } items ? new JsonArray([.. items.Select(item => JsonValue.Create(item))])
                : definition.Value.Default is { } text ? JsonValue.Create(text)
                : null,
            Form = FormatForm(definition.Value.Form),
            Sensitive = definition.IsSensitive ? true : null,
        };

    private static PrimitiveJson ToJson(PrimitiveDescriptor primitive) =>
        new()
        {
            Type = primitive.Type,
            Name = primitive.Name,
            Description = primitive.Description,
            Base = primitive.Base?.Name,
            Element = primitive.Element is { } element ? new ReferenceJson { Type = element.Type, Name = element.Name } : null,
            Delimiter = primitive.Delimiter,
            Normalizers = NullIfEmpty(primitive.Normalizers),
            Validators = NullIfEmpty(primitive.Validators),
            Values = primitive.Values,
        };

    private static string ToText(JsonNode? node) =>
        node is JsonValue value && value.TryGetValue(out string? text)
            ? text
            : throw new JsonException($"A default must be a string or an array of strings, not '{node?.ToJsonString() ?? "null"}'.");

    // Properties are written in declaration order.
    private sealed class DefinitionJson
    {
        [JsonPropertyName("key")]
        public required string Key { get; init; }

        [JsonPropertyName("description")]
        public string? Description { get; init; }

        [JsonPropertyName("type")]
        public required string Type { get; init; }

        [JsonPropertyName("primitive")]
        public string? Primitive { get; init; }

        [JsonPropertyName("presence")]
        public required string Presence { get; init; }

        // A string, or for an indexed value an array with an item per entry.
        [JsonPropertyName("default")]
        public JsonNode? Default { get; init; }

        [JsonPropertyName("form")]
        public required string Form { get; init; }

        // Only written when true.
        [JsonPropertyName("sensitive")]
        public bool? Sensitive { get; init; }
    }

    private sealed class DocumentJson
    {
        [JsonPropertyName("formatVersion")]
        public required int FormatVersion { get; init; }

        [JsonPropertyName("definitions")]
        public required IReadOnlyList<DefinitionJson> Definitions { get; init; }

        [JsonPropertyName("primitives")]
        public required IReadOnlyList<PrimitiveJson> Primitives { get; init; }
    }

    private sealed class HeaderJson
    {
        [JsonPropertyName("formatVersion")]
        public required int FormatVersion { get; init; }
    }

    private sealed class PrimitiveJson
    {
        [JsonPropertyName("type")]
        public required string Type { get; init; }

        [JsonPropertyName("name")]
        public required string Name { get; init; }

        [JsonPropertyName("description")]
        public string? Description { get; init; }

        [JsonPropertyName("base")]
        public string? Base { get; init; }

        [JsonPropertyName("element")]
        public ReferenceJson? Element { get; init; }

        [JsonPropertyName("delimiter")]
        public char? Delimiter { get; init; }

        [JsonPropertyName("normalizers")]
        public IReadOnlyList<string>? Normalizers { get; init; }

        [JsonPropertyName("validators")]
        public IReadOnlyList<string>? Validators { get; init; }

        [JsonPropertyName("values")]
        public IReadOnlyList<string>? Values { get; init; }
    }

    // An element has another type than its list, so it's named with its type.
    private sealed class ReferenceJson
    {
        [JsonPropertyName("type")]
        public required string Type { get; init; }

        [JsonPropertyName("name")]
        public required string Name { get; init; }
    }
}
