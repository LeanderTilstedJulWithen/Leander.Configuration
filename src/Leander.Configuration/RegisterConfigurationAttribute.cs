namespace Leander.Configuration;

/// <summary>
/// Marks configuration definitions for <see cref="ConfigurationContractBuilder.RegisterFrom(System.Reflection.Assembly)"/>
/// and <see cref="ConfigurationContractBuilder.RegisterFrom(Type)"/>, which find them through reflection.
/// </summary>
/// <remarks>
/// <para>
/// On a type: every static field and property of the type whose type is a <see cref="ConfigurationDefinition"/>, of any accessibility.
/// </para>
/// <para>
/// On a member: that static field, property or parameterless method, of any accessibility. Its value is a
/// <see cref="ConfigurationDefinition"/> or an <see cref="IEnumerable{T}"/> of them, e.g. definitions built in a method.
/// </para>
/// <para>
/// Opt-in: <see cref="ConfigurationContractBuilder.Register"/> stays the default, and nothing is registered without this attribute.
/// </para>
/// </remarks>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method,
    Inherited = false)]
public sealed class RegisterConfigurationAttribute : Attribute;
