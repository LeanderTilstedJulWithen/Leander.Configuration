using System.Text;
using Leander.Configuration.Descriptors;

namespace Leander.Configuration.Tooling.Internal;

// Documentation as Markdown. Definitions are grouped by their first key segment, and each group has a summary table
// followed by a section per key. Primitives are described once, at the end, and linked from the keys that use them.
// Primitives with nothing to say (no description, rules, base, element or values) are left out.
internal static class MarkdownDocumentation
{
    public static string Write(ContractDescriptor contract, string title)
    {
        var writer = new Writer(contract);
        writer.WriteContract(title);
        return writer.ToString();
    }

    private sealed class Writer(ContractDescriptor contract)
    {
        private readonly ContractDescriptor _contract = contract;
        private readonly List<PrimitiveDescriptor> _listed = [.. contract.Primitives.Where(IsWorthListing)];
        private readonly StringBuilder _text = new();

        public override string ToString() => _text.ToString();

        public void WriteContract(string title)
        {
            Line($"# {title}");
            Line();

            foreach (var group in _contract.Definitions.GroupBy(definition => GroupName(definition.Key)))
            {
                Line($"## {group.Key}");
                Line();
                Line("| Key | Type | Presence | Default |");
                Line("|-----|------|----------|---------|");

                foreach (var definition in group)
                {
                    var value = definition.Value;
                    Line($"| [{Code(definition.Key)}](#{Anchor(definition.Key)}) | {Cell(TypeText(value))} | {PresenceText(value.Presence)} | {Cell(DefaultCell(value))} |");
                }

                Line();

                foreach (var definition in group)
                {
                    WriteDefinition(definition);
                }
            }

            if (_listed.Count > 0)
            {
                Line("## Primitives");
                Line();

                foreach (var primitive in _listed)
                {
                    WritePrimitive(primitive);
                }
            }
        }

        private void WriteDefinition(DefinitionDescriptor definition)
        {
            Line($"### {Code(definition.Key)}");
            Line();

            if (definition.Description is { } description)
            {
                Line(description);
                Line();
            }

            Line($"- **Type:** {TypeText(definition.Value)}");
            Line($"- **Presence:** {PresenceDetail(definition.Value)}");

            if (definition.Value.Form == ValueForm.Indexed)
            {
                Line($"- **Form:** indexed: {Code($"{definition.Key}:0")}, {Code($"{definition.Key}:1")}, …");
            }

            if (definition.IsSensitive)
            {
                Line("- **Sensitive:** the value is never shown in diagnostics or documentation.");
            }

            Line();
        }

        private void WritePrimitive(PrimitiveDescriptor primitive)
        {
            Line($"### {Text(Heading(primitive))}");
            Line();

            if (primitive.Description is { } description)
            {
                Line(description);
                Line();
            }

            if (primitive.Base is { } @base)
            {
                Line($"- **Derived from:** {PrimitiveText(@base)}");
            }

            if (primitive.Element is { } element)
            {
                Line($"- **Element:** {PrimitiveText(element)}");
            }

            if (primitive.Delimiter is { } delimiter)
            {
                Line($"- **Delimiter:** {Code(delimiter.ToString())}");
            }

            if (primitive.Values is { } values)
            {
                Line($"- **Values:** {string.Join(", ", values.Select(Code))}");
            }

            if (primitive.Normalizers.Count > 0)
            {
                Line($"- **Normalized:** {string.Join("; ", primitive.Normalizers)}");
            }

            if (primitive.Validators.Count > 0)
            {
                Line($"- **Validated:** {string.Join("; ", primitive.Validators)}");
            }

            if (HasDetails(primitive))
            {
                Line();
            }
        }

        private string TypeText(ValueDescriptor value) =>
            value.Primitive is { } reference ? PrimitiveText(reference) : Text(value.Type);

        // A listed primitive links to its description; one with nothing to say is only named.
        private string PrimitiveText(PrimitiveReference reference)
        {
            var heading = Heading(reference.Type, reference.Name);
            return _listed.Any(primitive => primitive.Type == reference.Type && primitive.Name == reference.Name)
                ? $"[{Text(heading)}](#{Anchor(heading)})"
                : Text(heading);
        }

        private void Line(string text = "") => _text.Append(text).Append('\n');

        private static bool IsWorthListing(PrimitiveDescriptor primitive) =>
            primitive.Description is not null || HasDetails(primitive);

        // The lines under a primitive's description. A list primitive always has its element.
        private static bool HasDetails(PrimitiveDescriptor primitive) =>
            primitive.Base is not null ||
            primitive.Element is not null ||
            primitive.Normalizers.Count > 0 ||
            primitive.Validators.Count > 0 ||
            primitive.Values is not null;

        private static string GroupName(string key) => key.Split(':')[0];

        private static string Heading(PrimitiveDescriptor primitive) => Heading(primitive.Type, primitive.Name);

        // Like Primitive.DisplayName: "Int32 (Port)", or just "Int32" when the name is the type name.
        private static string Heading(string type, string name) => name == type ? type : $"{type} ({name})";

        private static string PresenceText(ValuePresence presence) => presence switch
        {
            ValuePresence.Default => "default",
            ValuePresence.Optional => "optional",
            _ => "required",
        };

        private static string PresenceDetail(ValueDescriptor value) => value.Presence switch
        {
            ValuePresence.Default => DefaultText(value) is { } text ? $"default {text}" : "default (hidden)",
            ValuePresence.Optional => "optional, missing is null",
            _ => "required",
        };

        private static string DefaultCell(ValueDescriptor value) =>
            DefaultText(value) ?? (value.Presence == ValuePresence.Default ? "*hidden*" : "");

        // An indexed default shows its items, one entry each in the source.
        private static string? DefaultText(ValueDescriptor value) =>
            value.DefaultItems is { } items ? items.Count == 0 ? "*empty*" : string.Join(", ", items.Select(Code))
            : value.Default is { } text ? Code(text)
            : null;

        private static string Code(string text) => text.Length switch
        {
            0 => "*empty*",
            _ when text.Contains('`') => $"`` {text} ``",
            _ => $"`{text}`",
        };

        private static string Cell(string text) => text.Replace("|", "\\|");

        // Plain text outside code spans. GitHub would read <Uri> in IReadOnlyList<Uri> as an HTML tag.
        private static string Text(string text) => text.Replace("<", "\\<").Replace(">", "\\>");

        // GitHub's heading anchors: lower case, spaces become hyphens, other punctuation is dropped.
        private static string Anchor(string heading)
        {
            var anchor = new StringBuilder();
            foreach (var character in heading.ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(character) || character is '-' or '_')
                {
                    anchor.Append(character);
                }
                else if (character == ' ')
                {
                    anchor.Append('-');
                }
            }

            return anchor.ToString();
        }
    }
}
