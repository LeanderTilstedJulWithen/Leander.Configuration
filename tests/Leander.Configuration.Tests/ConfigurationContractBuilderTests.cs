using Leander.Primitives;
using Leander.Primitives.Parsing;
using Leander.Primitives.Validation;

namespace Leander.Configuration.Tests;

public class ConfigurationContractBuilderTests
{
    private static IValueSource Source(params (string Key, string? Value)[] values) =>
        ValueSource.FromPairs(values.Select(pair => KeyValuePair.Create(pair.Key, pair.Value)));

    private static string BuildFailure(ConfigurationContractBuilder builder) =>
        Assert.Throws<InvalidOperationException>(builder.Build).Message;

    // Definitions

    [Fact]
    public void Build_Empty_HasNoDefinitions() =>
        Assert.Empty(new ConfigurationContractBuilder().Build().Definitions);

    [Fact]
    public void Build_KeepsDefinitionsInRegistrationOrder()
    {
        var host = ConfigurationDefinition.Define<string>("Host");
        var port = ConfigurationDefinition.Define<int>("Port");
        var timeout = ConfigurationDefinition.Define<TimeSpan>("Timeout");

        var contract = new ConfigurationContractBuilder()
            .RegisterDefaultPrimitives()
            .Register(port)
            .Register(host)
            .Register(timeout)
            .Build();

        Assert.Equal([port, host, timeout], contract.Definitions);
    }

    [Fact]
    public void Contains_IsByDefinitionInstance()
    {
        var port = ConfigurationDefinition.Define<int>("Port");
        var samePort = ConfigurationDefinition.Define<int>("Port");

        var contract = new ConfigurationContractBuilder().RegisterDefaultPrimitives().Register(port).Build();

        Assert.True(contract.Contains(port));
        Assert.False(contract.Contains(samePort));
    }

    [Fact]
    public void Build_DoesNotSeeLaterRegistrations()
    {
        var builder = new ConfigurationContractBuilder().RegisterDefaultPrimitives().Register(ConfigurationDefinition.Define<int>("Port"));
        var contract = builder.Build();

        builder.Register(ConfigurationDefinition.Define<string>("Host"));

        Assert.Single(contract.Definitions);
    }

    [Fact]
    public void Build_DuplicateKey_Fails()
    {
        var builder = new ConfigurationContractBuilder()
            .RegisterDefaultPrimitives()
            .Register(ConfigurationDefinition.Define<int>("Port"))
            .Register(ConfigurationDefinition.Define<string>("Port"));

        Assert.Contains("Port: defined more than once.", BuildFailure(builder));
    }

    [Fact]
    public void Build_DuplicateKey_IgnoresCase()
    {
        var builder = new ConfigurationContractBuilder()
            .RegisterDefaultPrimitives()
            .Register(ConfigurationDefinition.Define<int>("Server:Port"))
            .Register(ConfigurationDefinition.Define<int>("server:PORT"));

        Assert.Contains("defined more than once", BuildFailure(builder));
    }

    [Fact]
    public void Build_SameDefinitionTwice_IsDuplicate()
    {
        var port = ConfigurationDefinition.Define<int>("Port");
        var builder = new ConfigurationContractBuilder().RegisterDefaultPrimitives().Register(port).Register(port);

        Assert.Contains("Port: defined more than once.", BuildFailure(builder));
    }

    // Resolving primitives by definition

    [Fact]
    public void Build_TypeWithoutConverter_Fails()
    {
        var builder = new ConfigurationContractBuilder().Register(ConfigurationDefinition.Define<Custom>("Value"));

        Assert.Contains("Value: no converter is available for Custom.", BuildFailure(builder));
    }

    [Fact]
    public void Build_WithoutDefaultPrimitives_BuiltInTypeHasNoConverter()
    {
        var builder = new ConfigurationContractBuilder().Register(ConfigurationDefinition.Define<int>("Port"));

        Assert.Contains("Port: no converter is available for Int32.", BuildFailure(builder));
    }

    [Fact]
    public void Build_DefinitionWithOwnConverter_NeedsNoRegistration()
    {
        var value = ConfigurationDefinition.Define("Value", PrimitiveDefinition.Define(new CustomConverter()));

        var contract = new ConfigurationContractBuilder().Register(value).Build();

        Assert.Equal(new Custom("abc"), contract.Read(Source(("Value", "abc"))).Get(value));
    }

    [Fact]
    public void Build_RegisteredDefaultPrimitive_AppliesToDefinitionsOfItsType()
    {
        var value = ConfigurationDefinition.Define<Custom>("Value");

        var contract = new ConfigurationContractBuilder()
            .Register(PrimitiveDefinition.Define(new CustomConverter()))
            .Register(value)
            .Build();

        Assert.Equal(new Custom("abc"), contract.Read(Source(("Value", "abc"))).Get(value));
    }

    [Fact]
    public void Build_DefaultPrimitiveRules_ApplyToDefinitionsOfItsType()
    {
        var port = ConfigurationDefinition.Define<int>("Port");
        var contract = new ConfigurationContractBuilder()
            .RegisterDefaultPrimitives()
            .Register(PrimitiveDefinition.Define(Converters.Int32).Validate(Validators.GreaterThan(0)))
            .Register(port)
            .Build();

        var success = contract.TryRead(Source(("Port", "0")), out _, out var diagnostics);

        Assert.False(success);
        Assert.Equal("must be greater than 0", Assert.Single(diagnostics).Message);
    }

    [Fact]
    public void Build_UnregisteredPrimitive_AppendsRulesToDefault()
    {
        var primitive = PrimitiveDefinition.Define<int>().Validate(Validators.LessThan(10));
        var port = ConfigurationDefinition.Define("Port", primitive);
        var contract = new ConfigurationContractBuilder()
            .Register(PrimitiveDefinition.Define(Converters.Int32).Validate(Validators.GreaterThan(0)))
            .Register(port)
            .Build();

        contract.TryRead(Source(("Port", "0")), out _, out var low);
        contract.TryRead(Source(("Port", "10")), out _, out var high);

        Assert.Equal("must be greater than 0", Assert.Single(low).Message);
        Assert.Equal("must be less than 10", Assert.Single(high).Message);
    }

    [Fact]
    public void Build_Fallback_ResolvesType()
    {
        var value = ConfigurationDefinition.Define<Custom>("Value");

        var contract = new ConfigurationContractBuilder()
            .RegisterFallback(new CustomFallback())
            .Register(value)
            .Build();

        Assert.Equal(new Custom("abc"), contract.Read(Source(("Value", "abc"))).Get(value));
    }

    [Fact]
    public void Build_DefaultPrimitives_ResolveEnumsThroughFallback()
    {
        var mode = ConfigurationDefinition.Define<Mode>("Mode");

        var contract = new ConfigurationContractBuilder().RegisterDefaultPrimitives().Register(mode).Build();

        Assert.Equal(Mode.Fast, contract.Read(Source(("Mode", "Fast"))).Get(mode));
    }

    // Derived primitives keep the name, but are resolved by instance and never collide with the registered one.
    [Fact]
    public void Build_DerivedPrimitive_DoesNotAffectRegisteredPrimitive()
    {
        var email = PrimitiveDefinition.Define<string>("Email").Validate(Validators.Create<string>("must contain '@'", v => v.Contains('@')));
        var adminEmail = email.Validate(Validators.Create<string>("must end with '.com'", v => v.EndsWith(".com")));
        var admin = ConfigurationDefinition.Define("Admin", adminEmail);
        var support = ConfigurationDefinition.Define<string>("Support", "Email");

        var contract = new ConfigurationContractBuilder()
            .RegisterDefaultPrimitives()
            .Register(email)
            .Register(admin)
            .Register(support)
            .Build();

        contract.TryRead(Source(("Admin", "admin@example.org"), ("Support", "support@example.org")), out _, out var diagnostics);

        var error = Assert.Single(diagnostics);
        Assert.Equal("Admin", error.Key);
        Assert.Equal("must end with '.com'", error.Message);
    }

    // Resolving primitives by name

    [Fact]
    public void Build_ByName_UsesRegisteredPrimitive()
    {
        var value = ConfigurationDefinition.Define<int>("Value", "Hex");

        var contract = new ConfigurationContractBuilder().RegisterDefaultPrimitives().Register(value).Build();

        Assert.Equal(255, contract.Read(Source(("Value", "FF"))).Get(value));
    }

    [Fact]
    public void Build_ByName_Unregistered_Fails()
    {
        var builder = new ConfigurationContractBuilder()
            .RegisterDefaultPrimitives()
            .Register(ConfigurationDefinition.Define<string>("Admin", "Email"));

        Assert.Contains("Admin: no primitive named 'Email' is registered for String.", BuildFailure(builder));
    }

    [Fact]
    public void Build_ByName_RegisteredForOtherType_Fails()
    {
        var builder = new ConfigurationContractBuilder()
            .RegisterDefaultPrimitives()
            .Register(ConfigurationDefinition.Define<long>("Value", "Hex"));

        Assert.Contains("Value: no primitive named 'Hex' is registered for Int64.", BuildFailure(builder));
    }

    [Fact]
    public void Build_ByName_ResolvesElementOfList()
    {
        var values = ConfigurationDefinition.Define<int>("Values", "Hex").Delimited();

        var contract = new ConfigurationContractBuilder().RegisterDefaultPrimitives().Register(values).Build();

        Assert.Equal([10, 255], contract.Read(Source(("Values", "A,FF"))).Get(values));
    }

    // Collections

    [Fact]
    public void Build_UnresolvableElementOfIndexedList_Fails()
    {
        var builder = new ConfigurationContractBuilder().Register(ConfigurationDefinition.Define<Custom>("Values").Indexed());

        Assert.Contains("Values: no converter is available for Custom.", BuildFailure(builder));
    }

    [Fact]
    public void Build_DelimitedOfIndexed_Fails()
    {
        var builder = new ConfigurationContractBuilder()
            .RegisterDefaultPrimitives()
            .Register(ConfigurationDefinition.Define<int>("Values").Indexed().Delimited());

        Assert.Contains("Values: Delimited() requires a single-value element definition.", BuildFailure(builder));
    }

    [Fact]
    public void Build_DelimitedOfDelimited_Fails()
    {
        var builder = new ConfigurationContractBuilder()
            .RegisterDefaultPrimitives()
            .Register(ConfigurationDefinition.Define<int>("Values").Delimited().Delimited(';'));

        Assert.Contains("Values: Delimited() requires a single-value element definition.", BuildFailure(builder));
    }

    [Fact]
    public void Build_IndexedOfDelimited_IsAllowed()
    {
        var rows = ConfigurationDefinition.Define<int>("Rows").Delimited().Indexed();

        var contract = new ConfigurationContractBuilder().RegisterDefaultPrimitives().Register(rows).Build();

        var values = contract.Read(Source(("Rows:0", "1,2"), ("Rows:1", "3"))).Get(rows);
        Assert.Equal([1, 2], values[0]);
        Assert.Equal([3], values[1]);
    }

    // Failures

    [Fact]
    public void Build_ReportsEveryFailureAtOnce()
    {
        var builder = new ConfigurationContractBuilder()
            .RegisterDefaultPrimitives()
            .Register(ConfigurationDefinition.Define<int>("Port"))
            .Register(ConfigurationDefinition.Define<int>("Port"))
            .Register(ConfigurationDefinition.Define<Custom>("Value"))
            .Register(ConfigurationDefinition.Define<string>("Admin", "Email"))
            .Register(ConfigurationDefinition.Define<int>("Values").Indexed().Delimited());

        var expected = string.Join(
            Environment.NewLine,
            "Configuration contract could not be built:",
            "Port: defined more than once.",
            "Value: no converter is available for Custom.",
            "Admin: no primitive named 'Email' is registered for String.",
            "Values: Delimited() requires a single-value element definition.");
        Assert.Equal(expected, BuildFailure(builder));
    }

    // Primitive failures are thrown by the registry before definitions are resolved, so they are not combined.
    [Fact]
    public void Build_UnresolvablePrimitive_FailsBeforeDefinitions()
    {
        var builder = new ConfigurationContractBuilder()
            .Register(PrimitiveDefinition.Define<Custom>("Named"))
            .Register(ConfigurationDefinition.Define<Custom>("Value"));

        var message = BuildFailure(builder);

        Assert.StartsWith("Primitives could not be resolved:", message);
        Assert.DoesNotContain("Value:", message);
    }

    [Fact]
    public void Build_ExposesPrimitiveRegistry()
    {
        var email = PrimitiveDefinition.Define<string>("Email");

        var contract = new ConfigurationContractBuilder().RegisterDefaultPrimitives().Register(email).Build();

        Assert.True(contract.Primitives.TryGet<string>("Email", out _));
    }

    private enum Mode
    {
        Slow,
        Fast,
    }

    private sealed record Custom(string Value);

    private sealed class CustomConverter : IConverter<Custom>
    {
        public bool TryParse(string input, out Custom result)
        {
            result = new Custom(input);
            return true;
        }

        public string Format(Custom value) => value.Value;
    }

    private sealed class CustomFallback : IPrimitiveFallback
    {
        public PrimitiveDefinition<T>? Define<T>() =>
            typeof(T) == typeof(Custom) ? PrimitiveDefinition.Define((IConverter<T>)(object)new CustomConverter()) : null;
    }
}
