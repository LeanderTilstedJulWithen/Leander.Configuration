using static Leander.Configuration.Tests.TestHelpers;

namespace Leander.Configuration.Tests;

public class CollectionDefinitionTests
{
    [Fact]
    public void Indexed_ReadsElementsInNumericOrder()
    {
        var definition = ConfigurationDefinition.Define<int>("Ports").Indexed();
        var reader = Reader(("Ports:10", "10"), ("Ports:2", "2"), ("Ports:0", "0"), ("Ports:1", "1"),
            ("Ports:3", "3"), ("Ports:4", "4"), ("Ports:5", "5"), ("Ports:6", "6"), ("Ports:7", "7"), ("Ports:8", "8"), ("Ports:9", "9"));

        Assert.Equal([0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10], reader.Get(definition));
        Assert.Empty(reader.Diagnostics);
    }

    [Fact]
    public void Indexed_ElementAndListValidators_ApplyToTheirOwnLevel()
    {
        var definition = ConfigurationDefinition.Define<int>("Ports")
            .Validate(Validators.GreaterThan(0))
            .Indexed()
            .Validate(Validators.Create<IReadOnlyList<int>>("must have at most one element", list => list.Count <= 1));
        var reader = Reader(("Ports:0", "80"), ("Ports:1", "0"));

        reader.Get(definition);

        var errors = Errors(reader).ToList();
        Assert.Single(errors, e => e.Key == "Ports:1" && e.Message == "must be greater than 0");
        Assert.DoesNotContain(errors, e => e.Message == "must have at most one element");
    }

    [Fact]
    public void Indexed_ListValidator_RunsWhenElementsAreValid()
    {
        var definition = ConfigurationDefinition.Define<int>("Ports")
            .Indexed()
            .Validate(Validators.Create<IReadOnlyList<int>>("must have at most one element", list => list.Count <= 1));
        var reader = Reader(("Ports:0", "80"), ("Ports:1", "81"));

        reader.Get(definition);

        Assert.Equal("must have at most one element", SingleError(reader).Message);
    }

    [Fact]
    public void Indexed_CollectionsNotEmpty_AppliesToAnyList()
    {
        var definition = ConfigurationDefinition.Define<string>("Names")
            .Indexed()
            .Default([])
            .Validate(Validators.Collections.NotEmpty);
        var reader = Reader();

        Assert.False(reader.TryGet(definition, out _));
        Assert.Equal("must not be empty", SingleError(reader).Message);
    }

    [Fact]
    public void Indexed_Missing_UsesListLevelPresenceRules()
    {
        var definition = ConfigurationDefinition.Define<string>("Names").Required().Indexed();
        var reader = Reader();

        Assert.True(definition.IsRequired);
        Assert.False(reader.TryGet(definition, out _));
        Assert.Equal("value is required", SingleError(reader).Message);
    }

    [Fact]
    public void Indexed_Gap_ReportsWarning()
    {
        var definition = ConfigurationDefinition.Define<int>("Ports").Indexed();
        var reader = Reader(("Ports:0", "80"), ("Ports:2", "82"));

        Assert.Equal([80, 82], reader.Get(definition));
        Assert.Contains(reader.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning);
    }

    [Fact]
    public void Indexed_NonIntegerIndex_ReportsError()
    {
        var definition = ConfigurationDefinition.Define<int>("Ports").Indexed();
        var reader = Reader(("Ports:0", "80"), ("Ports:http", "81"));

        Assert.False(reader.TryGet(definition, out _));
        Assert.Equal("'http' is not a valid index", SingleError(reader).Message);
    }

    [Fact]
    public void Indexed_DuplicateIndex_ReportsError()
    {
        var definition = ConfigurationDefinition.Define<int>("Ports").Indexed();
        var reader = Reader(("Ports:1", "80"), ("Ports:01", "81"), ("Ports:0", "79"));

        Assert.False(reader.TryGet(definition, out _));
        Assert.Equal("index 1 is defined more than once", SingleError(reader).Message);
    }

    [Fact]
    public void Indexed_SingleValueInsteadOfEntries_ReportsWarning()
    {
        var definition = ConfigurationDefinition.Define<int>("Ports").Indexed();
        var reader = Reader(("Ports", "80,81"));

        reader.Get(definition);

        Assert.Contains(reader.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning);
    }

    [Fact]
    public void Indexed_Nested_ReadsJaggedList()
    {
        var definition = ConfigurationDefinition.Define<int>("Grid").Indexed().Indexed();
        var reader = Reader(("Grid:0:0", "1"), ("Grid:0:1", "2"), ("Grid:1:0", "3"));

        var grid = reader.Get(definition);

        Assert.Equal(2, grid.Count);
        Assert.Equal([1, 2], grid[0]);
        Assert.Equal([3], grid[1]);
    }

    [Fact]
    public void Delimited_ReadsElements()
    {
        var definition = ConfigurationDefinition.Define<int>("Ports").Delimited();
        var reader = Reader(("Ports", "80, 443"));

        Assert.Equal([80, 443], reader.Get(definition));
    }

    [Fact]
    public void Delimited_CustomDelimiter_IsUsed()
    {
        var definition = ConfigurationDefinition.Define<string>("Paths").Delimited(';');
        var reader = Reader(("Paths", "a;b"));

        Assert.Equal(["a", "b"], reader.Get(definition));
    }

    [Fact]
    public void Delimited_ElementValidator_ReportsItemPosition()
    {
        var definition = ConfigurationDefinition.Define<int>("Ports").Validate(Validators.GreaterThan(0)).Delimited();
        var reader = Reader(("Ports", "80,0"));

        reader.Get(definition);

        Assert.Equal("Ports[1]", SingleError(reader).Key);
    }

    [Fact]
    public void Delimited_InvalidElement_ReportsParseError()
    {
        var definition = ConfigurationDefinition.Define<int>("Ports").Delimited();
        var reader = Reader(("Ports", "80,abc"));

        Assert.False(reader.TryGet(definition, out _));
        Assert.Equal("'80,abc' is not a valid ','-delimited list of Int32", SingleError(reader).Message);
    }

    [Fact]
    public void Delimited_IndexedEntriesInsteadOfValue_ReportsWarning()
    {
        var definition = ConfigurationDefinition.Define<int>("Ports").Delimited();
        var reader = Reader(("Ports:0", "80"));

        reader.Get(definition);

        Assert.Contains(reader.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning);
    }

    [Fact]
    public void Delimited_OnIndexedElement_ReportsError()
    {
        var definition = ConfigurationDefinition.Define<int>("Grid").Indexed().Delimited();
        var reader = Reader(("Grid", "1,2"));

        Assert.False(reader.TryGet(definition, out _));
        Assert.Equal("Delimited() requires a single-value element definition", SingleError(reader).Message);
    }
}
