using Leander.Configuration.MicrosoftExtensions.Internal;
using Microsoft.Extensions.Configuration;

namespace Leander.Configuration.MicrosoftExtensions;

public static class ConfigurationExtensions
{
    // Reads the configuration live, without copying: each contract.Read sees the values as they are at that moment.
    public static IValueSource AsValueSource(this IConfiguration configuration) =>
        new ConfigurationValueSource(configuration);
}
