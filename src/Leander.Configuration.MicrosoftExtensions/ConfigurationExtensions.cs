using Leander.Configuration.MicrosoftExtensions.Internal;
using Microsoft.Extensions.Configuration;

namespace Leander.Configuration.MicrosoftExtensions;

/// <summary>
/// Reads an <see cref="IConfiguration"/> as an <see cref="IValueSource"/>.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Returns a value source that reads <paramref name="configuration"/> live, without copying:
    /// each read of a contract sees the values as they are at that moment.
    /// </summary>
    /// <remarks>
    /// A section reads its keys relative to the section.
    /// </remarks>
    public static IValueSource AsValueSource(this IConfiguration configuration) =>
        new ConfigurationValueSource(configuration);
}
