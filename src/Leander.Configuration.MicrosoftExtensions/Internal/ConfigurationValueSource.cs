using Microsoft.Extensions.Configuration;

namespace Leander.Configuration.MicrosoftExtensions.Internal;

internal sealed class ConfigurationValueSource(IConfiguration configuration) : IValueSource
{
    public string? GetValue(string key) => configuration[key];

    public IReadOnlyList<string> GetChildNames(string key) =>
        configuration.GetSection(key).GetChildren().Select(child => child.Key).ToList();
}
