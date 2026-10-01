using Leander.Primitives.Normalization;
using Leander.Primitives.Parsing;
using Leander.Primitives.Validation;

namespace Leander.Primitives.Tests;

public class PrimitiveTests
{
    private enum Color
    {
        Red,
        Green,
    }

    // A custom rule whose failure holds the checked value as well as the bound.
    private sealed class AtMost(int maximum) : IValidator<int>
    {
        private readonly int _maximum = maximum;

        public IFormattableText<int> Description =>
            FormattableText.Create<int>(formatter => $"must be at most {formatter.Format(_maximum)}");

        public IReadOnlyList<IFormattableText<int>> Validate(int value) =>
            value <= _maximum
                ? []
                : [FormattableText.Create<int>(formatter => $"must be at most {formatter.Format(_maximum)}, but was {formatter.Format(value)}")];
    }

    // A custom rule reporting each unmet requirement.
    private sealed class PasswordPolicy : IValidator<string>
    {
        public IFormattableText<string> Description { get; } = FormattableText.Create<string>("must meet the password policy");

        public IReadOnlyList<IFormattableText<string>> Validate(string value)
        {
            var failures = new List<IFormattableText<string>>();
            if (value.Length < 12)
            {
                failures.Add(FormattableText.Create<string>("must be at least 12 characters"));
            }

            if (!value.Any(char.IsDigit))
            {
                failures.Add(FormattableText.Create<string>("must contain a digit"));
            }

            return failures;
        }
    }

    private static readonly Primitive<int> Port = new("Port", Converters.Int32)
    {
        Validators = [Validators.InRange(1, 65535)],
        Description = "A TCP port.",
    };

    // Constructors

    [Fact]
    public void Constructor_WithConverter_HasNameConverterAndNoRules()
    {
        var primitive = new Primitive<int>("Count", Converters.Int32);

        Assert.Equal("Count", primitive.Name);
        Assert.Same(Converters.Int32, primitive.Converter);
        Assert.Equal(typeof(int), primitive.ValueType);
        Assert.Null(primitive.Base);
        Assert.Null(primitive.Description);
        Assert.Empty(primitive.Normalizers);
        Assert.Empty(primitive.Validators);
    }

    [Fact]
    public void Constructor_InitProperties_AreKept()
    {
        Assert.Equal("A TCP port.", Port.Description);
        Assert.Single(Port.Validators);
    }

    // TryParse and TryAccept

    [Fact]
    public void TryParse_ValidValue_ReturnsValue()
    {
        Assert.True(Port.TryParse("8080", out var port));
        Assert.Equal(8080, port);
    }

    [Fact]
    public void TryParse_InvalidText_NamesTheType()
    {
        Assert.False(Port.TryParse("abc", out var port, out var errors));
        Assert.Equal(default, port);
        Assert.Equal(["'abc' is not a valid Int32 (Port)"], errors);
    }

    [Fact]
    public void TryParse_NameIsTypeName_NamesOnlyTheType()
    {
        Primitive.Int32.TryParse("abc", out _, out var errors);

        Assert.Equal(["'abc' is not a valid Int32"], errors);
    }

    [Fact]
    public void TryParse_FailingValidator_ReturnsItsMessage()
    {
        Assert.False(Port.TryParse("99999", out _, out var errors));
        Assert.Equal(["must be between 1 and 65535"], errors);
    }

    [Fact]
    public void TryParse_NormalizesBeforeValidating()
    {
        var name = new Primitive<string>("Name", Converters.String)
        {
            Normalizers = [Normalizers.Trim],
            Validators = [Validators.NotEmpty],
        };

        Assert.True(name.TryParse("  abc  ", out var value));
        Assert.Equal("abc", value);
        Assert.False(name.TryParse("   ", out _, out var errors));
        Assert.Equal(["must not be empty"], errors);
    }

    [Fact]
    public void TryParse_ReportsEveryFailingValidator()
    {
        var value = new Primitive<int>("Value", Converters.Int32)
        {
            Validators = [Validators.GreaterThan(10), Validators.GreaterThan(20)],
        };

        value.TryParse("5", out _, out var errors);

        Assert.Equal(["must be greater than 10", "must be greater than 20"], errors);
    }

    [Fact]
    public void TryParse_ThrowingNormalizer_IsErrorAndSkipsValidation()
    {
        var value = new Primitive<string>("Value", Converters.String)
        {
            Normalizers = [Normalizers.Create<string>("boom", _ => throw new InvalidOperationException("failed hard"))],
            Validators = [Validators.NotEmpty],
        };

        Assert.False(value.TryParse("", out _, out var errors));
        Assert.Equal(["normalizer 'boom' failed: failed hard"], errors);
    }

    [Fact]
    public void TryParse_ThrowingValidator_IsError()
    {
        var value = new Primitive<string>("Value", Converters.String)
        {
            Validators = [Validators.Create<string>("boom", _ => throw new InvalidOperationException("failed hard"))],
        };

        Assert.False(value.TryParse("x", out _, out var errors));
        Assert.Equal(["validator 'boom' failed: failed hard"], errors);
    }

    [Fact]
    public void TryAccept_AppliesRulesToValue()
    {
        Assert.True(Port.TryAccept(8080, out var accepted));
        Assert.Equal(8080, accepted);
        Assert.False(Port.TryAccept(0, out _, out var errors));
        Assert.Equal(["must be between 1 and 65535"], errors);
    }

    // Deriving

    [Fact]
    public void Derived_TakesBaseConverterAndRefersToBase()
    {
        var adminPort = new Primitive<int>("AdminPort", Port);

        Assert.Same(Port, adminPort.Base);
        Assert.Same(Port.Converter, adminPort.Converter);
        Assert.Null(adminPort.Description);
    }

    [Fact]
    public void Derived_OwnRulesOnly_InValidators()
    {
        var greaterThan1024 = Validators.GreaterThan(1024);
        var adminPort = new Primitive<int>("AdminPort", Port) { Validators = [greaterThan1024] };

        Assert.Equal([greaterThan1024], adminPort.Validators);
    }

    // Bounds

    [Fact]
    public void Bounds_AreFormattedWithTheConverter()
    {
        var mask = new Primitive<int>("Mask", Converters.Int32Hex) { Validators = [Validators.LessThanOrEqual(255)] };

        mask.TryAccept(256, out _, out var errors);

        Assert.Equal(["must be less than or equal to 0xFF"], errors);
    }

    [Fact]
    public void CustomValidator_FailureHoldingTheValue_IsFormattedWithTheConverter()
    {
        var mask = new Primitive<int>("Mask", Converters.Int32Hex) { Validators = [new AtMost(255)] };

        mask.TryAccept(256, out _, out var errors);

        Assert.Equal(["must be at most 0xFF, but was 0x100"], errors);
    }

    [Fact]
    public void CustomValidator_SeveralFailures_AreAllReported()
    {
        var password = new Primitive<string>("Password", Converters.String) { Validators = [new PasswordPolicy()] };

        Assert.False(password.TryParse("secret", out _, out var errors));
        Assert.Equal(["must be at least 12 characters", "must contain a digit"], errors);
        Assert.False(password.TryParse("secret", out _));
        Assert.True(password.TryParse("correct horse 1", out _));
    }

    [Fact]
    public void ThrowingValidator_WithoutErrorList_Fails()
    {
        var value = new Primitive<int>("Value", Converters.Int32)
        {
            Validators = [Validators.Create<int>("boom", _ => throw new InvalidOperationException())],
        };

        Assert.False(value.TryAccept(1, out _));
    }

    [Fact]
    public void Bounds_Normalize()
    {
        var count = new Primitive<int>("Count", Converters.Int32)
        {
            Normalizers = [Normalizers.LowerBound(1), Normalizers.UpperBound(10)],
        };

        count.TryAccept(0, out var low);
        count.TryAccept(99, out var high);

        Assert.Equal(1, low);
        Assert.Equal(10, high);
    }

    [Fact]
    public void CreatedRule_KeepsItsText()
    {
        var value = new Primitive<int>("Value", Converters.Int32)
        {
            Validators = [Validators.Create<int>("must not be {x}", _ => false)],
        };

        value.TryAccept(1, out _, out var errors);

        Assert.Equal(["must not be {x}"], errors);
    }

    [Fact]
    public void Derived_RunsBaseRulesFirst()
    {
        var adminPort = new Primitive<int>("AdminPort", Port) { Validators = [Validators.GreaterThan(1024)] };

        adminPort.TryParse("99999", out _, out var high);
        adminPort.TryParse("0", out _, out var low);

        Assert.Equal(["must be between 1 and 65535"], high);
        Assert.Equal(["must be between 1 and 65535", "must be greater than 1024"], low);
    }

    [Fact]
    public void Derived_BaseNormalizersRunBeforeOwn()
    {
        var trimmed = new Primitive<string>("Trimmed", Converters.String) { Normalizers = [Normalizers.Trim] };
        var tagged = new Primitive<string>("Tagged", trimmed)
        {
            Normalizers = [Normalizers.Create<string>("tag", value => $"[{value}]")],
        };

        tagged.TryParse("  a  ", out var value);

        Assert.Equal("[a]", value);
    }

    [Fact]
    public void Derived_ParseErrorsUseItsOwnName()
    {
        var adminPort = new Primitive<int>("AdminPort", Port);

        adminPort.TryParse("abc", out _, out var errors);

        Assert.Equal(["'abc' is not a valid Int32 (AdminPort)"], errors);
    }

    // Ready-made primitives

    [Fact]
    public void ReadyMade_AreNamedAfterTheirType()
    {
        Assert.Equal("String", Primitive.String.Name);
        Assert.Equal("Int32", Primitive.Int32.Name);
        Assert.Equal("TimeSpan", Primitive.TimeSpan.Name);
        Assert.Equal("Uri", Primitive.Uri.Name);
        Assert.Equal("DateTimeOffset", Primitive.DateTimeOffset.Name);
    }

    [Fact]
    public void ReadyMade_Variants_HaveTheirOwnName()
    {
        Assert.Equal("Hex", Primitive.Int32Hex.Name);
        Assert.Equal("Hex", Primitive.UInt32Hex.Name);
        Assert.Equal("Local", Primitive.DateTimeLocal.Name);

        Primitive.Int32Hex.TryParse("xyz", out _, out var errors);
        Assert.Equal(["'xyz' is not a valid Int32 (Hex)"], errors);
    }

    [Fact]
    public void ReadyMade_AreTheSameInstanceEveryTime()
    {
        Assert.Same(Primitive.Int32, Primitive.Int32);
    }

    [Fact]
    public void ReadyMade_HaveNoRules()
    {
        Assert.Empty(Primitive.String.Normalizers);
        Assert.Empty(Primitive.String.Validators);
        Assert.True(Primitive.String.TryParse("  ", out var value));
        Assert.Equal("  ", value);
    }

    // Enum primitives

    [Fact]
    public void Enum_IsNamedAfterTheEnumType()
    {
        Assert.Equal("Color", Primitive.Enum<Color>().Name);
        Assert.Equal(typeof(Color), Primitive.Enum<Color>().ValueType);
    }

    [Fact]
    public void Enum_IsTheSameInstanceEveryTime()
    {
        Assert.Same(Primitive.Enum<Color>(), Primitive.Enum<Color>());
    }

    [Fact]
    public void Enum_ParsesNames()
    {
        Assert.True(Primitive.Enum<Color>().TryParse("Green", out var color));
        Assert.Equal(Color.Green, color);
        Assert.False(Primitive.Enum<Color>().TryParse("Blue", out _, out var errors));
        Assert.Equal(["'Blue' is not a valid Color"], errors);
    }
}
