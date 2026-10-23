namespace Leander.Configuration.MicrosoftExtensions.Internal;

// Listeners called in the order they were added. Adding and removing copy the list, so a listener may add or remove
// listeners while it is called.
internal sealed class Listeners<T>
{
    private readonly Lock _lock = new();
    private Action<T>[] _listeners = [];

    public IDisposable Add(Action<T> listener)
    {
        lock (_lock)
        {
            _listeners = [.. _listeners, listener];
        }

        return new Subscription(this, listener);
    }

    public void Invoke(T value)
    {
        foreach (var listener in Volatile.Read(ref _listeners))
        {
            listener(value);
        }
    }

    private void Remove(Action<T> listener)
    {
        lock (_lock)
        {
            var index = Array.IndexOf(_listeners, listener);

            if (index >= 0)
            {
                _listeners = [.. _listeners[..index], .. _listeners[(index + 1)..]];
            }
        }
    }

    private sealed class Subscription(Listeners<T> listeners, Action<T> listener) : IDisposable
    {
        private readonly Action<T> _listener = listener;
        private readonly Listeners<T> _listeners = listeners;

        public void Dispose() => _listeners.Remove(_listener);
    }
}
