namespace Leander.Configuration.MicrosoftExtensions.Sample;

// Written by the application. Construction is explicit; see ServerOptions.From.
public sealed record ServerOptions(
    string Host,
    int Port,
    IReadOnlyList<Uri> AllowedOrigins,
    IReadOnlyList<string> Features)
{
    // The snapshot is already validated, so reading values cannot fail.
    public static ServerOptions From(ConfigurationSnapshot configuration) => new(
        configuration.Get(ServerConfiguration.Host),
        configuration.Get(ServerConfiguration.Port),
        configuration.Get(ServerConfiguration.AllowedOrigins),
        configuration.Get(ServerConfiguration.Features));
}
