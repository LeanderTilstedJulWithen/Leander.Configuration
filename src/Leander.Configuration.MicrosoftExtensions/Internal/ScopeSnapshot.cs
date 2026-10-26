namespace Leander.Configuration.MicrosoftExtensions.Internal;

// The current snapshot when a scope first asks for options, so that every IOptionsSnapshot<T> in the scope, of any T,
// comes from the same read.
internal sealed class ScopeSnapshot(ConfigurationSnapshotMonitor monitor)
{
    public ConfigurationSnapshot Snapshot { get; } = monitor.Current;
}
