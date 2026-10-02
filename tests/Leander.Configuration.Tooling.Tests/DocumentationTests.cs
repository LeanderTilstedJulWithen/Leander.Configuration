using Leander.Primitives;

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
    public void SummaryTable_LinksKeysAndPrimitives()
    {
        Assert.Contains("| [`Server:Port`](#serverport) | [Int32 (Port)](#int32-port) | required |  |", Markdown);
        Assert.Contains("| [`Server:Host`](#serverhost) | String | default | `localhost` |", Markdown);
        Assert.Contains("| [`Server:MaxConnections`](#servermaxconnections) | Int32 | optional |  |", Markdown);
    }

    [Fact]
    public void KeySection_HasDescriptionTypeAndPresence()
    {
        Assert.Contains("### `Server:Port`\n\nThe port to listen on.\n\n- **Type:** [Int32 (Port)](#int32-port)\n- **Presence:** required\n", Markdown);
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
    public void Primitives_AreDescribedOnce()
    {
        Assert.Contains("## Primitives\n", Markdown);
        Assert.Contains("### Int32 (Port)\n\nA TCP port.\n\n- **Validated:** must be between 1 and 65535\n", Markdown);
    }

    [Fact]
    public void ListPrimitive_ShowsElementAndDelimiter_WithEscapedAngleBrackets()
    {
        Assert.Contains("### IReadOnlyList\\<Uri\\> (Origins)\n\n- **Element:** Uri\n- **Delimiter:** `,`\n- **Validated:** must not be empty\n", Markdown);
    }

    [Fact]
    public void PrimitivesWithNothingToSay_AreNotListed()
    {
        Assert.DoesNotContain("### String", Markdown);
        Assert.DoesNotContain("### Int32\n", Markdown);
    }

    [Fact]
    public void Derived_NamesItsBase()
    {
        var markdown = Documentation.WriteMarkdown(Contracts.Describe(
            ConfigurationDefinition.Define("Admin:Port", new Primitive<int>("AdminPort", Contracts.Port)),
            ConfigurationDefinition.Define("Count", new Primitive<int>("Count", Primitive.Int32))));

        Assert.Contains("- **Derived from:** [Int32 (Port)](#int32-port)", markdown);
        Assert.Contains("- **Derived from:** Int32\n", markdown);
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
