using Leander.Primitives.Parsing;
using Leander.Primitives.Validation;

namespace Leander.Primitives.Tests.Validation;

public class ValidatorsTests
{
    [Fact]
    public void Create_ReturnsNoFailuresWhenValidAndTheDescriptionWhenInvalid()
    {
        var validator = Validators.Create<int>("must be even", value => value % 2 == 0);

        Assert.Equal("must be even", validator.Description.FormatWith(Converters.Int32));
        Assert.Empty(validator.Validate(2));
        Assert.Equal(["must be even"], Failures(validator, 3, Converters.Int32));
        Assert.True(validator.IsValid(2));
        Assert.False(validator.IsValid(3));
    }

    [Fact]
    public void NotEmpty_RejectsNullAndEmptyStrings()
    {
        Assert.Equal(["must not be empty"], Failures(Validators.NotEmpty, "", Converters.String));
        Assert.Equal(["must not be empty"], Failures(Validators.NotEmpty, null!, Converters.String));
        Assert.Empty(Validators.NotEmpty.Validate(" "));
        Assert.Empty(Validators.NotEmpty.Validate("x"));
    }

    [Theory]
    [InlineData(4, false)]
    [InlineData(5, false)]
    [InlineData(6, true)]
    public void GreaterThan_IsExclusive(int value, bool valid) =>
        AssertValidity(Validators.GreaterThan(5), value, valid, "must be greater than 5");

    [Theory]
    [InlineData(4, false)]
    [InlineData(5, true)]
    [InlineData(6, true)]
    public void GreaterThanOrEqual_IsInclusive(int value, bool valid) =>
        AssertValidity(Validators.GreaterThanOrEqual(5), value, valid, "must be greater than or equal to 5");

    [Theory]
    [InlineData(4, true)]
    [InlineData(5, false)]
    [InlineData(6, false)]
    public void LessThan_IsExclusive(int value, bool valid) =>
        AssertValidity(Validators.LessThan(5), value, valid, "must be less than 5");

    [Theory]
    [InlineData(4, true)]
    [InlineData(5, true)]
    [InlineData(6, false)]
    public void LessThanOrEqual_IsInclusive(int value, bool valid) =>
        AssertValidity(Validators.LessThanOrEqual(5), value, valid, "must be less than or equal to 5");

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(5, true)]
    [InlineData(10, true)]
    [InlineData(11, false)]
    public void InRange_IsInclusiveOnBothEnds(int value, bool valid) =>
        AssertValidity(Validators.InRange(1, 10), value, valid, "must be between 1 and 10");

    [Fact]
    public void Bounds_AreFormattedWithTheGivenFormatter()
    {
        var validator = Validators.LessThanOrEqual(255);

        Assert.Equal("must be less than or equal to 0xFF", validator.Description.FormatWith(Converters.Int32Hex));
        Assert.Equal(["must be less than or equal to 0xFF"], Failures(validator, 256, Converters.Int32Hex));
    }

    [Fact]
    public void Text_ToString_FormatsInvariantly()
    {
        Assert.Equal("must be greater than 1.5", Validators.GreaterThan(1.5).Description.ToString());
    }

    [Fact]
    public void Comparisons_WorkForAnyComparableType()
    {
        var validator = Validators.GreaterThan(TimeSpan.Zero);

        Assert.True(validator.IsValid(TimeSpan.FromSeconds(1)));
        Assert.Single(validator.Validate(TimeSpan.Zero));
    }

    [Fact]
    public void CollectionsNotEmpty_RejectsEmptyLists()
    {
        var validator = Validators.Collections.NotEmpty<string>();

        Assert.Single(validator.Validate([]));
        Assert.Empty(validator.Validate(["a"]));
    }

    [Fact]
    public void Create_WithFormattableDescription()
    {
        var validator = Validators.Create(
            FormattableText.Create<int>(formatter => $"must be at most {formatter.Format(255)}"),
            value => value <= 255);

        Assert.Equal(["must be at most 0xFF"], Failures(validator, 256, Converters.Int32Hex));
    }

    private static IEnumerable<string> Failures<T>(IValidator<T> validator, T value, IFormatter<T> formatter) =>
        validator.Validate(value).Select(failure => failure.FormatWith(formatter));

    private static void AssertValidity(IValidator<int> validator, int value, bool valid, string description)
    {
        Assert.Equal(description, validator.Description.FormatWith(Converters.Int32));
        Assert.Equal(valid, validator.IsValid(value));
        Assert.Equal(valid ? [] : [description], Failures(validator, value, Converters.Int32));
    }
}
