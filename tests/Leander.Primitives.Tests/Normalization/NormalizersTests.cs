using Leander.Primitives.Normalization;

namespace Leander.Primitives.Tests.Normalization;

public class NormalizersTests
{
    [Fact]
    public void Create_UsesGivenDescriptionAndFunction()
    {
        var normalizer = Normalizers.Create<int>("absolute value", Math.Abs);

        Assert.Equal("absolute value", normalizer.Description);
        Assert.Equal(5, normalizer.Normalize(-5));
    }

    [Theory]
    [InlineData("value", "value")]
    [InlineData("  value  ", "value")]
    [InlineData("\t value \n", "value")]
    [InlineData("   ", "")]
    public void Trim_RemovesSurroundingWhitespace(string input, string expected)
    {
        Assert.Equal("trim whitespace", Normalizers.Trim.Description);
        Assert.Equal(expected, Normalizers.Trim.Normalize(input));
    }

    [Fact]
    public void FullPath_ResolvesRelativePathsAgainstCurrentDirectory()
    {
        var expected = Path.Combine(Directory.GetCurrentDirectory(), "b");

        Assert.Equal("full path", Normalizers.FullPath.Description);
        Assert.Equal(expected, Normalizers.FullPath.Normalize(Path.Combine("a", "..", "b")));
    }

    [Fact]
    public void FullPath_KeepsAbsolutePaths()
    {
        var path = Path.Combine(Path.GetTempPath(), "file.txt");

        Assert.Equal(path, Normalizers.FullPath.Normalize(path));
    }
}
