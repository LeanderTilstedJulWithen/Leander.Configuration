using Leander.Primitives.Normalization;
using Leander.Primitives.Parsing;
using Leander.Primitives.Validation;

namespace Leander.Primitives.Tests;

public class PrimitiveRegistryTests
{
    public sealed class Custom;

    [Fact]
    public void RegisterDefaults_RegistersBuiltInDefaults()
    {
        var registry = new PrimitiveRegistryBuilder().RegisterDefaults().Build();

        Assert.Same(Converters.String, registry.Get<string>().Converter);
        Assert.Same(Converters.Boolean, registry.Get<bool>().Converter);
        Assert.Same(Converters.Byte, registry.Get<byte>().Converter);
        Assert.Same(Converters.SByte, registry.Get<sbyte>().Converter);
        Assert.Same(Converters.Int16, registry.Get<short>().Converter);
        Assert.Same(Converters.UInt16, registry.Get<ushort>().Converter);
        Assert.Same(Converters.Int32, registry.Get<int>().Converter);
        Assert.Same(Converters.UInt32, registry.Get<uint>().Converter);
        Assert.Same(Converters.Int64, registry.Get<long>().Converter);
        Assert.Same(Converters.UInt64, registry.Get<ulong>().Converter);
        Assert.Same(Converters.Single, registry.Get<float>().Converter);
        Assert.Same(Converters.Double, registry.Get<double>().Converter);
        Assert.Same(Converters.Decimal, registry.Get<decimal>().Converter);
        Assert.Same(Converters.Guid, registry.Get<Guid>().Converter);
        Assert.Same(Converters.Uri, registry.Get<Uri>().Converter);
        Assert.Same(Converters.TimeSpan, registry.Get<TimeSpan>().Converter);
        Assert.Same(Converters.DateTimeUtc, registry.Get<DateTime>().Converter);
        Assert.Same(Converters.DateTimeOffset, registry.Get<DateTimeOffset>().Converter);
    }

    [Fact]
    public void RegisterDefaults_RegistersNamedVariants()
    {
        var registry = new PrimitiveRegistryBuilder().RegisterDefaults().Build();

        Assert.Same(Converters.Int32Hex, registry.Get<int>("Hex").Converter);
        Assert.Same(Converters.UInt32Hex, registry.Get<uint>("Hex").Converter);
        Assert.Same(Converters.DateTimeLocal, registry.Get<DateTime>("Local").Converter);
        Assert.Equal("Hex", registry.Get<int>("Hex").Name);
    }

    [Fact]
    public void RegisterDefaults_CoversEnumsThroughFallback()
    {
        var registry = new PrimitiveRegistryBuilder().RegisterDefaults().Build();

        var primitive = registry.Get<DayOfWeek>();

        Assert.Null(primitive.Name);
        Assert.Same(Converters.Enum<DayOfWeek>(), primitive.Converter);
    }

    [Fact]
    public void Get_DefaultPrimitive_HasNullNameAndValueType()
    {
        var registry = new PrimitiveRegistryBuilder().RegisterDefaults().Build();

        var primitive = registry.Get<int>();

        Assert.Null(primitive.Name);
        Assert.Equal(typeof(int), primitive.ValueType);
    }

    [Fact]
    public void Get_MissingDefault_Throws()
    {
        var registry = new PrimitiveRegistryBuilder().Build();

        var exception = Assert.Throws<KeyNotFoundException>(() => registry.Get<int>());

        Assert.Contains("No default primitive", exception.Message);
    }

    [Fact]
    public void Get_MissingName_ThrowsWithName()
    {
        var registry = new PrimitiveRegistryBuilder().RegisterDefaults().Build();

        var exception = Assert.Throws<KeyNotFoundException>(() => registry.Get<int>("Octal"));

        Assert.Contains("'Octal'", exception.Message);
    }

    [Fact]
    public void TryGet_MissingPrimitive_ReturnsFalse()
    {
        var registry = new PrimitiveRegistryBuilder().RegisterDefaults().Build();

        Assert.False(registry.TryGet<Custom>(null, out var primitive));
        Assert.Null(primitive);
    }

    [Fact]
    public void TryGet_NamesAreNotResolvedThroughFallbacks()
    {
        var registry = new PrimitiveRegistryBuilder().RegisterDefaults().Build();

        Assert.False(registry.TryGet<DayOfWeek>("Short", out _));
    }

    [Fact]
    public void Register_SameTypeAndNameTwice_LastRegistrationWins()
    {
        var registry = new PrimitiveRegistryBuilder()
            .Register(PrimitiveDefinition.Define(Converters.Int32))
            .Register(PrimitiveDefinition.Define(Converters.Int32Hex))
            .Build();

        Assert.Same(Converters.Int32Hex, registry.Get<int>().Converter);
    }

    [Fact]
    public void Register_ReplacingABuiltInDefault()
    {
        var registry = new PrimitiveRegistryBuilder()
            .RegisterDefaults()
            .Register(PrimitiveDefinition.Define(Converters.DateTimeLocal))
            .Build();

        Assert.Same(Converters.DateTimeLocal, registry.Get<DateTime>().Converter);
    }

    [Fact]
    public void Default_KeepsItsOwnSettings()
    {
        var normalizer = Normalizers.Trim;
        var validator = Validators.NotEmpty;

        var registry = new PrimitiveRegistryBuilder()
            .Register(PrimitiveDefinition.Define(Converters.String).Describe("Text.").Normalize(normalizer).Validate(validator))
            .Build();

        var primitive = registry.Get<string>();
        Assert.Equal("Text.", primitive.Description);
        Assert.Equal([normalizer], primitive.Normalizers);
        Assert.Equal([validator], primitive.Validators);
    }

    [Fact]
    public void Named_WithoutConverter_UsesDefaultConverter()
    {
        var registry = new PrimitiveRegistryBuilder()
            .RegisterDefaults()
            .Register(PrimitiveDefinition.Define<int>("Port").Validate(Validators.InRange(1, 65535)))
            .Build();

        Assert.Same(Converters.Int32, registry.Get<int>("Port").Converter);
    }

    [Fact]
    public void Named_WithConverter_ReplacesDefaultConverter()
    {
        var converter = Converters.List(Converters.Int32);

        var registry = new PrimitiveRegistryBuilder()
            .Register(PrimitiveDefinition.Define<IReadOnlyList<int>>(Converters.List(Converters.Int32, ';')))
            .Register(PrimitiveDefinition.Define("Comma", converter))
            .Build();

        Assert.Same(converter, registry.Get<IReadOnlyList<int>>("Comma").Converter);
    }

    [Fact]
    public void Named_AppendsNormalizersAndValidatorsToDefaults()
    {
        var defaultNormalizer = Normalizers.Trim;
        var namedNormalizer = Normalizers.FullPath;
        var defaultValidator = Validators.NotEmpty;
        var namedValidator = Validators.Create<string>("must end with .txt", value => value.EndsWith(".txt"));

        var registry = new PrimitiveRegistryBuilder()
            .Register(PrimitiveDefinition.Define(Converters.String).Normalize(defaultNormalizer).Validate(defaultValidator))
            .Register(PrimitiveDefinition.Define<string>("TextFile").Normalize(namedNormalizer).Validate(namedValidator))
            .Build();

        var primitive = registry.Get<string>("TextFile");
        Assert.Equal([defaultNormalizer, namedNormalizer], primitive.Normalizers);
        Assert.Equal([defaultValidator, namedValidator], primitive.Validators);
    }

    [Fact]
    public void Named_DoesNotInheritDefaultDescription()
    {
        var registry = new PrimitiveRegistryBuilder()
            .Register(PrimitiveDefinition.Define(Converters.Int32).Describe("An integer."))
            .Register(PrimitiveDefinition.Define<int>("Port"))
            .Build();

        Assert.Null(registry.Get<int>("Port").Description);
    }

    [Fact]
    public void Named_RegisteredBeforeDefault_StillUsesDefault()
    {
        var validator = Validators.GreaterThan(0);

        var registry = new PrimitiveRegistryBuilder()
            .Register(PrimitiveDefinition.Define<int>("Positive"))
            .Register(PrimitiveDefinition.Define(Converters.Int32).Validate(validator))
            .Build();

        var primitive = registry.Get<int>("Positive");
        Assert.Same(Converters.Int32, primitive.Converter);
        Assert.Equal([validator], primitive.Validators);
    }

    [Fact]
    public void Named_WithoutConverter_ResolvesAgainstFallbackDefault()
    {
        var registry = new PrimitiveRegistryBuilder()
            .RegisterDefaults()
            .Register(PrimitiveDefinition.Define<DayOfWeek>("Weekday")
                .Validate(Validators.Create<DayOfWeek>("must be a weekday", day => day is not (DayOfWeek.Saturday or DayOfWeek.Sunday))))
            .Build();

        var primitive = registry.Get<DayOfWeek>("Weekday");
        Assert.Same(Converters.Enum<DayOfWeek>(), primitive.Converter);
        Assert.Single(primitive.Validators);
    }

    [Fact]
    public void Build_DefaultWithoutConverter_Throws()
    {
        var builder = new PrimitiveRegistryBuilder().Register(PrimitiveDefinition.Define<Custom>());

        var exception = Assert.Throws<InvalidOperationException>(builder.Build);

        Assert.Contains("Default primitive for Custom: has no converter.", exception.Message);
    }

    [Fact]
    public void Build_NamedWithoutConverterOrDefault_Throws()
    {
        var builder = new PrimitiveRegistryBuilder().Register(PrimitiveDefinition.Define<Custom>("Special"));

        var exception = Assert.Throws<InvalidOperationException>(builder.Build);

        Assert.Contains("Primitive 'Special' for Custom: has no converter, and no default converter is registered for Custom.", exception.Message);
    }

    [Fact]
    public void Build_ReportsEveryFailure()
    {
        var builder = new PrimitiveRegistryBuilder()
            .RegisterDefaults()
            .Register(PrimitiveDefinition.Define<Custom>())
            .Register(PrimitiveDefinition.Define<Custom>("Special"))
            .Register(PrimitiveDefinition.Define<Version>("Short"));

        var exception = Assert.Throws<InvalidOperationException>(builder.Build);

        Assert.Contains("Default primitive for Custom:", exception.Message);
        Assert.Contains("'Special' for Custom:", exception.Message);
        Assert.Contains("'Short' for Version:", exception.Message);
    }

    [Fact]
    public void Fallback_FirstMatchingFallbackWins()
    {
        var first = new ConverterFallback<Custom>(new CustomConverter());
        var second = new ConverterFallback<Custom>(new CustomConverter());

        var registry = new PrimitiveRegistryBuilder()
            .RegisterFallback(first)
            .RegisterFallback(second)
            .Build();

        Assert.Same(first.Converter, registry.Get<Custom>().Converter);
    }

    [Fact]
    public void Fallback_NotConsultedForRegisteredDefault()
    {
        var fallback = new ConverterFallback<int>(Converters.Int32Hex);

        var registry = new PrimitiveRegistryBuilder()
            .Register(PrimitiveDefinition.Define(Converters.Int32))
            .RegisterFallback(fallback)
            .Build();

        Assert.Same(Converters.Int32, registry.Get<int>().Converter);
        Assert.Equal(0, fallback.Calls);
    }

    [Fact]
    public void Fallback_ResultIsCached()
    {
        var fallback = new ConverterFallback<Custom>(new CustomConverter());
        var registry = new PrimitiveRegistryBuilder().RegisterFallback(fallback).Build();

        var first = registry.Get<Custom>();
        var second = registry.Get<Custom>();

        Assert.Same(first, second);
        Assert.Equal(1, fallback.Calls);
    }

    [Fact]
    public void Fallback_MissIsCached()
    {
        var fallback = new ConverterFallback<Custom>(new CustomConverter());
        var registry = new PrimitiveRegistryBuilder().RegisterFallback(fallback).Build();

        Assert.False(registry.TryGet<int>(null, out _));
        Assert.False(registry.TryGet<int>(null, out _));

        Assert.Equal(1, fallback.Calls);
    }

    private sealed class ConverterFallback<TValue>(IConverter<TValue> converter) : IPrimitiveFallback
    {
        public IConverter<TValue> Converter => converter;

        public int Calls { get; private set; }

        public PrimitiveDefinition<T>? Define<T>()
        {
            Calls++;
            return typeof(T) == typeof(TValue) ? PrimitiveDefinition.Define((IConverter<T>)converter) : null;
        }
    }

    private sealed class CustomConverter : IConverter<Custom>
    {
        public bool TryParse(string input, out Custom result)
        {
            result = new Custom();
            return true;
        }

        public string Format(Custom value) => nameof(Custom);
    }
}
