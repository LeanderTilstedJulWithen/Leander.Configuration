using Leander.Primitives;

namespace Leander.Configuration.Internal;

// Reads a single value. The primitive is given either as a definition or as the name of a registered primitive.
internal sealed class ScalarReader<T>(PrimitiveDefinition<T>? primitive, string? primitiveName) : ValueReader<T>
{
    public override void Resolve(ContractResolver resolver, ConfigurationDefinition definition)
    {
        if (primitiveName is not null)
        {
            resolver.Resolve<T>(this, definition.Key, primitiveName);
        }
        else
        {
            resolver.Resolve(this, definition.Key, primitive!);
        }
    }

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

        var success = GetPrimitive(context).TryParse(raw, out value, out var errors);
        Pipeline.Report(context, definition, key, errors);
        return success ? ReadStatus.Read : ReadStatus.Failed;
    }

    public override bool TryProcess(ReadContext context, ConfigurationDefinition definition, string key, T value, out T result)
    {
        var success = GetPrimitive(context).TryAccept(value, out result, out var errors);
        Pipeline.Report(context, definition, key, errors);
        return success;
    }

    public Primitive<T> GetPrimitive(ReadContext context) => context.Contract.GetPrimitive<T>(this);
}
