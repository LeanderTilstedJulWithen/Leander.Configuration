namespace Leander.Configuration.Sample;

// Written by the application. Construction is explicit; see ServerOptions.From.
public sealed record ServerOptions(
    string Host,
    int Port,
    TimeSpan RequestTimeout,
    int? MaxConnections,
    IReadOnlyList<Uri> AllowedOrigins,
    IReadOnlyList<string> Features,
    string AdminEmail,
    string? BackupEmail,
    Verbosity Verbosity)
{
    // The snapshot is already validated, so reading values cannot fail.
    public static ServerOptions From(ConfigurationSnapshot configuration) => new(
        configuration.Get(ServerConfiguration.Host),
        configuration.Get(ServerConfiguration.Port),
        configuration.Get(ServerConfiguration.RequestTimeout),
        configuration.Get(ServerConfiguration.MaxConnections),
        configuration.Get(ServerConfiguration.AllowedOrigins),
        configuration.Get(ServerConfiguration.Features),
        configuration.Get(ServerConfiguration.AdminEmail),
        configuration.Get(ServerConfiguration.BackupEmail),
        configuration.Get(ServerConfiguration.Verbosity));
}
