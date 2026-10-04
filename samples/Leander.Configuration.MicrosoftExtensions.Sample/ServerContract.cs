namespace Leander.Configuration.MicrosoftExtensions.Sample;

// The contract every example reads, and how strictly.
public static class ServerContract
{
    // Built once at startup. It fails fast if a key is defined twice or two primitives share a name.
    public static readonly ConfigurationContract Contract = new ConfigurationContractBuilder()
        .Register(ServerConfiguration.Host)
        .Register(ServerConfiguration.Port)
        .Register(ServerConfiguration.AllowedOrigins)
        .Register(ServerConfiguration.Features)
        .Register(ServerConfiguration.CertificatePath)
        .Build();

    // Server belongs to this application, so a key there that the contract doesn't define is a mistake, e.g. Server:Prot.
    // The rest of the configuration (Logging, environment variables, ...) isn't checked.
    public static readonly ReadOptions ReadOptions = new() { CheckedSections = ["Server"], WarningsAsErrors = true };
}
