using Leander.Configuration.Descriptors;
using Leander.Primitives;

namespace Leander.Configuration.Internal;

// Reads a single value with its primitive.
internal sealed class ScalarReader<T>(Primitive<T> primitive) : ValueReader<T>
{
    private readonly Primitive<T> _primitive = primitive;

    public Primitive<T> Primitive => _primitive;

    public override void Check(ContractChecker checker, ConfigurationDefinition definition) =>
        checker.Check(definition.Key, _primitive);

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

        var success = _primitive.TryParse(raw, context.IsSensitive, out value, out var errors);
        Pipeline.Report(context, definition, key, errors);
        return success ? ReadStatus.Read : ReadStatus.Failed;
    }

    public override bool TryProcess(ReadContext context, ConfigurationDefinition definition, string key, T value, out T result)
    {
        var success = _primitive.TryAccept(value, context.IsSensitive, out result, out var errors);
        Pipeline.Report(context, definition, key, errors);
        return success;
    }

    public override ValueDescriptor Describe(DescriptorContext context) => context.DescribeScalar(_primitive);

    public override string Format(DescriptorContext context, T value) => _primitive.Converter.Format(value);
}
