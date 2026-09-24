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

    public override ReadStatus Read(ConfigurationReader reader, ConfigurationDefinition definition, string key, out T value)
    {
        value = default!;

        var raw = reader.Source.GetValue(key);
        if (raw is null)
        {
            if (reader.Source.GetChildNames(key).Count > 0)
            {
                reader.Report(DiagnosticSeverity.Warning, key, "has child entries, but a single value is expected", definition);
            }

            return ReadStatus.Missing;
        }

        var resolved = GetPrimitive(reader);
        if (!resolved.Converter.TryParse(raw, out var parsed))
        {
            reader.Report(DiagnosticSeverity.Error, key, $"'{raw}' is not a valid {Describe(resolved)}", definition);
            return ReadStatus.Failed;
        }

        return TryProcess(reader, definition, key, parsed, out value) ? ReadStatus.Read : ReadStatus.Failed;
    }

    public override bool TryProcess(ConfigurationReader reader, ConfigurationDefinition definition, string key, T value, out T result)
    {
        var resolved = GetPrimitive(reader);
        return Pipeline.TryProcess(reader, definition, key, resolved.Normalizers, resolved.Validators, value, out result);
    }

    public Primitive<T> GetPrimitive(ConfigurationReader reader) => reader.Contract.GetPrimitive<T>(this);

    public static string Describe(Primitive<T> resolved) =>
        resolved.Name is null ? TypeNames.Get(typeof(T)) : $"{TypeNames.Get(typeof(T))} ({resolved.Name})";
}
