using Leander.Configuration.Descriptors;
using Leander.Configuration.Tooling.Internal;

namespace Leander.Configuration.Tooling;

/// <summary>
/// Writes documentation for a contract descriptor.
/// </summary>
public static class Documentation
{
    /// <summary>
    /// Writes the documentation for <paramref name="contract"/> as Markdown, under a level-1 heading <paramref name="title"/>.
    /// </summary>
    /// <remarks>
    /// Definitions are grouped by their first key segment, and each group has a summary table followed by a section per key.
    /// Primitives are described once, at the end, and linked from the keys that use them.
    /// Primitives with nothing to say (no description, rules, base, element or values) are left out.
    /// </remarks>
    public static string WriteMarkdown(ContractDescriptor contract, string title = "Configuration") =>
        MarkdownDocumentation.Write(contract, title);
}
