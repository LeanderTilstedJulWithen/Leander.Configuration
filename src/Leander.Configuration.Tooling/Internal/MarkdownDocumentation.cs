using System.Text;
using Leander.Configuration.Descriptors;

namespace Leander.Configuration.Tooling.Internal;

// Documentation as Markdown. Definitions are grouped by their first key segment, and each group has a summary table
// followed by a section per key, with every rule that applies. A primitive used by two or more keys or primitives
// gets its own section at the end, linked from its users. One used once is folded into its user. Primitives with
// nothing to say (no description, format, rules, base, element or values) are never listed.
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
        private readonly HashSet<PrimitiveReference> _listed = Listed(contract);
        private readonly Dictionary<PrimitiveReference, PrimitiveDescriptor> _primitives = Index(contract);
        private readonly Dictionary<PrimitiveReference, Usage> _usages = Usages(contract);
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

                foreach (var primitive in _contract.Primitives.Where(IsListed))
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

            var indexed = definition.Value.Form == ValueForm.Indexed;
            if (indexed)
            {
                Line($"- **Form:** indexed: {Code($"{definition.Key}:0")}, {Code($"{definition.Key}:1")}, …");
            }

            // Every rule that applies, so a key reads on its own. An indexed list has no delimiter in the source.
            if (primitive is not null)
            {
                WriteRules(primitive, "", showDelimiter: !indexed);
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

            // A folded base has its rules here already, so only a listed one is named.
            if (primitive.Base is { } @base && IsListed(@base))
            {
                Line($"- **Derived from:** {PrimitiveText(@base)}");
            }

            WriteRules(primitive, "", showDelimiter: true);

            var reference = Reference(primitive);
            var usage = _usages.GetValueOrDefault(reference) ?? new Usage([], []);
            var derived = usage.Primitives.Where(user => user.Base == reference && IsListed(user)).ToList();
            var usedBy = UsedBy(primitive).Distinct().ToList();

            if (usedBy.Count > 0)
            {
                Line($"- **Used by:** {string.Join(", ", usedBy)}");
            }

            if (derived.Count > 0)
            {
                Line($"- **Derived primitives:** {string.Join(", ", derived.Select(PrimitiveLink))}");
            }

            Line();
        }

        // The keys and listed primitives that use a primitive, looking through folded primitives to their users.
        // Listed derived primitives are left out: they have their own line.
        private IEnumerable<string> UsedBy(PrimitiveDescriptor primitive)
        {
            var reference = Reference(primitive);
            var usage = _usages.GetValueOrDefault(reference) ?? new Usage([], []);

            foreach (var definition in usage.Keys)
            {
                yield return $"[{Code(definition.Key)}](#{Anchor(definition.Key)})";
            }

            foreach (var user in usage.Primitives)
            {
                if (!IsListed(user))
                {
                    foreach (var link in UsedBy(user))
                    {
                        yield return link;
                    }
                }
                else if (user.Base != reference)
                {
                    yield return PrimitiveLink(user);
                }
            }
        }

        private bool IsListed(PrimitiveDescriptor primitive) => _listed.Contains(Reference(primitive));

        private bool IsListed(PrimitiveReference reference) => _listed.Contains(reference);

        // The format, from the furthest base, whose converter it takes. A list's delimiter and items, with the item's
        // rules nested, then the rules of the primitive and its bases, the bases' first, in the order they run.
        // An indexed list (no delimiter shown) isn't read by the list's converter, so its format doesn't apply.
        private void WriteRules(PrimitiveDescriptor primitive, string indent, bool showDelimiter)
        {
            var chain = Chain(primitive);

            if ((showDelimiter || primitive.Element is null) && chain[0].Converter is { } format)
            {
                Line($"{indent}- **Format:** {Text(format)}");
            }

            if (showDelimiter && primitive.Delimiter is { } delimiter)
            {
                Line($"{indent}- **Delimiter:** {Code(delimiter.ToString())}");
            }

            if (primitive.Element is { } element)
            {
                Line($"{indent}- **Items:** {PrimitiveText(element)}");

                if (Find(element) is { } elementPrimitive)
                {
                    WriteRules(elementPrimitive, indent + "  ", showDelimiter: true);
                }
            }

            if (primitive.Values is { } values)
            {
                Line($"{indent}- **Values:** {string.Join(", ", values.Select(Code))}");
            }

            var normalizers = chain.SelectMany(link => link.Normalizers).ToList();
            var validators = chain.SelectMany(link => link.Validators).ToList();

            if (normalizers.Count > 0)
            {
                Line($"{indent}- **Normalized:** {string.Join("; ", normalizers)}");
            }

            if (validators.Count > 0)
            {
                Line($"{indent}- **Validated:** {string.Join("; ", validators)}");
            }
        }

        // The primitive and its bases, the furthest base first.
        private List<PrimitiveDescriptor> Chain(PrimitiveDescriptor primitive)
        {
            var chain = new List<PrimitiveDescriptor>();
            for (PrimitiveDescriptor? current = primitive; current is not null; current = Find(current.Base))
            {
                chain.Insert(0, current);
            }

            return chain;
        }

        private PrimitiveDescriptor? Find(PrimitiveReference? reference) =>
            reference is not null && _primitives.TryGetValue(reference, out var primitive) ? primitive : null;

        private string TypeText(ValueDescriptor value) =>
            value.Primitive is { } reference ? PrimitiveText(reference) : Text(value.Type);

        // A listed primitive links to its description; one with nothing to say is only named.
        private string PrimitiveText(PrimitiveReference reference)
        {
            var heading = Heading(reference.Type, reference.Name);
            return IsListed(reference) ? $"[{Text(heading)}](#{Anchor(heading)})" : Text(heading);
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

        // Worth its own section: something to say, and two or more users to say it to.
        private static HashSet<PrimitiveReference> Listed(ContractDescriptor contract)
        {
            var usages = Usages(contract);
            return
            [
                .. contract.Primitives
                    .Where(primitive => IsWorthListing(primitive))
                    .Select(Reference)
                    .Where(reference => usages.TryGetValue(reference, out var usage) && usage.Count >= 2),
            ];
        }

        private static PrimitiveReference Reference(PrimitiveDescriptor primitive) => new(primitive.Type, primitive.Name);

        // Who uses each primitive: keys directly, and primitives as their base or element.
        private static Dictionary<PrimitiveReference, Usage> Usages(ContractDescriptor contract)
        {
            var usages = new Dictionary<PrimitiveReference, Usage>();

            void Add(PrimitiveReference? reference, DefinitionDescriptor? key, PrimitiveDescriptor? primitive)
            {
                if (reference is null)
                {
                    return;
                }

                if (!usages.TryGetValue(reference, out var usage))
                {
                    usages[reference] = usage = new Usage([], []);
                }

                if (key is not null)
                {
                    usage.Keys.Add(key);
                }

                if (primitive is not null)
                {
                    usage.Primitives.Add(primitive);
                }
            }

            foreach (var definition in contract.Definitions)
            {
                Add(definition.Value.Primitive, definition, null);
            }

            foreach (var primitive in contract.Primitives)
            {
                Add(primitive.Base, null, primitive);
                Add(primitive.Element, null, primitive);
            }

            return usages;
        }

        private static bool IsWorthListing(PrimitiveDescriptor primitive) =>
            primitive.Description is not null || HasDetails(primitive);

        // The lines under a primitive's description. A list primitive always has its element.
        private static bool HasDetails(PrimitiveDescriptor primitive) =>
            primitive.Converter is not null ||
            primitive.Base is not null ||
            primitive.Element is not null ||
            primitive.Normalizers.Count > 0 ||
            primitive.Validators.Count > 0 ||
            primitive.Values is not null;

        private static string GroupName(string key) => key.Split(':')[0];

        private static string Heading(PrimitiveDescriptor primitive) => Heading(primitive.Type, primitive.Name);

        // The keys and primitives that use one primitive.
        private sealed record Usage(List<DefinitionDescriptor> Keys, List<PrimitiveDescriptor> Primitives)
        {
            public int Count => Keys.Count + Primitives.Count;
        }

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
