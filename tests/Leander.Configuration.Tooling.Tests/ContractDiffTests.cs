using Leander.Primitives;
using Leander.Primitives.Parsing;
using Leander.Primitives.Validation;

namespace Leander.Configuration.Tooling.Tests;

public class ContractDiffTests
{
    [Fact]
    public void SameContract_HasNoDifferences()
    {
        Assert.Empty(ContractDiff.Compare(Contracts.Server(), Contracts.Server()));
    }

    [Fact]
    public void AddedAndRemovedKeys()
    {
        var left = Contracts.Describe(
            ConfigurationDefinition.Define("A", Primitive.Int32),
            ConfigurationDefinition.Define("B", Primitive.Int32));
        var right = Contracts.Describe(
            ConfigurationDefinition.Define("B", Primitive.Int32),
            ConfigurationDefinition.Define("C", Primitive.Int32));

        var differences = ContractDiff.Compare(left, right);

        Assert.Equal(
            [new ContractDifference("A", DifferenceKind.Removed), new ContractDifference("C", DifferenceKind.Added)],
            differences);
        Assert.Equal(["A: removed", "C: added"], differences.Select(difference => difference.ToString()));
    }

    [Fact]
    public void PresenceAndDefault_AreSeparateAspects()
    {
        var differences = Compare(
            ConfigurationDefinition.Define("Timeout", Primitive.TimeSpan).Default(TimeSpan.FromSeconds(30)),
            ConfigurationDefinition.Define("Timeout", Primitive.TimeSpan));

        Assert.Equal(2, differences.Count);
        Assert.Equal("Timeout: presence: default → required", Changed(differences, DifferenceAspect.Presence).ToString());
        Assert.Equal("Timeout: default: 00:00:30 → none", Changed(differences, DifferenceAspect.Default).ToString());
    }

    [Fact]
    public void Description_IsItsOwnAspect()
    {
        var difference = Changed(
            Compare(
                ConfigurationDefinition.Define("Port", Primitive.Int32).Describe("Old."),
                ConfigurationDefinition.Define("Port", Primitive.Int32).Describe("New.")),
            DifferenceAspect.Description);

        Assert.Equal(DifferenceKind.Changed, difference.Kind);
        Assert.Null(difference.Primitive);
        Assert.Equal("Old.", difference.Left);
        Assert.Equal("New.", difference.Right);
    }

    [Fact]
    public void SensitiveAndForm_AreAspects()
    {
        var list = new ListPrimitive<int>("Ports", Primitive.Int32);

        var differences = Compare(
            ConfigurationDefinition.Define("Ports", list),
            ConfigurationDefinition.Indexed("Ports", list).Sensitive());

        Assert.Equal("scalar", Changed(differences, DifferenceAspect.Form).Left);
        Assert.Equal("true", Changed(differences, DifferenceAspect.Sensitive).Right);
    }

    [Fact]
    public void KeysMatchCaseInsensitively_SpellingIsAnAspect()
    {
        var difference = Assert.Single(Compare(
            ConfigurationDefinition.Define("Server:Port", Primitive.Int32),
            ConfigurationDefinition.Define("server:port", Primitive.Int32)));

        Assert.Equal(DifferenceAspect.Key, difference.Aspect);
        Assert.Equal("server:port", difference.Right);
    }

    [Fact]
    public void DifferentPrimitive_ReportsOnlyTheReference()
    {
        var differences = Compare(
            ConfigurationDefinition.Define("Flags", Primitive.Int32),
            ConfigurationDefinition.Define("Flags", Primitive.Int32Hex));

        var difference = Assert.Single(differences);
        Assert.Equal("Flags: primitive: Int32 → Int32 (Hex)", difference.ToString());
    }

    [Fact]
    public void ChangedPrimitive_IsReportedOnEveryKeyUsingIt()
    {
        var oldPort = new Primitive<int>("Port", Converters.Int32) { Validators = [Validators.InRange(1, 65535)] };
        var newPort = new Primitive<int>("Port", Converters.Int32) { Validators = [Validators.InRange(1024, 65535)] };
        var left = Contracts.Describe(
            ConfigurationDefinition.Define("Server:Port", oldPort),
            ConfigurationDefinition.Define("Admin:Port", oldPort));
        var right = Contracts.Describe(
            ConfigurationDefinition.Define("Server:Port", newPort),
            ConfigurationDefinition.Define("Admin:Port", newPort));

        var differences = ContractDiff.Compare(left, right);

        Assert.Equal(["Server:Port", "Admin:Port"], differences.Select(difference => difference.Key));
        Assert.All(differences, difference =>
        {
            Assert.Equal(DifferenceAspect.Validators, difference.Aspect);
            Assert.Equal("Int32 (Port)", difference.Primitive);
        });
        Assert.Equal(
            "Server:Port: validators of Int32 (Port): [must be between 1 and 65535] → [must be between 1024 and 65535]",
            differences[0].ToString());
    }

    [Fact]
    public void ChangedBase_IsFoundThroughTheDerivedPrimitive()
    {
        var oldPort = new Primitive<int>("Port", Converters.Int32);
        var newPort = new Primitive<int>("Port", Converters.Int32) { Description = "A TCP port." };

        var difference = Assert.Single(Compare(
            ConfigurationDefinition.Define("Admin:Port", new Primitive<int>("AdminPort", oldPort)),
            ConfigurationDefinition.Define("Admin:Port", new Primitive<int>("AdminPort", newPort))));

        Assert.Equal(DifferenceAspect.Description, difference.Aspect);
        Assert.Equal("Int32 (Port)", difference.Primitive);
    }

    [Fact]
    public void ChangedElementAndDelimiter_OfAList()
    {
        var oldList = new ListPrimitive<int>("Ports", new Primitive<int>("Port", Converters.Int32));
        var newList = new ListPrimitive<int>("Ports", new Primitive<int>("Port", Converters.Int32) { Validators = [Validators.GreaterThan(0)] }, ';');

        var differences = Compare(
            ConfigurationDefinition.Define("Ports", oldList),
            ConfigurationDefinition.Define("Ports", newList));

        Assert.Equal(",", Changed(differences, DifferenceAspect.Delimiter).Left);
        Assert.Equal("Int32 (Port)", Changed(differences, DifferenceAspect.Validators).Primitive);
    }

    [Fact]
    public void IndexedDefault_IsComparedByItems()
    {
        var rows = new ListPrimitive<IReadOnlyList<int>>("Rows", new ListPrimitive<int>("Row", Primitive.Int32));

        var difference = Changed(
            Compare(
                ConfigurationDefinition.Indexed("Rows", rows).Default([[1, 2]]),
                ConfigurationDefinition.Indexed("Rows", rows).Default([[1], [2]])),
            DifferenceAspect.Default);

        Assert.Equal("[1,2]", difference.Left);
        Assert.Equal("[1, 2]", difference.Right);
    }

    // Type names

    [Fact]
    public void AddingACollidingType_DoesNotChangeTheKeysThatUseTheOther()
    {
        var statuses = new ListPrimitive<Billing.Status>("Statuses", Primitive.Enum<Billing.Status>());
        var left = Contracts.Describe(
            ConfigurationDefinition.Define("Status", Primitive.Enum<Billing.Status>()),
            ConfigurationDefinition.Define("Statuses", statuses));
        var right = Contracts.Describe(
            ConfigurationDefinition.Define("Status", Primitive.Enum<Billing.Status>()),
            ConfigurationDefinition.Define("Statuses", statuses),
            ConfigurationDefinition.Define("Shipping", Primitive.Enum<Shipping.Status>()));

        Assert.Equal("Status", left.Definitions[0].Value.Type);
        Assert.Equal("Billing.Status", right.Definitions[0].Value.Type);
        Assert.Equal([new ContractDifference("Shipping", DifferenceKind.Added)], ContractDiff.Compare(left, right));
    }

    [Fact]
    public void CollidingTypesSwapped_AreReported()
    {
        var left = Contracts.Describe(
            ConfigurationDefinition.Define("A", Primitive.Enum<Billing.Status>()),
            ConfigurationDefinition.Define("B", Primitive.Enum<Shipping.Status>()));
        var right = Contracts.Describe(
            ConfigurationDefinition.Define("A", Primitive.Enum<Shipping.Status>()),
            ConfigurationDefinition.Define("B", Primitive.Enum<Billing.Status>()));

        var differences = ContractDiff.Compare(left, right).Where(difference => difference.Key == "A").ToList();

        Assert.Equal("A: type: Billing.Status → Shipping.Status", Changed(differences, DifferenceAspect.Type).ToString());
        Assert.Equal(
            "A: primitive: Billing.Status (Status) → Shipping.Status (Status)",
            Changed(differences, DifferenceAspect.Primitive).ToString());
    }

    [Fact]
    public void TypeNames_MatchOnlyAtANamespaceBoundary()
    {
        var differences = Compare(
            ConfigurationDefinition.Define("Status", new Primitive<OrderStatus>("Status", Converters.Enum<OrderStatus>())),
            ConfigurationDefinition.Define("Status", Primitive.Enum<Billing.Status>()));

        Assert.Equal("Status: type: OrderStatus → Status", Changed(differences, DifferenceAspect.Type).ToString());
    }

    private static ContractDifference Changed(IReadOnlyList<ContractDifference> differences, DifferenceAspect aspect) =>
        Assert.Single(differences, difference => difference.Aspect == aspect);

    private static IReadOnlyList<ContractDifference> Compare(ConfigurationDefinition left, ConfigurationDefinition right) =>
        ContractDiff.Compare(Contracts.Describe(left), Contracts.Describe(right));
}
