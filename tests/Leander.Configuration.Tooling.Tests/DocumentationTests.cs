using Leander.Primitives;
using Leander.Primitives.Validation;

namespace Leander.Configuration.Tooling.Tests;

public class DocumentationTests
{
    private static readonly string Markdown = Documentation.WriteMarkdown(Contracts.Server(), "Server configuration");

    // The whole document, so that any change to the layout fails here and has to be made on purpose.
    [Fact]
    public void Server_RendersEverything()
    {
        Assert.Equal(
            """
            # Server configuration

            ## Server

            | Key | Type | Presence | Default |
            |-----|------|----------|---------|
            | [`Server:Host`](#serverhost) | String | default | `localhost` |
            | [`Server:Port`](#serverport) | Int32 (Port) | required |  |
            | [`Server:MaxConnections`](#servermaxconnections) | Int32 | optional |  |
            | [`Server:AllowedOrigins`](#serverallowedorigins) | IReadOnlyList\<Uri\> (Origins) | required |  |
            | [`Server:Features`](#serverfeatures) | IReadOnlyList\<String\> (Features) | default | *empty* |

            ### `Server:Host`

            The host name.

            - **Type:** String
            - **Format:** Any text.
            - **Presence:** default `localhost`

            ### `Server:Port`

            The port to listen on.

            - **Type:** Int32 (Port)
            - **Format:** A 32-bit integer.
            - **Validated:** must be between 1 and 65535
            - **Presence:** required

            ### `Server:MaxConnections`

            - **Type:** Int32
            - **Format:** A 32-bit integer.
            - **Presence:** optional, missing is null

            ### `Server:AllowedOrigins`

            - **Type:** IReadOnlyList\<Uri\> (Origins)
            - **Form:** indexed: `Server:AllowedOrigins:0`, `Server:AllowedOrigins:1`, …
            - **Validated:** must not be empty
            - **Items:** Uri
              - **Format:** An absolute URI, e.g. https://example.com.
            - **Presence:** required

            ### `Server:Features`

            - **Type:** IReadOnlyList\<String\> (Features)
            - **Form:** one entry, items separated by `,`
            - **Items:** String
              - **Format:** Any text.
            - **Presence:** default *empty*

            ## Database

            | Key | Type | Presence | Default |
            |-----|------|----------|---------|
            | [`Database:Password`](#databasepassword) | String | default | *hidden* |

            ### `Database:Password`

            - **Type:** String
            - **Format:** Any text.
            - **Presence:** default (hidden)
            - **Sensitive:** the value is never shown in diagnostics or documentation.


            """.ReplaceLineEndings("\n"),
            Markdown);
    }

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
