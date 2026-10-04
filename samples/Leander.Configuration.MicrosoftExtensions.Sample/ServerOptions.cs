namespace Leander.Configuration.MicrosoftExtensions.Sample;

// Written by the application and injected as it is, without IOptions. Construction is explicit; see FromSnapshot.
public sealed record ServerOptions(
    string Host,
    int Port,
    IReadOnlyList<string> Features,
    string? CertificatePath)
{
    // The snapshot is already validated, so reading values cannot fail.
    public static ServerOptions FromSnapshot(ConfigurationSnapshot configuration) => new(
        configuration.Get(ServerConfiguration.Host),
        configuration.Get(ServerConfiguration.Port),
        configuration.Get(ServerConfiguration.Features),
        configuration.Get(ServerConfiguration.CertificatePath));
}
