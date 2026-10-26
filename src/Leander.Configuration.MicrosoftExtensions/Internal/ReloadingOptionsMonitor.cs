using Microsoft.Extensions.Options;

namespace Leander.Configuration.MicrosoftExtensions.Internal;

internal sealed class ReloadingOptionsMonitor<T>(ConfigurationSnapshotMonitor monitor, SnapshotOptions<T> cache)
    : IOptionsMonitor<T> where T : class
{
    private readonly SnapshotOptions<T> _cache = cache;
    private readonly ConfigurationSnapshotMonitor _monitor = monitor;

    public T CurrentValue => _cache.Get(_monitor.Current);

    public T Get(string? name) => _cache.Get(_monitor.Current, name);

    public IDisposable OnChange(Action<T, string?> listener) =>
        _monitor.OnChange(snapshot => listener(_cache.Get(snapshot), Options.DefaultName));
}
