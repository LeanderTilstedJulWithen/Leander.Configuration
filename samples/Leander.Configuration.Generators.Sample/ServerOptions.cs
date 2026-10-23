namespace Leander.Configuration.Generators.Sample;

// A positional record: each constructor parameter names the definition it is read from.
// The generator writes FromSnapshot as new ServerOptions(snapshot.Get(ServerConfiguration.Host), ...), nothing more.
public sealed partial record ServerOptions(
    [FromDefinition(nameof(ServerConfiguration.Host))] string Host,
    [FromDefinition(nameof(ServerConfiguration.Port))] int Port,
    [FromDefinition(nameof(ServerConfiguration.AllowedOrigins))] IReadOnlyList<Uri> AllowedOrigins,
    [FromDefinition(nameof(ServerConfiguration.CertificatePath))] string? CertificatePath)
{
    [GeneratedOptions(typeof(ServerConfiguration))]
    public static partial ServerOptions FromSnapshot(ConfigurationSnapshot snapshot);
}
