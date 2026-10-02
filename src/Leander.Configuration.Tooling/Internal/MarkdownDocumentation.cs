using System.Text;
using Leander.Configuration.Descriptors;

namespace Leander.Configuration.Tooling.Internal;

// Documentation as Markdown. Definitions are grouped by their first key segment, and each group has a summary table
// followed by a section per key with what belongs to the key. Then every primitive with something to say has a
// section with its own parts, linked from its users: rules live on primitives, not on keys.
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
        private readonly HashSet<PrimitiveReference> _listed =
            [.. contract.Primitives.Where(HasSomethingToSay).Select(Reference)];
        private readonly Dictionary<PrimitiveReference, PrimitiveDescriptor> _primitives = Index(contract);
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

                // By type, so a ready-made primitive is followed by those derived from it: Int32, Int32 (Port), …
                var listed = _contract.Primitives
                    .Where(primitive => _listed.Contains(Reference(primitive)))
                    .OrderBy(primitive => primitive.Type, StringComparer.Ordinal)
                    .ThenBy(primitive => primitive.Name != primitive.Type)
                    .ThenBy(primitive => primitive.Name, StringComparer.Ordinal);

                foreach (var primitive in listed)
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

            var primitive = Find(definition.Value.Primitive);
            var type = TypeText(definition.Value);
            Line(primitive?.Description is { } primitiveDescription ? $"- **Type:** {type}: {primitiveDescription}" : $"- **Type:** {type}");
            Line($"- **Presence:** {PresenceDetail(definition.Value)}");

            // How the key is written. An indexed list has an entry per item, so the delimiter doesn't apply.
            if (definition.Value.Form == ValueForm.Indexed)
            {
                Line($"- **Form:** indexed: {Code($"{definition.Key}:0")}, {Code($"{definition.Key}:1")}, …");
            }
            else if (primitive?.Delimiter is { } delimiter)
            {
                Line($"- **Form:** one entry, items separated by {Code(delimiter.ToString())}");
            }

            if (definition.IsSensitive)
            {
                Line("- **Sensitive:** the value is never shown in diagnostics or documentation.");
            }

            Line();
        }

        // Only the primitive's own parts. A derived primitive's format, items, delimiter and values are its base's.
        private void WritePrimitive(PrimitiveDescriptor primitive)
        {
            Line($"### {Text(Heading(primitive))}");
            Line();

            if (primitive.Description is { } description)
            {
                Line(description);
                Line();
            }

            // A list's converter description says what Items and Delimiter already say.
            if (primitive.Converter is { } format && primitive.Element is null)
            {
                Line($"- **Format:** {Text(format)}");
            }

            if (primitive.Base is { } @base)
            {
                Line($"- **Derived from:** {PrimitiveText(@base)}");
            }
            else
            {
                if (primitive.Element is { } element)
                {
                    Line($"- **Items:** {PrimitiveText(element)}");
                }

                if (primitive.Delimiter is { } delimiter)
                {
                    Line($"- **Delimiter:** {Code(delimiter.ToString())}");
                }

                if (primitive.Values is { } values)
                {
                    Line($"- **Values:** {string.Join(", ", values.Select(Code))}");
                }
            }

            if (primitive.Normalizers.Count > 0)
            {
                Line($"- **Normalized:** {string.Join("; ", primitive.Normalizers)}");
            }

            if (primitive.Validators.Count > 0)
            {
                Line($"- **Validated:** {string.Join("; ", primitive.Validators)}");
            }

            var reference = Reference(primitive);
            var usedBy = _contract.Definitions
                .Where(definition => definition.Value.Primitive == reference)
                .Select(definition => $"[{Code(definition.Key)}](#{Anchor(definition.Key)})")
                .Concat(_contract.Primitives.Where(user => user.Base is null && user.Element == reference).Select(PrimitiveLink))
                .ToList();
            var derived = _contract.Primitives.Where(user => user.Base == reference).Select(PrimitiveLink).ToList();

            if (usedBy.Count > 0)
            {
                Line($"- **Used by:** {string.Join(", ", usedBy)}");
            }

            if (derived.Count > 0)
            {
                Line($"- **Derived primitives:** {string.Join(", ", derived)}");
            }

            Line();
        }

        private PrimitiveDescriptor? Find(PrimitiveReference? reference) =>
            reference is not null && _primitives.TryGetValue(reference, out var primitive) ? primitive : null;

        private string TypeText(ValueDescriptor value) =>
            value.Primitive is { } reference ? PrimitiveText(reference) : Text(value.Type);

        // A listed primitive links to its section; one with nothing to say is only named.
        private string PrimitiveText(PrimitiveReference reference)
        {
            var heading = Heading(reference.Type, reference.Name);
            return _listed.Contains(reference) ? $"[{Text(heading)}](#{Anchor(heading)})" : Text(heading);
        }

        private string PrimitiveLink(PrimitiveDescriptor primitive) => PrimitiveText(Reference(primitive));

        private void Line(string text = "") => _text.Append(text).Append('\n');

        // A descriptor read from a file isn't checked, so a duplicate keeps the first.
        private static Dictionary<PrimitiveReference, PrimitiveDescriptor> Index(ContractDescriptor contract)
        {
            var primitives = new Dictionary<PrimitiveReference, PrimitiveDescriptor>();
            foreach (var primitive in contract.Primitives)
            {
                primitives.TryAdd(new PrimitiveReference(primitive.Type, primitive.Name), primitive);
            }

            return primitives;
        }

        private static PrimitiveReference Reference(PrimitiveDescriptor primitive) => new(primitive.Type, primitive.Name);

        // Anything for its section to show besides links to its users.
        private static bool HasSomethingToSay(PrimitiveDescriptor primitive) =>
            primitive.Description is not null ||
            primitive.Converter is not null ||
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
