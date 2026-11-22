using Leander.Configuration.Descriptors;
using Leander.Primitives;

namespace Leander.Configuration.Internal;

// Reads a single value with its primitive.
internal sealed class ScalarReader<T>(Primitive<T> primitive) : ValueReader<T>
{
    private readonly Primitive<T> _primitive = primitive;

    public override Primitive Primitive => _primitive;

    public override void Check(ContractChecker checker, ConfigurationDefinition definition) =>
        checker.Check(definition.Key, _primitive);

    public override ValueDescriptor Describe(DescriptorContext context) => context.DescribeValue(_primitive, ValueForm.Scalar);

    public override string Format(DescriptorContext context, T value) => _primitive.Converter.Format(value);

    public override ReadStatus Read(ReadContext context, ConfigurationDefinition definition, string key, out T value)
    {
        value = default!;

        var raw = context.Source.GetValue(key);
        if (raw is null)
        {
            if (context.Source.GetChildNames(key).Count > 0)
            {
                context.Report(DiagnosticSeverity.Warning, key, "has child entries, but a single value is expected", definition);
            }

            return ReadStatus.Missing;
        }

        // On failure the value is default, and the status says not to use it.
        var success = _primitive.TryParse(raw, context.IsSensitive, out var parsed, out var errors);
        value = parsed!;
        Pipeline.Report(context, definition, key, errors);
        return success ? ReadStatus.Read : ReadStatus.Failed;
    }

    public override bool TryProcess(ReadContext context, ConfigurationDefinition definition, string key, T value, out T result)
    {
        var success = _primitive.TryAccept(value, context.IsSensitive, out var accepted, out var errors);
        result = accepted!;
        Pipeline.Report(context, definition, key, errors);
        return success;
    }
}
