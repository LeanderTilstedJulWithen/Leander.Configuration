using Leander.Primitives.Validation;

namespace Leander.Primitives.Tests.Validation;

public class ValidatorsTests
{
    [Fact]
    public void Create_ReturnsNullWhenValidAndDescriptionWhenInvalid()
    {
        var validator = Validators.Create<int>("must be even", value => value % 2 == 0);

        Assert.Equal("must be even", validator.Description);
        Assert.Null(validator.Validate(2));
        Assert.Equal("must be even", validator.Validate(3));
    }

    [Fact]
    public void NotEmpty_RejectsNullAndEmptyStrings()
    {
        Assert.Equal("must not be empty", Validators.NotEmpty.Validate(""));
        Assert.Equal("must not be empty", Validators.NotEmpty.Validate(null!));
        Assert.Null(Validators.NotEmpty.Validate(" "));
        Assert.Null(Validators.NotEmpty.Validate("x"));
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
    public void Comparisons_WorkForAnyComparableType()
    {
        var validator = Validators.GreaterThan(TimeSpan.Zero);

        Assert.Null(validator.Validate(TimeSpan.FromSeconds(1)));
        Assert.NotNull(validator.Validate(TimeSpan.Zero));
    }

    [Fact]
    public void CollectionsNotEmpty_AppliesToTypedCollections()
    {
        IValidator<IReadOnlyList<string>> validator = Validators.Collections.NotEmpty;

        Assert.Equal("must not be empty", validator.Validate([]));
        Assert.Null(validator.Validate(["a"]));
    }

    [Fact]
    public void CollectionsNotEmpty_DisposesEnumerator()
    {
        var disposed = false;

        IEnumerable<int> Values()
        {
            try
            {
                yield return 1;
                yield return 2;
            }
            finally
            {
                disposed = true;
            }
        }

        Assert.Null(Validators.Collections.NotEmpty.Validate(Values()));
        Assert.True(disposed);
    }

    private static void AssertValidity(IValidator<int> validator, int value, bool valid, string description)
    {
        Assert.Equal(description, validator.Description);
        Assert.Equal(valid ? null : description, validator.Validate(value));
    }
}
