using System.Globalization;
using Leander.Primitives.Parsing;

namespace Leander.Primitives.Tests.Parsing;

public class DateTimeConverterTests
{
    [Fact]
    public void DateTimeUtc_ParsesUtcTimestamp()
    {
        Assert.True(Converters.DateTimeUtc.TryParse("2024-01-02T03:04:05Z", out var result));

        Assert.Equal(new DateTime(2024, 1, 2, 3, 4, 5), result);
        Assert.Equal(DateTimeKind.Utc, result.Kind);
    }

    [Fact]
    public void DateTimeUtc_AdjustsOffsetToUtc()
    {
        Assert.True(Converters.DateTimeUtc.TryParse("2024-01-02T03:04:05+02:00", out var result));

        Assert.Equal(new DateTime(2024, 1, 2, 1, 4, 5), result);
        Assert.Equal(DateTimeKind.Utc, result.Kind);
    }

    [Fact]
    public void DateTimeUtc_AssumesUtcWhenNoOffsetIsGiven()
    {
        Assert.True(Converters.DateTimeUtc.TryParse("2024-01-02", out var result));

        Assert.Equal(new DateTime(2024, 1, 2), result);
        Assert.Equal(DateTimeKind.Utc, result.Kind);
    }

    [Theory]
    [InlineData("")]
    [InlineData("02-01-2024")]
    [InlineData("2024-01-02 03:04:05")]
    [InlineData("2024-13-01")]
    public void DateTimeUtc_RejectsInvalidInput(string input) =>
        ConverterAssert.Rejects(Converters.DateTimeUtc, input);

    [Fact]
    public void DateTimeUtc_FormatsWithFirstFormat()
    {
        var value = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        Assert.Equal("2024-01-02T03:04:05Z", Converters.DateTimeUtc.Format(value));
        ConverterAssert.RoundTrips(Converters.DateTimeUtc, value);
    }

    [Fact]
    public void DateTimeLocal_AssumesLocalWhenNoOffsetIsGiven()
    {
        Assert.True(Converters.DateTimeLocal.TryParse("2024-01-02", out var result));

        Assert.Equal(new DateTime(2024, 1, 2), result);
        Assert.Equal(DateTimeKind.Local, result.Kind);
    }

    [Fact]
    public void DateTimeOffset_KeepsGivenOffset()
    {
        ConverterAssert.Parses(
            Converters.DateTimeOffset,
            "2024-01-02T03:04:05+02:00",
            new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.FromHours(2)));
    }

    [Fact]
    public void DateTimeOffset_AssumesUtcWhenNoOffsetIsGiven()
    {
        Assert.True(Converters.DateTimeOffset.TryParse("2024-01-02", out var result));

        Assert.Equal(new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero), result);
        Assert.Equal(TimeSpan.Zero, result.Offset);
    }

    [Fact]
    public void DateTimeOffset_FormatsWithFirstFormat()
    {
        var value = new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.FromHours(2));

        Assert.Equal("2024-01-02T03:04:05+02:00", Converters.DateTimeOffset.Format(value));
        ConverterAssert.RoundTrips(Converters.DateTimeOffset, value);
    }

    [Fact]
    public void DateTimeConverterBuilder_UsesGivenFormatsAndStyles()
    {
        var converter = new DateTimeConverterBuilder()
            .AddFormat("dd/MM/yyyy")
            .WithStyles(DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal)
            .Build();

        Assert.True(converter.TryParse("02/01/2024", out var result));
        Assert.Equal(new DateTime(2024, 1, 2), result);
        Assert.Equal(DateTimeKind.Utc, result.Kind);
        ConverterAssert.Rejects(converter, "2024-01-02");
    }

    [Fact]
    public void DateTimeOffsetConverterBuilder_UsesGivenFormats()
    {
        var converter = new DateTimeOffsetConverterBuilder()
            .AddFormat("yyyyMMdd")
            .WithStyles(DateTimeStyles.AssumeUniversal)
            .Build();

        ConverterAssert.Parses(converter, "20240102", new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero));
        Assert.Equal("20240102", converter.Format(new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero)));
    }
}
