using static Leander.Configuration.Tests.TestHelpers;

namespace Leander.Configuration.Tests;

public class PipelineTests
{
    [Fact]
    public void Validate_Failure_ReportsValidatorMessage()
    {
        var definition = ConfigurationDefinition.Define<int>("Timeout").Validate(Validators.GreaterThan(0));
        var reader = Reader(("Timeout", "0"));

        Assert.False(reader.TryGet(definition, out _));
        Assert.Equal("must be greater than 0", SingleError(reader).Message);
    }

    [Fact]
    public void Validate_AllValidatorsRun_EvenAfterFailure()
    {
        var definition = ConfigurationDefinition.Define<int>("Timeout")
            .Validate(Validators.GreaterThan(10))
            .Validate(Validators.LessThan(-10));
        var reader = Reader(("Timeout", "0"));

        reader.Get(definition);

        Assert.Equal(2, Errors(reader).Count());
    }

    [Fact]
    public void Normalize_RunsBeforeValidate_RegardlessOfCallOrder()
    {
        var definition = ConfigurationDefinition.Define<string>("Name")
            .Validate(Validators.Create<string>("must not have surrounding whitespace", v => v == v.Trim()))
            .Normalize(Normalizers.Trim);
        var reader = Reader(("Name", "  value  "));

        Assert.Equal("value", reader.Get(definition));
        Assert.False(reader.HasErrors);
    }

    [Fact]
    public void Normalize_RunsInDeclarationOrder()
    {
        var definition = ConfigurationDefinition.Define<string>("Name")
            .Normalize(Normalizers.Create<string>("append a", v => v + "a"))
            .Normalize(Normalizers.Create<string>("append b", v => v + "b"));
        var reader = Reader(("Name", "x"));

        Assert.Equal("xab", reader.Get(definition));
    }

    [Fact]
    public void Default_IsNormalizedAndValidated()
    {
        var definition = ConfigurationDefinition.Define<int>("Timeout")
            .Default(-1)
            .Validate(Validators.GreaterThan(0));
        var reader = Reader();

        Assert.False(reader.TryGet(definition, out _));
        Assert.Equal("must be greater than 0", SingleError(reader).Message);
    }

    [Fact]
    public void Validate_KeyedValidator_IsResolvedFromRegistry()
    {
        var validators = new ValidatorRegistryBuilder()
            .Register(Validators.Create<string>("must be an email address", v => v.Contains('@')), "Email")
            .Build();
        var definition = ConfigurationDefinition.Define<string>("Admin").Validate("Email");
        var reader = new ConfigurationReader(ValueSource.FromDictionary([new("Admin", "nobody")]), validators: validators);

        reader.Get(definition);

        Assert.Equal("must be an email address", SingleError(reader).Message);
    }

    [Fact]
    public void Validate_UnknownValidatorKey_ReportsError()
    {
        var definition = ConfigurationDefinition.Define<string>("Admin").Validate("Email");
        var reader = Reader(("Admin", "someone@example.com"));

        Assert.False(reader.TryGet(definition, out _));
        Assert.Equal("no validator is registered for String with key 'Email'", SingleError(reader).Message);
    }

    [Fact]
    public void Normalize_KeyedNormalizer_IsResolvedFromRegistry()
    {
        var normalizers = new NormalizerRegistryBuilder()
            .Register(Normalizers.Create<string>("upper case", v => v.ToUpperInvariant()), "Upper")
            .Build();
        var definition = ConfigurationDefinition.Define<string>("Name").Normalize("Upper");
        var reader = new ConfigurationReader(ValueSource.FromDictionary([new("Name", "abc")]), normalizers: normalizers);

        Assert.Equal("ABC", reader.Get(definition));
    }

    [Fact]
    public void Validate_ThrowingValidator_IsReportedAsError()
    {
        var definition = ConfigurationDefinition.Define<string>("Name")
            .Validate(Validators.Create<string>("explodes", _ => throw new InvalidOperationException("boom")));
        var reader = Reader(("Name", "x"));

        Assert.False(reader.TryGet(definition, out _));
        Assert.Equal("validator 'explodes' failed: boom", SingleError(reader).Message);
    }

    [Fact]
    public void Normalize_ThrowingNormalizer_SkipsValidation()
    {
        var definition = ConfigurationDefinition.Define<string>("Name")
            .Normalize(Normalizers.Create<string>("explodes", _ => throw new InvalidOperationException("boom")))
            .Validate(Validators.Create<string>("never valid", _ => false));
        var reader = Reader(("Name", "x"));

        Assert.False(reader.TryGet(definition, out _));
        Assert.Equal("normalizer 'explodes' failed: boom", SingleError(reader).Message);
    }
}
