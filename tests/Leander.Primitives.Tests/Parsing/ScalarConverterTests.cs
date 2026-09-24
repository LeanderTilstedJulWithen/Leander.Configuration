using System.Globalization;
using Leander.Primitives.Parsing;

namespace Leander.Primitives.Tests.Parsing;

public class ScalarConverterTests
{
    [Theory]
    [InlineData("")]
    [InlineData("  padded  ")]
    [InlineData("anything")]
    public void String_ReturnsInputUnchanged(string input)
    {
        ConverterAssert.Parses(Converters.String, input, input);
        Assert.Equal(input, Converters.String.Format(input));
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("False", false)]
    [InlineData("TRUE", true)]
    public void Boolean_ParsesValidInput(string input, bool expected) =>
        ConverterAssert.Parses(Converters.Boolean, input, expected);

    [Theory]
    [InlineData("")]
    [InlineData("yes")]
    [InlineData("1")]
    public void Boolean_RejectsInvalidInput(string input) =>
        ConverterAssert.Rejects(Converters.Boolean, input);

    [Fact]
    public void Boolean_RoundTrips()
    {
        ConverterAssert.RoundTrips(Converters.Boolean, true);
        ConverterAssert.RoundTrips(Converters.Boolean, false);
    }

    [Fact]
    public void Byte_ParsesRangeAndRejectsOverflow()
    {
        ConverterAssert.Parses(Converters.Byte, "0", (byte)0);
        ConverterAssert.Parses(Converters.Byte, "255", (byte)255);
        ConverterAssert.Rejects(Converters.Byte, "256");
        ConverterAssert.Rejects(Converters.Byte, "-1");
    }

    [Fact]
    public void SByte_ParsesRangeAndRejectsOverflow()
    {
        ConverterAssert.Parses(Converters.SByte, "-128", sbyte.MinValue);
        ConverterAssert.Parses(Converters.SByte, "127", sbyte.MaxValue);
        ConverterAssert.Rejects(Converters.SByte, "128");
    }

    [Fact]
    public void Int16_ParsesRangeAndRejectsOverflow()
    {
        ConverterAssert.Parses(Converters.Int16, "-32768", short.MinValue);
        ConverterAssert.Rejects(Converters.Int16, "32768");
    }

    [Fact]
    public void UInt16_ParsesRangeAndRejectsOverflow()
    {
        ConverterAssert.Parses(Converters.UInt16, "65535", ushort.MaxValue);
        ConverterAssert.Rejects(Converters.UInt16, "-1");
    }

    [Theory]
    [InlineData("0", 0)]
    [InlineData("42", 42)]
    [InlineData("-42", -42)]
    [InlineData(" 7 ", 7)]
    [InlineData("2147483647", int.MaxValue)]
    public void Int32_ParsesValidInput(string input, int expected) =>
        ConverterAssert.Parses(Converters.Int32, input, expected);

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("1.5")]
    [InlineData("1,000")]
    [InlineData("0x10")]
    [InlineData("2147483648")]
    public void Int32_RejectsInvalidInput(string input) =>
        ConverterAssert.Rejects(Converters.Int32, input);

    [Fact]
    public void Int32_FormatsWithInvariantCulture() =>
        Assert.Equal("-1234567", Converters.Int32.Format(-1234567));

    [Fact]
    public void UInt32_ParsesRangeAndRejectsNegative()
    {
        ConverterAssert.Parses(Converters.UInt32, "4294967295", uint.MaxValue);
        ConverterAssert.Rejects(Converters.UInt32, "-1");
    }

    [Fact]
    public void Int64_ParsesRange()
    {
        ConverterAssert.Parses(Converters.Int64, "-9223372036854775808", long.MinValue);
        ConverterAssert.RoundTrips(Converters.Int64, long.MaxValue);
    }

    [Fact]
    public void UInt64_ParsesRange()
    {
        ConverterAssert.Parses(Converters.UInt64, "18446744073709551615", ulong.MaxValue);
        ConverterAssert.Rejects(Converters.UInt64, "-1");
    }

    [Theory]
    [InlineData("0x1F", 31)]
    [InlineData("0X1f", 31)]
    [InlineData("1F", 31)]
    [InlineData("FFFFFFFF", -1)]
    public void Int32Hex_ParsesWithOrWithoutPrefix(string input, int expected) =>
        ConverterAssert.Parses(Converters.Int32Hex, input, expected);

    [Theory]
    [InlineData("")]
    [InlineData("0x")]
    [InlineData("0xG1")]
    [InlineData("-1")]
    public void Int32Hex_RejectsInvalidInput(string input) =>
        ConverterAssert.Rejects(Converters.Int32Hex, input);

    [Fact]
    public void Int32Hex_FormatsWithPrefixAndUppercaseDigits()
    {
        Assert.Equal("0xFF", Converters.Int32Hex.Format(255));
        ConverterAssert.RoundTrips(Converters.Int32Hex, -1);
    }

    [Fact]
    public void UInt32Hex_ParsesAndFormats()
    {
        ConverterAssert.Parses(Converters.UInt32Hex, "0xFFFFFFFF", uint.MaxValue);
        Assert.Equal("0xFFFFFFFF", Converters.UInt32Hex.Format(uint.MaxValue));
        ConverterAssert.Rejects(Converters.UInt32Hex, "0x100000000");
    }

    [Theory]
    [InlineData("1.5", 1.5)]
    [InlineData("-0.25", -0.25)]
    [InlineData("1e3", 1000.0)]
    public void Double_ParsesValidInput(string input, double expected) =>
        ConverterAssert.Parses(Converters.Double, input, expected);

    [Theory]
    [InlineData("")]
    [InlineData("1,5")]
    [InlineData("abc")]
    public void Double_RejectsInvalidInput(string input) =>
        ConverterAssert.Rejects(Converters.Double, input);

    [Fact]
    public void Double_IgnoresCurrentCulture()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("da-DK");

            ConverterAssert.Parses(Converters.Double, "1.5", 1.5);
            Assert.Equal("1.5", Converters.Double.Format(1.5));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Double_RoundTrips() => ConverterAssert.RoundTrips(Converters.Double, 0.1);

    [Fact]
    public void Single_ParsesAndRoundTrips()
    {
        ConverterAssert.Parses(Converters.Single, "1.5", 1.5f);
        ConverterAssert.RoundTrips(Converters.Single, 0.1f);
    }

    [Fact]
    public void Decimal_ParsesAndRoundTrips()
    {
        ConverterAssert.Parses(Converters.Decimal, "123.456", 123.456m);
        ConverterAssert.Rejects(Converters.Decimal, "1,5");
        ConverterAssert.RoundTrips(Converters.Decimal, 0.1m);
    }

    [Fact]
    public void Guid_ParsesCommonFormats()
    {
        var expected = new Guid("3f2504e0-4f89-11d3-9a0c-0305e82c3301");

        ConverterAssert.Parses(Converters.Guid, "3f2504e0-4f89-11d3-9a0c-0305e82c3301", expected);
        ConverterAssert.Parses(Converters.Guid, "3F2504E04F8911D39A0C0305E82C3301", expected);
        ConverterAssert.Parses(Converters.Guid, "{3f2504e0-4f89-11d3-9a0c-0305e82c3301}", expected);
        ConverterAssert.Rejects(Converters.Guid, "not-a-guid");
    }

    [Fact]
    public void Guid_FormatsAsLowercaseHyphenated() =>
        Assert.Equal(
            "3f2504e0-4f89-11d3-9a0c-0305e82c3301",
            Converters.Guid.Format(new Guid("3F2504E04F8911D39A0C0305E82C3301")));

    [Fact]
    public void Uri_ParsesAbsoluteUris()
    {
        ConverterAssert.Parses(Converters.Uri, "https://example.com/path?q=1", new Uri("https://example.com/path?q=1"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("/relative/path")]
    [InlineData("example.com")]
    public void Uri_RejectsNonAbsoluteUris(string input) =>
        ConverterAssert.Rejects(Converters.Uri, input);

    [Fact]
    public void Uri_FormatsAsAbsoluteUri() =>
        Assert.Equal("https://example.com/", Converters.Uri.Format(new Uri("https://example.com")));

    [Theory]
    [InlineData("00:00:30", 0, 0, 0, 30)]
    [InlineData("01:02:03", 0, 1, 2, 3)]
    [InlineData("2.01:02:03", 2, 1, 2, 3)]
    [InlineData("-00:05:00", 0, 0, -5, 0)]
    public void TimeSpan_ParsesConstantFormat(string input, int days, int hours, int minutes, int seconds) =>
        ConverterAssert.Parses(Converters.TimeSpan, input, new TimeSpan(days, hours, minutes, seconds));

    [Theory]
    [InlineData("")]
    [InlineData("30s")]
    [InlineData("abc")]
    public void TimeSpan_RejectsInvalidInput(string input) =>
        ConverterAssert.Rejects(Converters.TimeSpan, input);

    [Fact]
    public void TimeSpan_FormatsWithConstantFormat()
    {
        Assert.Equal("2.01:02:03", Converters.TimeSpan.Format(new TimeSpan(2, 1, 2, 3)));
        ConverterAssert.RoundTrips(Converters.TimeSpan, TimeSpan.FromMilliseconds(1500));
    }

    [Fact]
    public void TimeSpanConverterBuilder_ParsesAnyFormatButFormatsWithTheFirst()
    {
        var converter = new TimeSpanConverterBuilder()
            .AddFormat(@"hh\:mm")
            .AddFormat(@"s\s")
            .Build();

        ConverterAssert.Parses(converter, "01:30", new TimeSpan(1, 30, 0));
        ConverterAssert.Parses(converter, "45s", TimeSpan.FromSeconds(45));
        Assert.Equal("00:00", converter.Format(TimeSpan.FromSeconds(45)));
    }
}
