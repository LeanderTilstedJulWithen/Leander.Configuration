using Microsoft.Extensions.Options;

namespace Leander.Configuration.MicrosoftExtensions.Internal;

internal sealed class ScopedOptionsSnapshot<T>(ScopeSnapshot scope, SnapshotOptions<T> cache) : IOptionsSnapshot<T> where T : class
{
    private readonly SnapshotOptions<T> _cache = cache;
    private readonly ScopeSnapshot _scope = scope;

    public T Value => _cache.Get(_scope.Snapshot);

    public T Get(string? name) => _cache.Get(_scope.Snapshot, name);
}
