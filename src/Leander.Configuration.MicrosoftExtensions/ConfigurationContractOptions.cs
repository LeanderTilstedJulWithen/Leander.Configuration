namespace Leander.Configuration.MicrosoftExtensions;

/// <summary>
/// How a configuration contract is registered.
/// </summary>
public sealed record ConfigurationContractOptions
{
    /// <summary>
    /// Reads with <see cref="ReadOptions.Default"/> and validates when the host starts.
    /// </summary>
    public static ConfigurationContractOptions Default { get; } = new();

    /// <summary>
    /// How strictly the configuration is read.
    /// </summary>
    public ReadOptions ReadOptions { get; init; } = ReadOptions.Default;

    /// <summary>
    /// Reads the configuration when the host starts, so an invalid configuration fails <c>StartAsync</c>.
    /// Otherwise, it fails on the first resolve of the snapshot or anything built from it.
    /// </summary>
    /// <remarks>
    /// Turn it off when the host is built without a valid configuration, e.g. to write documentation from the contract.
    /// </remarks>
    public bool ValidateOnStart { get; init; } = true;
}
