namespace Leander.Configuration;

/// <summary>
/// Marks a type whose definitions <see cref="ConfigurationContractBuilder.RegisterFrom(Type)"/> registers:
/// every static field and property of the type whose type is a <see cref="ConfigurationDefinition"/>, of any accessibility.
/// </summary>
/// <remarks>
/// Opt-in: <see cref="ConfigurationContractBuilder.Register"/> stays the default, and nothing is registered without this attribute.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class RegisterConfigurationAttribute : Attribute;
