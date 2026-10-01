namespace Leander.Configuration;

/// <summary>
/// How strictly a contract is read.
/// </summary>
/// <remarks>
/// The same contract can be read strictly in CI and loosely in development.
/// </remarks>
public sealed record ReadOptions
{
    /// <summary>
    /// Checks no sections and keeps warnings as warnings.
    /// </summary>
    public static ReadOptions Default { get; } = new();

    /// <summary>
    /// Sections the application owns, e.g. <c>Server</c>. A key under one of them that the contract doesn't define
    /// is reported as a warning, e.g. a misspelled <c>Server:Prot</c>.
    /// </summary>
    /// <remarks>
    /// Other parts of the source are never checked: an <c>IConfiguration</c> holds Logging, AllowedHosts,
    /// environment variables and more.
    /// </remarks>
    public IReadOnlyList<string> CheckedSections { get; init; } = [];

    /// <summary>
    /// Reports every warning as an error, so it prevents a snapshot and shows in the exception message.
    /// </summary>
    public bool WarningsAsErrors { get; init; }
}
