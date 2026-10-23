using System.Reflection;
using System.Reflection.Emit;
using Leander.Primitives;

namespace Leander.Configuration.Tests;

public class RegisterFromTests
{
    private static readonly ConfigurationDefinition<int> Alpha = ConfigurationDefinition.Define("Alpha", Primitive.Int32);
    private static readonly ConfigurationDefinition<int> Zeta = ConfigurationDefinition.Define("Zeta", Primitive.Int32);

    [Fact]
    public void RegisterFrom_MarkedType_RegistersFieldsThenPropertiesInDeclarationOrder()
    {
        var contract = new ConfigurationContractBuilder().RegisterFrom(typeof(MarkedType)).Build();

        Assert.Equal(["B:Field", "A:Field", "Private:Field", "Property", "Private:Property"], contract.Definitions.Select(d => d.Key));
    }

    [Fact]
    public void RegisterFrom_MarkedType_IgnoresInstanceMembersAndOtherTypes()
    {
        var contract = new ConfigurationContractBuilder().RegisterFrom(typeof(MarkedTypeWithOtherMembers)).Build();

        Assert.Equal(["Static"], contract.Definitions.Select(d => d.Key));
    }

    [Fact]
    public void RegisterFrom_MarkedMembers_RegistersOnlyThose()
    {
        var contract = new ConfigurationContractBuilder().RegisterFrom(typeof(MarkedMembers)).Build();

        Assert.Equal(["Marked:Field", "Marked:Property", "Method:Single", "Method:First", "Method:Second"], contract.Definitions.Select(d => d.Key));
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
        var builder = new ConfigurationContractBuilder().RegisterFrom(typeof(Unmarked));

        var message = Assert.Throws<InvalidOperationException>(builder.Build).Message;

        Assert.Contains(
            "Leander.Configuration.Tests.RegisterFromTests.Unmarked: neither the type nor any of its members is marked with RegisterConfiguration.",
            message);
    }

    [Fact]
    public void RegisterFrom_OpenGenericType_FailsAtBuild()
    {
        var message = Assert.Throws<InvalidOperationException>(new ConfigurationContractBuilder().RegisterFrom(typeof(Generic<>)).Build).Message;

        Assert.Contains("RegisterConfiguration can't be used in an open generic type.", message);
    }

    [Fact]
    public void RegisterFrom_ClosedGenericType_Registers()
    {
        var contract = new ConfigurationContractBuilder().RegisterFrom(typeof(Generic<int>)).Build();

        Assert.Equal(["Generic:Int32"], contract.Definitions.Select(d => d.Key));
    }

    [Fact]
    public void RegisterFrom_InvalidMembers_ReportsEveryFailureAtBuild()
    {
        // Nothing throws while registering.
        var builder = new ConfigurationContractBuilder().RegisterFrom(typeof(InvalidMembers));

        var message = Assert.Throws<InvalidOperationException>(builder.Build).Message;

        const string type = "Leander.Configuration.Tests.RegisterFromTests.InvalidMembers";
        Assert.Contains($"{type}.Instance: RegisterConfiguration needs a static member.", message);
        Assert.Contains($"{type}.Null: is null.", message);
        Assert.Contains($"{type}.Text: is a String, not a ConfigurationDefinition or IEnumerable<ConfigurationDefinition>.", message);
        Assert.Contains($"{type}.WithNull: contains null.", message);
        Assert.Contains($"{type}.WithParameter: RegisterConfiguration needs a field, a readable property or a parameterless, non-generic method.", message);
        Assert.Contains($"{type}.Throwing: could not be read: broken", message);
    }

    [Fact]
    public void RegisterFrom_InvalidMembers_StillRegistersTheValidOnes()
    {
        var builder = new ConfigurationContractBuilder()
            .Register(ConfigurationDefinition.Define("Valid", Primitive.Int32))
            .RegisterFrom(typeof(InvalidMembers));

        var message = Assert.Throws<InvalidOperationException>(builder.Build).Message;

        Assert.Contains("Valid: defined more than once.", message);
    }

    [Fact]
    public void RegisterFrom_ThrowingStaticInitializer_FailsAtBuild()
    {
        var message = Assert.Throws<InvalidOperationException>(new ConfigurationContractBuilder().RegisterFrom(typeof(ThrowingInitializer)).Build).Message;

        Assert.Contains("ThrowingInitializer.Definition: could not be read: initializer failed", message);
    }

    // Assemblies

    [Fact]
    public void RegisterFrom_Assembly_RegistersMarkedTypesInOrderOfFullName()
    {
        var assembly = DefineAssembly("Registration", ("Zeta.Definitions", nameof(ZetaDefinition)), ("Alpha.Definitions", nameof(AlphaDefinition)));

        var contract = new ConfigurationContractBuilder().RegisterFrom(assembly).Build();

        Assert.Equal(["Alpha", "Zeta"], contract.Definitions.Select(d => d.Key));
    }

    [Fact]
    public void RegisterFrom_AssemblyWithoutMarkedTypes_FailsAtBuild()
    {
        var assembly = DefineAssembly("NothingMarked");

        var message = Assert.Throws<InvalidOperationException>(new ConfigurationContractBuilder().RegisterFrom(assembly).Build).Message;

        Assert.Contains("NothingMarked: no types or members are marked with RegisterConfiguration.", message);
    }

    public static ConfigurationDefinition AlphaDefinition() => Alpha;

    public static ConfigurationDefinition ZetaDefinition() => Zeta;

    // An assembly made at runtime, with a marked type per name whose static property Definition calls the given method of this class.
    private static Assembly DefineAssembly(string name, params (string Type, string Method)[] types)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(name), AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule(name);
        var marked = new CustomAttributeBuilder(typeof(RegisterConfigurationAttribute).GetConstructor(Type.EmptyTypes)!, []);

        foreach (var (typeName, method) in types)
        {
            var type = module.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed);
            type.SetCustomAttribute(marked);

            var getter = type.DefineMethod(
                "get_Definition",
                MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                typeof(ConfigurationDefinition),
                Type.EmptyTypes);
            var il = getter.GetILGenerator();
            il.Emit(OpCodes.Call, typeof(RegisterFromTests).GetMethod(method)!);
            il.Emit(OpCodes.Ret);

            var property = type.DefineProperty("Definition", PropertyAttributes.None, typeof(ConfigurationDefinition), null);
            property.SetGetMethod(getter);
            type.CreateType();
        }

        return assembly;
    }

    [RegisterConfiguration]
    public static class MarkedType
    {
        public static readonly ConfigurationDefinition<int> BField = ConfigurationDefinition.Define("B:Field", Primitive.Int32);
        public static readonly ConfigurationDefinition<int> AField = ConfigurationDefinition.Define("A:Field", Primitive.Int32);

        public static ConfigurationDefinition<string> Property { get; } = ConfigurationDefinition.Define("Property", Primitive.String);

        private static ConfigurationDefinition<string> PrivateProperty { get; } = ConfigurationDefinition.Define("Private:Property", Primitive.String);

        private static readonly ConfigurationDefinition<int> PrivateField = ConfigurationDefinition.Define("Private:Field", Primitive.Int32);

        // Methods are only registered when marked themselves.
        public static ConfigurationDefinition<int> Method() => ConfigurationDefinition.Define("Method", Primitive.Int32);
    }

    [RegisterConfiguration]
    public sealed class MarkedTypeWithOtherMembers
    {
        public static readonly ConfigurationDefinition<int> Static = ConfigurationDefinition.Define("Static", Primitive.Int32);
        public static readonly string Text = "not a definition";

        public readonly ConfigurationDefinition<int> Instance = ConfigurationDefinition.Define("Instance", Primitive.Int32);
    }

    public static class MarkedMembers
    {
        [RegisterConfiguration]
        public static readonly ConfigurationDefinition<int> Field = ConfigurationDefinition.Define("Marked:Field", Primitive.Int32);

        public static readonly ConfigurationDefinition<int> NotMarked = ConfigurationDefinition.Define("NotMarked", Primitive.Int32);

        [RegisterConfiguration]
        private static ConfigurationDefinition<int> Property => PropertyValue;

        private static readonly ConfigurationDefinition<int> PropertyValue = ConfigurationDefinition.Define("Marked:Property", Primitive.Int32);

        [RegisterConfiguration]
        internal static ConfigurationDefinition Single() => SingleValue;

        [RegisterConfiguration]
        private static IEnumerable<ConfigurationDefinition> Several() => SeveralValues;

        private static readonly ConfigurationDefinition<int> SingleValue = ConfigurationDefinition.Define("Method:Single", Primitive.Int32);

        private static readonly ConfigurationDefinition[] SeveralValues =
        [
            ConfigurationDefinition.Define("Method:First", Primitive.Int32),
            ConfigurationDefinition.Define("Method:Second", Primitive.Int32),
        ];
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

    public sealed class InvalidMembers
    {
        [RegisterConfiguration]
        public static readonly ConfigurationDefinition<int> Valid = ConfigurationDefinition.Define("Valid", Primitive.Int32);

        [RegisterConfiguration]
        public static readonly ConfigurationDefinition<int>? Null = null;

        [RegisterConfiguration]
        public static readonly string Text = "not a definition";

        [RegisterConfiguration]
        public readonly ConfigurationDefinition<int> Instance = ConfigurationDefinition.Define("Instance", Primitive.Int32);

        [RegisterConfiguration]
        public static IEnumerable<ConfigurationDefinition?> WithNull() => [null];

        [RegisterConfiguration]
        public static ConfigurationDefinition WithParameter(int value) => Valid;

        [RegisterConfiguration]
        public static ConfigurationDefinition Throwing() => throw new InvalidOperationException("broken");
    }

    [RegisterConfiguration]
    public static class ThrowingInitializer
    {
        public static readonly ConfigurationDefinition<int> Definition = Fail();

        private static ConfigurationDefinition<int> Fail() => throw new InvalidOperationException("initializer failed");
    }
}
