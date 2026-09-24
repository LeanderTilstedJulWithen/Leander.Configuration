using Leander.Primitives.Parsing;

namespace Leander.Configuration.Internal;

// Reads a single entry holding a delimited list. Each item goes through the element's primitive.
internal sealed class DelimitedReader<T>(ConfigurationDefinition<T> element, char delimiter) : ValueReader<IReadOnlyList<T>>
{
    public override void Resolve(ContractResolver resolver, ConfigurationDefinition definition)
    {
        if (element.Reader is ScalarReader<T>)
        {
            element.Resolve(resolver);
        }
        else
        {
            resolver.Failures.Add($"{definition.Key}: Delimited() requires a single-value element definition.");
        }
    }

    public override ReadStatus Read(ReadContext context, ConfigurationDefinition definition, string key, out IReadOnlyList<T> value)
    {
        value = [];

        var raw = context.Source.GetValue(key);
        if (raw is null)
        {
            if (context.Source.GetChildNames(key).Count > 0)
            {
                context.Report(DiagnosticSeverity.Warning, key, $"has child entries, but a single '{delimiter}'-delimited value is expected", definition);
            }

            return ReadStatus.Missing;
        }

        // Resolution guarantees a scalar element.
        var scalar = (ScalarReader<T>)element.Reader;
        var primitive = scalar.GetPrimitive(context);

        if (!Converters.List(primitive.Converter, delimiter).TryParse(raw, out var items))
        {
            context.Report(DiagnosticSeverity.Error, key, $"'{raw}' is not a valid '{delimiter}'-delimited list of {ScalarReader<T>.Describe(primitive)}", definition);
            return ReadStatus.Failed;
        }

        var failed = false;
        var elements = new List<T>(items.Count);

        for (var i = 0; i < items.Count; i++)
        {
            if (scalar.TryProcess(context, definition, $"{key}[{i}]", items[i], out var item))
            {
                elements.Add(item);
            }
            else
            {
                failed = true;
            }
        }

        if (failed)
        {
            return ReadStatus.Failed;
        }

        value = elements;
        return ReadStatus.Read;
    }
}
