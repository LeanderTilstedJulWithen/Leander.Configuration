using Microsoft.Extensions.Options;

namespace Leander.Configuration.MicrosoftExtensions.Internal;

// Replaces the default factory, which would create T with new() and bind it by convention.
// There is one snapshot, so every name gets an instance built from it.
internal sealed class SnapshotOptionsFactory<T>(ConfigurationSnapshot snapshot, Func<ConfigurationSnapshot, T> create) : IOptionsFactory<T>
    where T : class
{
    private readonly ConfigurationSnapshot _snapshot = snapshot;
    private readonly Func<ConfigurationSnapshot, T> _create = create;

    public T Create(string name) => _create(_snapshot);
}
