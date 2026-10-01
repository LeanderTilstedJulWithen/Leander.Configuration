namespace Leander.Configuration;

// How strictly a contract is read. The same contract can be read strictly in CI and loosely in development.
public sealed record ReadOptions
{
    public static ReadOptions Default { get; } = new();

    // Sections the application owns, e.g. "Server". A key under one of them that the contract doesn't define
    // is reported as a warning, e.g. a misspelled "Server:Prot". Other parts of the source are never checked:
    // an IConfiguration holds Logging, AllowedHosts, environment variables and more.
    public IReadOnlyList<string> CheckedSections { get; init; } = [];

    // Every warning is reported as an error, so it prevents a snapshot and shows in the exception message.
    public bool WarningsAsErrors { get; init; }
}
