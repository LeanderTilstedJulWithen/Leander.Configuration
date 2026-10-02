using Leander.Configuration.Descriptors;
using Leander.Configuration.Tooling.Internal;

namespace Leander.Configuration.Tooling;

/// <summary>
/// Writes an example configuration for a contract descriptor, with every key, to copy and fill in.
/// </summary>
public static class ConfigurationGenerator
{
    /// <summary>
    /// Writes the example for <paramref name="contract"/> as indented, <c>appsettings.json</c>-style JSON.
    /// </summary>
    /// <remarks>
    /// Every value is a string, the text the converter formats. Without a default, a placeholder says what is expected,
    /// e.g. <c>"&lt;Port&gt;"</c>, because JSON has no comments. Sensitive values are always <c>"&lt;secret&gt;"</c>.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// A key is both a value and a section, e.g. <c>Server</c> and <c>Server:Port</c>.
    /// </exception>
    public static string WriteJson(ContractDescriptor contract) => JsonConfiguration.Write(contract);
}
