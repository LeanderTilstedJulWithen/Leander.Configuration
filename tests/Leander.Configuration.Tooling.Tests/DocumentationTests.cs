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
    public void KeySection_HasDescriptionTypePresenceAndRules()
    {
        Assert.Contains(
            "### `Server:Port`\n\nThe port to listen on.\n\n- **Type:** Int32 (Port): A TCP port.\n- **Presence:** required\n" +
            "- **Validated:** must be between 1 and 65535\n",
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
    public void SingleUsePrimitives_AreFolded()
    {
        Assert.DoesNotContain("## Primitives", Markdown);
        Assert.DoesNotContain("](#int32-port)", Markdown);
    }

    [Fact]
    public void SharedPrimitive_IsListed_WithUsedBy()
    {
        var markdown = Documentation.WriteMarkdown(Contracts.Describe(
            ConfigurationDefinition.Define("A", Contracts.Port),
            ConfigurationDefinition.Define("B", Contracts.Port)));

        Assert.Contains("- **Type:** [Int32 (Port)](#int32-port): A TCP port.\n", markdown);
        Assert.Contains(
            "### Int32 (Port)\n\nA TCP port.\n\n- **Validated:** must be between 1 and 65535\n- **Used by:** [`A`](#a), [`B`](#b)\n",
            markdown);
    }

    [Fact]
    public void UsedBy_LooksThroughFoldedPrimitives()
    {
        var markdown = Documentation.WriteMarkdown(Contracts.Describe(
            ConfigurationDefinition.Define("Server:Port", Contracts.Port),
            ConfigurationDefinition.Define("Admin:Port", new Primitive<int>("AdminPort", Contracts.Port))));

        Assert.Contains("- **Used by:** [`Server:Port`](#serverport), [`Admin:Port`](#adminport)\n", markdown);
        Assert.Contains("- **Type:** Int32 (AdminPort)\n- **Presence:** required\n- **Validated:** must be between 1 and 65535\n", markdown);
        Assert.DoesNotContain("Derived primitives", markdown);
    }

    [Fact]
    public void ListedDerived_LinksToBase_AndBaseLinksToIt()
    {
        var adminPort = new Primitive<int>("AdminPort", Contracts.Port) { Validators = [Validators.GreaterThan(1024)] };
        var markdown = Documentation.WriteMarkdown(Contracts.Describe(
            ConfigurationDefinition.Define("Server:Port", Contracts.Port),
            ConfigurationDefinition.Define("Admin:Port", adminPort),
            ConfigurationDefinition.Define("Admin:BackupPort", adminPort)));

        Assert.Contains(
            "### Int32 (AdminPort)\n\n- **Derived from:** [Int32 (Port)](#int32-port)\n" +
            "- **Validated:** must be between 1 and 65535; must be greater than 1024\n" +
            "- **Used by:** [`Admin:Port`](#adminport), [`Admin:BackupPort`](#adminbackupport)\n",
            markdown);
        Assert.Contains("- **Used by:** [`Server:Port`](#serverport)\n- **Derived primitives:** [Int32 (AdminPort)](#int32-adminport)\n", markdown);
    }

    [Fact]
    public void ListPrimitive_ShowsItemsAndRules_WithEscapedAngleBrackets()
    {
        Assert.Contains(
            "- **Type:** IReadOnlyList\\<Uri\\> (Origins)\n- **Presence:** required\n" +
            "- **Form:** indexed: `Server:AllowedOrigins:0`, `Server:AllowedOrigins:1`, …\n" +
            "- **Items:** Uri\n- **Validated:** must not be empty\n",
            Markdown);
        Assert.Contains("- **Delimiter:** `,`\n- **Items:** String\n", Markdown);
    }

    [Fact]
    public void ListItems_ShowTheElementsRulesNested()
    {
        var markdown = Documentation.WriteMarkdown(Contracts.Describe(
            ConfigurationDefinition.Define("Ports", new ListPrimitive<int>("Ports", Contracts.Port))));

        Assert.Contains("- **Items:** Int32 (Port)\n  - **Validated:** must be between 1 and 65535\n", markdown);
    }

    [Fact]
    public void PrimitivesWithNothingToSay_AreNotListed()
    {
        Assert.DoesNotContain("### String", Markdown);
        Assert.DoesNotContain("### Int32\n", Markdown);
    }

    [Fact]
    public void FoldedBase_IsNotNamed()
    {
        var count = new Primitive<int>("Count", Primitive.Int32) { Description = "A count." };
        var markdown = Documentation.WriteMarkdown(Contracts.Describe(
            ConfigurationDefinition.Define("A", count),
            ConfigurationDefinition.Define("B", count)));

        Assert.Contains("### Int32 (Count)\n", markdown);
        Assert.DoesNotContain("Derived from", markdown);
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
