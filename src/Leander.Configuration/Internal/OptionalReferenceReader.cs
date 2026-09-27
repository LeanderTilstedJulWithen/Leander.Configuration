namespace Leander.Configuration.Internal;

// Reads an optional reference type. A present value goes through the inner definition, which never sees null.
// A missing value is reported as missing; the optional definition turns it into null.
internal sealed class OptionalReferenceReader<T>(ConfigurationDefinition<T> inner) : ValueReader<T?>
    where T : class
{
    private readonly ConfigurationDefinition<T> _inner = inner;

    // A default set before Optional() would make the value never null.
    public override void Resolve(ContractResolver resolver, ConfigurationDefinition definition)
    {
        if (_inner.HasDefault)
        {
            resolver.Failures.Add($"{definition.Key}: Optional() cannot be combined with a default.");
        }

        _inner.Resolve(resolver);
    }

    public override ReadStatus Read(ReadContext context, ConfigurationDefinition definition, string key, out T? value)
    {
        value = null;

        var status = _inner.Reader.Read(context, definition, key, out var raw);
        if (status != ReadStatus.Read)
        {
            return status;
        }

        if (!_inner.TryProcess(context, key, raw, out var processed))
        {
            return ReadStatus.Failed;
        }

        value = processed;
        return ReadStatus.Read;
    }
}
