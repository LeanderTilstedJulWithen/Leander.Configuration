using System.Text;
using Leander.Configuration.Descriptors;

namespace Leander.Configuration.Tooling;

// Renders a contract descriptor as Markdown. Definitions are grouped by their first key segment, and each group has
// a summary table followed by a section per key. Primitives are described once, at the end, and linked from the keys
// that use them. Primitives with nothing to say (no description, rules, base or values) are left out.
public static class MarkdownDocumentation
{
    public static string Write(ContractDescriptor contract, string title = "Configuration")
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
            WriteDetails(definition.Value, definition.Key, indent: "");

            if (definition.IsSensitive)
            {
                Line("- **Sensitive:** the value is never shown in diagnostics or documentation.");
            }

            Line();
        }

        // Form, rules and elements. Elements are nested one level deeper.
        private void WriteDetails(ValueDescriptor value, string key, string indent)
        {
            switch (value.Form)
            {
                case ValueForm.Indexed:
                    Line($"{indent}- **Form:** indexed: {Code($"{key}:0")}, {Code($"{key}:1")}, …");
                    break;

                case ValueForm.Delimited:
                    Line($"{indent}- **Form:** one value, separated by {Code(value.Delimiter?.ToString() ?? "")}");
                    break;
            }

            WriteRules(value.Normalizers, value.Validators, values: null, indent);

            if (value.Element is { } element)
            {
                var elementIndent = indent + "  ";
                Line($"{indent}- **Element:** {TypeText(element)}");

                if (element.Presence != ValuePresence.Required)
                {
                    Line($"{elementIndent}- **Presence:** {PresenceDetail(element)}");
                }

                WriteDetails(element, value.Form == ValueForm.Indexed ? $"{key}:n" : key, elementIndent);
            }
        }

        private void WritePrimitive(PrimitiveDescriptor primitive)
        {
            Line($"### {Heading(primitive)}");
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

            WriteRules(primitive.Normalizers, primitive.Validators, primitive.Values, indent: "");

            if (primitive.Base is not null || primitive.Normalizers.Count > 0 || primitive.Validators.Count > 0 || primitive.Values is not null)
            {
                Line();
            }
        }

        private void WriteRules(IReadOnlyList<string> normalizers, IReadOnlyList<string> validators, IReadOnlyList<string>? values, string indent)
        {
            if (values is not null)
            {
                Line($"{indent}- **Values:** {string.Join(", ", values.Select(Code))}");
            }

            if (normalizers.Count > 0)
            {
                Line($"{indent}- **Normalized:** {string.Join("; ", normalizers)}");
            }

            if (validators.Count > 0)
            {
                Line($"{indent}- **Validated:** {string.Join("; ", validators)}");
            }
        }

        private string TypeText(ValueDescriptor value) => value.Form == ValueForm.Scalar || value.Element is null
            ? ScalarText(value)
            : $"list of {TypeText(value.Element)}";

        private string ScalarText(ValueDescriptor value) =>
            value.Primitive is { } reference ? PrimitiveText(reference) : value.Type;

        // A listed primitive links to its description; one with nothing to say is only named.
        private string PrimitiveText(PrimitiveReference reference)
        {
            var heading = Heading(reference.Type, reference.Name);
            return _listed.Any(primitive => primitive.Type == reference.Type && primitive.Name == reference.Name)
                ? $"[{heading}](#{Anchor(heading)})"
                : heading;
        }

        private void Line(string text = "") => _text.Append(text).Append('\n');

        private static bool IsWorthListing(PrimitiveDescriptor primitive) =>
            primitive.Description is not null ||
            primitive.Base is not null ||
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
            ValuePresence.Default => value.Default is { } text ? $"default {Code(text)}" : "default (hidden)",
            ValuePresence.Optional => "optional, missing is null",
            _ => "required",
        };

        private static string DefaultCell(ValueDescriptor value) =>
            value.Default is { } text ? Code(text) : value.Presence == ValuePresence.Default ? "*hidden*" : "";

        private static string Code(string text) => text.Length switch
        {
            0 => "*empty*",
            _ when text.Contains('`') => $"`` {text} ``",
            _ => $"`{text}`",
        };

        private static string Cell(string text) => text.Replace("|", "\\|");

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
