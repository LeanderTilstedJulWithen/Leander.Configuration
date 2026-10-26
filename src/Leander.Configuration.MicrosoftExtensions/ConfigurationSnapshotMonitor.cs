using Leander.Configuration.MicrosoftExtensions.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Leander.Configuration.MicrosoftExtensions;

/// <summary>
/// Holds the current snapshot of a contract registered with <c>AddConfigurationContract</c>. With
/// <see cref="ConfigurationContractOptions.ReloadOnChange"/>, the contract is read again whenever <see cref="IConfiguration"/>
/// reloads, and a valid read with changed values becomes <see cref="Current"/>.
/// </summary>
/// <remarks>
/// A reload is all or nothing: an invalid configuration keeps the last valid snapshot, and goes to <see cref="OnReloadFailed"/>.
/// </remarks>
public sealed class ConfigurationSnapshotMonitor : IDisposable
{
    private readonly Listeners<ConfigurationSnapshot> _changeListeners = new();
    private readonly ConfigurationContract _contract;
    private readonly Listeners<InvalidConfigurationException> _failureListeners = new();
    private readonly ReadOptions _readOptions;
    private readonly Lock _reloadLock = new();
    private readonly IDisposable? _reloading;
    private readonly IValueSource _source;
    private ConfigurationSnapshot _current;

    // Reads the contract, and throws InvalidConfigurationException like the startup read always has.
    internal ConfigurationSnapshotMonitor(
        ConfigurationContract contract,
        IConfiguration configuration,
        ConfigurationContractOptions options)
    {
        _contract = contract;
        _readOptions = options.ReadOptions;
        _source = configuration.AsValueSource();
        Startup = _current = contract.Read(_source, _readOptions);
        ReloadsOnChange = options.ReloadOnChange;

        if (ReloadsOnChange)
        {
            _reloading = ChangeToken.OnChange(configuration.GetReloadToken, Reload);
        }
    }

    /// <summary>
    /// The latest valid snapshot. Without <see cref="ReloadsOnChange"/>, the snapshot read at startup.
    /// </summary>
    public ConfigurationSnapshot Current => Volatile.Read(ref _current);

    /// <summary>
    /// Whether the contract is read again when <see cref="IConfiguration"/> reloads.
    /// See <see cref="ConfigurationContractOptions.ReloadOnChange"/>.
    /// </summary>
    public bool ReloadsOnChange { get; }

    // The first snapshot, registered as the ConfigurationSnapshot singleton.
    internal ConfigurationSnapshot Startup { get; }

    /// <summary>
    /// Stops reloading.
    /// </summary>
    public void Dispose() => _reloading?.Dispose();

    /// <summary>
    /// Calls <paramref name="listener"/> with each new snapshot, after it has become <see cref="Current"/>.
    /// A reload that changes no value in the contract gives no new snapshot.
    /// </summary>
    /// <returns>Removes the listener when disposed.</returns>
    public IDisposable OnChange(Action<ConfigurationSnapshot> listener) => _changeListeners.Add(listener);

    /// <summary>
    /// Calls <paramref name="listener"/> when a reload finds the configuration invalid. <see cref="Current"/> keeps the last
    /// valid snapshot. The exception is the one startup would throw, and lists every error.
    /// </summary>
    /// <returns>Removes the listener when disposed.</returns>
    public IDisposable OnReloadFailed(Action<InvalidConfigurationException> listener) => _failureListeners.Add(listener);

    // One reload at a time, so that two quick changes can't become current in the wrong order.
    private void Reload()
    {
        lock (_reloadLock)
        {
            if (!_contract.TryRead(_source, _readOptions, out var snapshot, out var diagnostics))
            {
                _failureListeners.Invoke(new InvalidConfigurationException(diagnostics));
                return;
            }

            if (snapshot.HasSameValues(_current))
            {
                return;
            }

            Volatile.Write(ref _current, snapshot);
            _changeListeners.Invoke(snapshot);
        }
    }
}
