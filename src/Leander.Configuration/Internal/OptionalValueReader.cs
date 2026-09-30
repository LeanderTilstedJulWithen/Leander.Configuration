using Leander.Configuration.Descriptors;

namespace Leander.Configuration.Internal;

// Reads an optional value type. A present value goes through the inner definition, which never sees null.
// A missing value is reported as missing; the optional definition turns it into null.
internal sealed class OptionalValueReader<T>(ConfigurationDefinition<T> inner) : ValueReader<T?>
    where T : struct
{
    private readonly ConfigurationDefinition<T> _inner = inner;

    // A default set before Optional() would make the value never null.
    public override void Check(ContractChecker checker, ConfigurationDefinition definition)
    {
        if (_inner.HasDefault)
        {
            checker.Failures.Add($"{definition.Key}: Optional() cannot be combined with a default.");
        }

        _inner.Check(checker);
    }

    // The inner definition describes the value; the optional definition sets the presence.
    public override ValueDescriptor Describe(DescriptorContext context) => _inner.CreateValueDescriptor(context);

    public override string Format(DescriptorContext context, T? value) =>
        value is null ? "null" : _inner.Reader.Format(context, value.Value);

    public override ReadStatus Read(ReadContext context, ConfigurationDefinition definition, string key, out T? value)
    {
        var status = _inner.Reader.Read(context, definition, key, out var raw);
        value = status == ReadStatus.Read ? raw : null;
        return status;
    }
}
