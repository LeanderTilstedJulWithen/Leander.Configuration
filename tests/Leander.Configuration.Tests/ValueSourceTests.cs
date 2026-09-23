namespace Leander.Configuration.Tests;

public class ValueSourceTests
{
    [Fact]
    public void GetChildNames_ReturnsDirectChildrenOnce()
    {
        var source = ValueSource.FromDictionary([
            new("A:0", "x"),
            new("A:1:Name", "y"),
            new("A:1:Value", "z"),
            new("AB:0", "not a child"),
        ]);

        Assert.Equal(["0", "1"], source.GetChildNames("A"));
    }

    [Fact]
    public void GetValue_MissingKey_ReturnsNull()
    {
        var source = ValueSource.FromDictionary([]);

        Assert.Null(source.GetValue("A"));
    }
}
