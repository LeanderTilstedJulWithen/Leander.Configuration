using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;

namespace Leander.Configuration.MicrosoftExtensions.Internal;

// Builds T at most once per snapshot, so IOptions<T>, IOptionsMonitor<T> and IOptionsSnapshot<T> share one instance
// while the snapshot is the same. Old snapshots and their options are collected with the snapshot.
internal sealed class SnapshotOptions<T>(Func<ConfigurationSnapshot, T> create) where T : class
{
    private readonly ConditionalWeakTable<ConfigurationSnapshot, T> _built = [];
    private readonly Func<ConfigurationSnapshot, T> _create = create;

    public T Get(ConfigurationSnapshot snapshot) => _built.GetValue(snapshot, snapshot => _create(snapshot));

    // AddOptionsFrom builds unnamed options only. A named one would quietly be the unnamed one.
    public T Get(ConfigurationSnapshot snapshot, string? name) =>
        string.IsNullOrEmpty(name)
            ? Get(snapshot)
            : throw new InvalidOperationException(
                $"Named options '{name}' of {typeof(T).Name} are not supported: AddOptionsFrom builds unnamed options " +
                $"only. Use the unnamed options ({nameof(Options)}.{nameof(Options.DefaultName)}).");
}
