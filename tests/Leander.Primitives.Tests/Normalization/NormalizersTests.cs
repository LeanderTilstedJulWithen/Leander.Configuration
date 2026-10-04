using Leander.Primitives.Normalization;
using Leander.Primitives.Parsing;

namespace Leander.Primitives.Tests.Normalization;

public class NormalizersTests
{
    [Fact]
    public void Create_UsesGivenDescriptionAndFunction()
    {
        var normalizer = Normalizers.Create<int>("absolute value", Math.Abs);

        Assert.Equal("absolute value", normalizer.Description.ToString());
        Assert.Equal(5, normalizer.Normalize(-5));
    }

    [Fact]
    public void Bounds_AreFormattedWithTheGivenFormatter()
    {
        var normalizer = Normalizers.UpperBound(TimeSpan.FromMinutes(5));

        Assert.Equal("upper bound 00:05:00", normalizer.Description.FormatWith(Converters.TimeSpan));
        Assert.Equal(TimeSpan.FromMinutes(5), normalizer.Normalize(TimeSpan.FromHours(1)));
    }

    [Theory]
    [InlineData("value", "value")]
    [InlineData("  value  ", "value")]
    [InlineData("\t value \n", "value")]
    [InlineData("   ", "")]
    public void Trim_RemovesSurroundingWhitespace(string input, string expected)
    {
        Assert.Equal("trim whitespace", Normalizers.Strings.Trim.Description.ToString());
        Assert.Equal(expected, Normalizers.Strings.Trim.Normalize(input));
    }

    [Fact]
    public void FullPath_ResolvesRelativePathsAgainstCurrentDirectory()
    {
        var expected = Path.Combine(Directory.GetCurrentDirectory(), "b");

        Assert.Equal("full path", Normalizers.Strings.FullPath.Description.ToString());
        Assert.Equal(expected, Normalizers.Strings.FullPath.Normalize(Path.Combine("a", "..", "b")));
    }

    [Fact]
    public void FullPath_KeepsAbsolutePaths()
    {
        var path = Path.Combine(Path.GetTempPath(), "file.txt");

        Assert.Equal(path, Normalizers.Strings.FullPath.Normalize(path));
    }
}
