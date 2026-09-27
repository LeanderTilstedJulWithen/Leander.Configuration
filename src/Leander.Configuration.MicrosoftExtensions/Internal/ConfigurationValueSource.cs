using Microsoft.Extensions.Configuration;

namespace Leander.Configuration.MicrosoftExtensions.Internal;

internal sealed class ConfigurationValueSource(IConfiguration configuration) : IValueSource
{
    private readonly IConfiguration _configuration = configuration;

    public string? GetValue(string key) => _configuration[key];

    public IReadOnlyList<string> GetChildNames(string key) =>
        _configuration.GetSection(key).GetChildren().Select(child => child.Key).ToList();
}
