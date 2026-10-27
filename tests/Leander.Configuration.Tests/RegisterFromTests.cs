using Leander.Primitives;

namespace Leander.Configuration.Tests;

public class RegisterFromTests
{
    [Fact]
    public void RegisterFrom_RegistersFieldsThenPropertiesInDeclarationOrder()
    {
        var contract = new ConfigurationContractBuilder().RegisterFrom(typeof(MarkedType)).Build();

        Assert.Equal(["B:Field", "A:Field", "Private:Field", "Property", "Private:Property"], contract.Definitions.Select(d => d.Key));
    }

    [Fact]
    public void RegisterFrom_IgnoresInstanceMembersMethodsAndOtherTypes()
    {
        var contract = new ConfigurationContractBuilder().RegisterFrom(typeof(MarkedTypeWithOtherMembers)).Build();

        Assert.Equal(["Static"], contract.Definitions.Select(d => d.Key));
    }

    [Fact]
    public void RegisterFrom_Struct_Registers()
    {
        var contract = new ConfigurationContractBuilder().RegisterFrom(typeof(MarkedStruct)).Build();

        Assert.Equal(["Struct"], contract.Definitions.Select(d => d.Key));
    }

    [Fact]
    public void RegisterFrom_DefinitionFoundTwice_CountsOnce()
    {
        var contract = new ConfigurationContractBuilder().RegisterFrom(typeof(SameDefinitionTwice)).Build();

        Assert.Equal([SameDefinitionTwice.Field], contract.Definitions);
    }

    [Fact]
    public void RegisterFrom_AlreadyRegistered_IsSkipped()
    {
        var contract = new ConfigurationContractBuilder()
            .Register(MarkedType.AField)
            .RegisterFrom(typeof(MarkedType))
            .RegisterFrom(typeof(MarkedType))
            .Build();

        Assert.Equal(["A:Field", "B:Field", "Private:Field", "Property", "Private:Property"], contract.Definitions.Select(d => d.Key));
    }

    [Fact]
    public void RegisterFrom_SameKeyAsAnotherDefinition_Fails()
    {
        var builder = new ConfigurationContractBuilder()
            .Register(ConfigurationDefinition.Define("A:Field", Primitive.Int32))
            .RegisterFrom(typeof(MarkedType));

        var message = Assert.Throws<InvalidOperationException>(builder.Build).Message;

        Assert.Contains("A:Field: defined more than once.", message);
    }

    [Fact]
    public void RegisterFrom_TypeWithoutAttribute_FailsAtBuild()
    {
        // Nothing throws while registering.
        var builder = new ConfigurationContractBuilder().RegisterFrom(typeof(Unmarked));

        var message = Assert.Throws<InvalidOperationException>(builder.Build).Message;

        Assert.Contains("Leander.Configuration.Tests.RegisterFromTests.Unmarked: is not marked with RegisterConfiguration.", message);
    }

    [Fact]
    public void RegisterFrom_OpenGenericType_FailsAtBuild()
    {
        var message = Assert.Throws<InvalidOperationException>(new ConfigurationContractBuilder().RegisterFrom(typeof(Generic<>)).Build).Message;

        Assert.Contains("RegisterConfiguration can't be used on an open generic type.", message);
    }

    [Fact]
    public void RegisterFrom_ClosedGenericType_Registers()
    {
        var contract = new ConfigurationContractBuilder().RegisterFrom(typeof(Generic<int>)).Build();

        Assert.Equal(["Generic:Int32"], contract.Definitions.Select(d => d.Key));
    }

    [Fact]
    public void RegisterFrom_NullAndThrowingMembers_AreReportedAtBuildWithTheValidOnesStillRegistered()
    {
        var builder = new ConfigurationContractBuilder()
            .Register(ConfigurationDefinition.Define("Valid", Primitive.Int32))
            .RegisterFrom(typeof(InvalidMembers));

        var message = Assert.Throws<InvalidOperationException>(builder.Build).Message;

        const string type = "Leander.Configuration.Tests.RegisterFromTests.InvalidMembers";
        Assert.Contains($"{type}.Null: is null.", message);
        Assert.Contains($"{type}.Throwing: could not be read: broken", message);
        Assert.Contains("Valid: defined more than once.", message);
    }

    [Fact]
    public void RegisterFrom_ThrowingStaticInitializer_FailsAtBuild()
    {
        var message = Assert.Throws<InvalidOperationException>(new ConfigurationContractBuilder().RegisterFrom(typeof(ThrowingInitializer)).Build).Message;

        Assert.Contains("ThrowingInitializer.Definition: could not be read: initializer failed", message);
    }

    [RegisterConfiguration]
    public static class MarkedType
    {
        public static readonly ConfigurationDefinition<int> BField = ConfigurationDefinition.Define("B:Field", Primitive.Int32);
        public static readonly ConfigurationDefinition<int> AField = ConfigurationDefinition.Define("A:Field", Primitive.Int32);

        public static ConfigurationDefinition<string> Property { get; } = ConfigurationDefinition.Define("Property", Primitive.String);

        private static ConfigurationDefinition<string> PrivateProperty { get; } = ConfigurationDefinition.Define("Private:Property", Primitive.String);

        private static readonly ConfigurationDefinition<int> PrivateField = ConfigurationDefinition.Define("Private:Field", Primitive.Int32);
    }

    [RegisterConfiguration]
    public sealed class MarkedTypeWithOtherMembers
    {
        public static readonly ConfigurationDefinition<int> Static = ConfigurationDefinition.Define("Static", Primitive.Int32);
        public static readonly string Text = "not a definition";

        public readonly ConfigurationDefinition<int> Instance = ConfigurationDefinition.Define("Instance", Primitive.Int32);

        public static ConfigurationDefinition<int> Method() => ConfigurationDefinition.Define("Method", Primitive.Int32);
    }

    [RegisterConfiguration]
    public struct MarkedStruct
    {
        public static readonly ConfigurationDefinition<int> Field = ConfigurationDefinition.Define("Struct", Primitive.Int32);
    }

    [RegisterConfiguration]
    public static class SameDefinitionTwice
    {
        public static readonly ConfigurationDefinition<int> Field = ConfigurationDefinition.Define("Field", Primitive.Int32);

        public static ConfigurationDefinition<int> Property => Field;
    }

    public static class Unmarked
    {
        public static readonly ConfigurationDefinition<int> Field = ConfigurationDefinition.Define("Unmarked", Primitive.Int32);
    }

    [RegisterConfiguration]
    public static class Generic<T>
    {
        public static readonly ConfigurationDefinition<int> Field = ConfigurationDefinition.Define($"Generic:{typeof(T).Name}", Primitive.Int32);
    }

    [RegisterConfiguration]
    public static class InvalidMembers
    {
        public static readonly ConfigurationDefinition<int> Valid = ConfigurationDefinition.Define("Valid", Primitive.Int32);

        public static readonly ConfigurationDefinition<int>? Null = null;

        public static ConfigurationDefinition<int> Throwing => throw new InvalidOperationException("broken");
    }

    [RegisterConfiguration]
    public static class ThrowingInitializer
    {
        public static readonly ConfigurationDefinition<int> Definition = Fail();

        private static ConfigurationDefinition<int> Fail() => throw new InvalidOperationException("initializer failed");
    }
}
