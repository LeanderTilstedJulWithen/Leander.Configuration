using System.Text;
using Leander.Configuration.Descriptors;

namespace Leander.Configuration.Tooling.Internal;

// Documentation as Markdown. Definitions are grouped by their first key segment, and each group has a summary table
// followed by a section per key with everything about the key. Primitives are an implementation detail: their
// format and rules are collected along the base chain and shown on every key that uses them.
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

            var chain = Chain(definition.Value.Primitive);
            Line($"- **Type:** {TypeText(definition.Value)}");

            // How the key is written. An indexed list has an entry per item, so the delimiter doesn't apply.
            if (definition.Value.Form == ValueForm.Indexed)
            {
                Line($"- **Form:** indexed: {Code($"{definition.Key}:0")}, {Code($"{definition.Key}:1")}, …");
            }
            else if (chain.FirstOrDefault()?.Delimiter is { } delimiter)
            {
                Line($"- **Form:** one entry, items separated by {Code(delimiter.ToString())}");
            }

            WriteRules(chain, "");
            Line($"- **Presence:** {PresenceDetail(definition.Value)}");

            if (definition.IsSensitive)
            {
                Line("- **Sensitive:** the value is never shown in diagnostics or documentation.");
            }

            Line();
        }

        // The format and values come from the root, whose converter reads the value; rules from the whole chain,
        // base first, the order they run in. A list's items get the same, one level deeper.
        private void WriteRules(IReadOnlyList<PrimitiveDescriptor> chain, string indent)
        {
            if (chain.Count == 0)
            {
                return;
            }

            var root = chain[0];

            // A list's converter description says what Items and Form already say.
            if (root.Converter is { } format && root.Element is null)
            {
                Line($"{indent}- **Format:** {Text(format)}");
            }

            if (root.Values is { } values)
            {
                Line($"{indent}- **Values:** {string.Join(", ", values.Select(Code))}");
            }

            var normalizers = chain.SelectMany(primitive => primitive.Normalizers).ToList();
            if (normalizers.Count > 0)
            {
                Line($"{indent}- **Normalized:** {string.Join("; ", normalizers)}");
            }

            var validators = chain.SelectMany(primitive => primitive.Validators).ToList();
            if (validators.Count > 0)
            {
                Line($"{indent}- **Validated:** {string.Join("; ", validators)}");
            }

            if (root.Element is { } element)
            {
                Line($"{indent}- **Items:** {Text(DisplayName(element))}");
                WriteRules(Chain(element), indent + "  ");
            }
        }

        // The primitive and its bases, root first. A descriptor read from a file isn't checked, so a missing base
        // ends the chain and a cycle is cut.
        private List<PrimitiveDescriptor> Chain(PrimitiveReference? reference)
        {
            var chain = new List<PrimitiveDescriptor>();
            var seen = new HashSet<PrimitiveReference>();
            while (reference is not null && seen.Add(reference) && _primitives.TryGetValue(reference, out var primitive))
            {
                chain.Insert(0, primitive);
                reference = primitive.Base;
            }

            return chain;
        }

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

        private static string TypeText(ValueDescriptor value) =>
            Text(value.Primitive is { } reference ? DisplayName(reference) : value.Type);

        // Like Primitive.DisplayName: "Int32 (Port)", or just "Int32" when the name is the type name.
        private static string DisplayName(PrimitiveReference reference) =>
            reference.Name == reference.Type ? reference.Type : $"{reference.Type} ({reference.Name})";

        private static string GroupName(string key) => key.Split(':')[0];

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
