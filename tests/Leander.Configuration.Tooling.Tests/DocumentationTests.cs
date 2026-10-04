using Leander.Primitives;
using Leander.Primitives.Validation;

namespace Leander.Configuration.Tooling.Tests;

public class DocumentationTests
{
    private static readonly string Markdown = Documentation.WriteMarkdown(Contracts.Server(), "Server configuration");

    [Fact]
    public void StartsWithTitle()
    {
        Assert.StartsWith("# Server configuration\n\n", Markdown);
    }

    [Fact]
    public void GroupsByFirstKeySegment_InOrderOfFirstAppearance()
    {
        Assert.True(Markdown.IndexOf("## Server\n", StringComparison.Ordinal) < Markdown.IndexOf("## Database\n", StringComparison.Ordinal));
    }

    [Fact]
    public void SummaryTable_LinksKeys()
    {
        Assert.Contains("| [`Server:Port`](#serverport) | Int32 (Port) | required |  |", Markdown);
        Assert.Contains("| [`Server:Host`](#serverhost) | String | default | `localhost` |", Markdown);
        Assert.Contains("| [`Server:MaxConnections`](#servermaxconnections) | Int32 | optional |  |", Markdown);
    }

    [Fact]
    public void KeySection_HasEverythingAboutTheKey()
    {
        Assert.Contains(
            "### `Server:Port`\n\nThe port to listen on.\n\n- **Type:** Int32 (Port)\n- **Format:** A 32-bit integer.\n" +
            "- **Validated:** must be between 1 and 65535\n- **Presence:** required\n",
            Markdown);
        Assert.Contains("- **Presence:** optional, missing is null", Markdown);
    }

    [Fact]
    public void Indexed_ShowsForm()
    {
        Assert.Contains("- **Form:** indexed: `Server:AllowedOrigins:0`, `Server:AllowedOrigins:1`, …", Markdown);
    }

    [Fact]
    public void EmptyDefault_IsShownAsEmpty()
    {
        Assert.Contains("- **Presence:** default *empty*", Markdown);
    }

    [Fact]
    public void Sensitive_HidesDefault()
    {
        Assert.Contains("| [`Database:Password`](#databasepassword) | String | default | *hidden* |", Markdown);
        Assert.Contains("- **Presence:** default (hidden)", Markdown);
        Assert.Contains("- **Sensitive:**", Markdown);
        Assert.DoesNotContain("hunter2", Markdown);
    }

    [Fact]
    public void Primitives_HaveNoSectionsOrLinks_AndTheirDescriptionIsNotShown()
    {
        Assert.DoesNotContain("## Primitives", Markdown);
        Assert.DoesNotContain("](#int32", Markdown);
        Assert.DoesNotContain("A TCP port.", Markdown);
    }

    [Fact]
    public void SharedPrimitive_IsDocumentedOnEveryKey()
    {
        var markdown = Documentation.WriteMarkdown(Contracts.Describe(
            ConfigurationDefinition.Define("A", Contracts.Port),
            ConfigurationDefinition.Define("B", Contracts.Port)));

        var section = "- **Type:** Int32 (Port)\n- **Format:** A 32-bit integer.\n- **Validated:** must be between 1 and 65535\n";
        Assert.Equal(2, markdown.Split(section).Length - 1);
    }

    [Fact]
    public void Derived_CollectsFormatAndRulesAlongTheChain_BaseFirst()
    {
        var adminPort = new Primitive<int>("AdminPort", Contracts.Port) { Validators = [Validators.GreaterThan(1024)] };
        var markdown = Documentation.WriteMarkdown(Contracts.Describe(
            ConfigurationDefinition.Define("Admin:Port", adminPort)));

        Assert.Contains(
            "- **Type:** Int32 (AdminPort)\n- **Format:** A 32-bit integer.\n" +
            "- **Validated:** must be between 1 and 65535; must be greater than 1024\n",
            markdown);
    }

    [Fact]
    public void ListPrimitive_ShowsFormRulesAndItems_WithEscapedAngleBrackets()
    {
        Assert.Contains(
            "- **Type:** IReadOnlyList\\<Uri\\> (Origins)\n" +
            "- **Form:** indexed: `Server:AllowedOrigins:0`, `Server:AllowedOrigins:1`, …\n" +
            "- **Validated:** must not be empty\n" +
            "- **Items:** Uri\n  - **Format:** An absolute URI, e.g. https://example.com.\n" +
            "- **Presence:** required\n",
            Markdown);
        Assert.Contains("- **Form:** one entry, items separated by `,`\n- **Items:** String\n  - **Format:** Any text.\n", Markdown);
    }

    [Fact]
    public void ListItems_ShowTheElementsFormatAndRulesNested()
    {
        var markdown = Documentation.WriteMarkdown(Contracts.Describe(
            ConfigurationDefinition.Define("Ports", new ListPrimitive<int>("Ports", Contracts.Port))));

        Assert.Contains("- **Items:** Int32 (Port)\n  - **Format:** A 32-bit integer.\n  - **Validated:** must be between 1 and 65535\n", markdown);
    }

    [Fact]
    public void Enum_ShowsValues()
    {
        var markdown = Documentation.WriteMarkdown(Contracts.Describe(
            ConfigurationDefinition.Define("Day", Primitive.Enum<DayOfWeek>())));

        Assert.Contains("- **Values:** `Sunday`, `Monday`", markdown);
    }

    [Fact]
    public void LinesEndInNewlineOnly()
    {
        Assert.DoesNotContain("\r", Markdown);
    }
}
