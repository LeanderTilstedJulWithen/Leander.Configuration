namespace Leander.Configuration.Internal;

// Reads an optional value type. A present value goes through the inner definition, which never sees null.
// A missing value is reported as missing; the optional definition turns it into null.
internal sealed class OptionalValueReader<T>(ConfigurationDefinition<T> inner) : ValueReader<T?>
    where T : struct
{
    public override void Resolve(ContractResolver resolver, ConfigurationDefinition definition) => inner.Resolve(resolver);

    public override ReadStatus Read(ReadContext context, ConfigurationDefinition definition, string key, out T? value)
    {
        value = null;

        var status = inner.Reader.Read(context, definition, key, out var raw);
        if (status != ReadStatus.Read)
        {
            return status;
        }

        if (!inner.TryProcess(context, key, raw, out var processed))
        {
            return ReadStatus.Failed;
        }

        value = processed;
        return ReadStatus.Read;
    }

    public override bool TryProcess(ReadContext context, ConfigurationDefinition definition, string key, T? value, out T? result)
    {
        result = null;

        if (value is null)
        {
            return true;
        }

        if (!inner.Reader.TryProcess(context, definition, key, value.Value, out var primitive) ||
            !inner.TryProcess(context, key, primitive, out var processed))
        {
            return false;
        }

        result = processed;
        return true;
    }
}
