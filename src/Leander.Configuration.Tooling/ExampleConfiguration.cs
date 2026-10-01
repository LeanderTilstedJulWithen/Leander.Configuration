using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Leander.Configuration.Descriptors;

namespace Leander.Configuration.Tooling;

// Renders a contract descriptor as an appsettings.json-style example with every key, to copy and fill in.
// Every value is a string, the text the converter formats. Without a default, a placeholder says what is expected,
// because JSON has no comments. Sensitive values are always "<secret>".
public static class ExampleConfiguration
{
    private const string Secret = "<secret>";

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        // Placeholders stay readable: "<Port>", not "<Port>".
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        NewLine = "\n",
    };

    // Throws ArgumentException when a key is both a value and a section, e.g. Server and Server:Port.
    public static string Write(ContractDescriptor contract)
    {
        // Sections merge case-insensitively, like IConfiguration reads them, and keep the spelling of their first key.
        var root = new JsonObject(new JsonNodeOptions { PropertyNameCaseInsensitive = true });

        foreach (var definition in contract.Definitions)
        {
            Add(root, definition.Key, Value(contract, definition));
        }

        return root.ToJsonString(Options);
    }

    private static void Add(JsonObject root, string key, JsonNode value)
    {
        var segments = key.Split(':');
        var section = root;

        foreach (var segment in segments[..^1])
        {
            if (!section.TryGetPropertyValue(segment, out var child))
            {
                child = new JsonObject(section.Options);
                section[segment] = child;
            }

            section = child as JsonObject
                ?? throw new ArgumentException($"{key}: a part of the key is also a value, and JSON can't hold both.", "contract");
        }

        if (section.ContainsKey(segments[^1]))
        {
            throw new ArgumentException($"{key}: the key is also a section, and JSON can't hold both.", "contract");
        }

        section[segments[^1]] = value;
    }

    private static JsonNode Value(ContractDescriptor contract, DefinitionDescriptor definition)
    {
        var value = definition.Value;

        if (value.Form == ValueForm.Indexed)
        {
            IEnumerable<string> items = definition.IsSensitive ? [Secret]
                : value.DefaultItems is { } defaultItems ? defaultItems
                : [Placeholder(value.Presence, ElementName(contract, value) ?? value.Type)];

            return new JsonArray([.. items.Select(item => JsonValue.Create(item))]);
        }

        return JsonValue.Create(
            definition.IsSensitive ? Secret
            : value.Default is { } text ? text
            : Placeholder(value.Presence, value.Primitive?.Name ?? value.Type));
    }

    private static string Placeholder(ValuePresence presence, string name) =>
        presence == ValuePresence.Optional ? $"<optional {name}>" : $"<{name}>";

    private static string? ElementName(ContractDescriptor contract, ValueDescriptor value) =>
        contract.Primitives.FirstOrDefault(primitive =>
            primitive.Type == value.Primitive?.Type && primitive.Name == value.Primitive.Name)?.Element?.Name;
}
