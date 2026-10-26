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
    /// Reads the contract again whenever <see cref="Microsoft.Extensions.Configuration.IConfiguration"/> reloads, so that
    /// <see cref="ConfigurationSnapshotMonitor.Current"/>, <c>IOptionsMonitor&lt;T&gt;</c> and <c>IOptionsSnapshot&lt;T&gt;</c>
    /// follow the configuration. Off by default.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A reload is all or nothing. An invalid configuration keeps the last valid snapshot, and goes to
    /// <see cref="ConfigurationSnapshotMonitor.OnReloadFailed"/>. A reload that changes no value in the contract keeps the
    /// current snapshot.
    /// </para>
    /// <para>
    /// The <see cref="ConfigurationSnapshot"/> service and <c>IOptions&lt;T&gt;</c> keep the values read at startup.
    /// </para>
    /// </remarks>
    public bool ReloadOnChange { get; init; }

    /// <summary>
    /// Reads the configuration when the host starts, so an invalid configuration fails <c>StartAsync</c>.
    /// Otherwise, it fails on the first resolve of the snapshot or anything built from it.
    /// </summary>
    /// <remarks>
    /// Turn it off when the host is built without a valid configuration, e.g. to write documentation from the contract.
    /// </remarks>
    public bool ValidateOnStart { get; init; } = true;
}
