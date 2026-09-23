using Leander.Parsing;

namespace Leander.Configuration.Internal;

// Reads a single entry holding a delimited list. Each item is normalized and validated by the element definition.
internal sealed class DelimitedReader<T>(ConfigurationDefinition<T> element, char delimiter) : ValueReader<IReadOnlyList<T>>
{
    public override ReadStatus Read(ConfigurationReader reader, ConfigurationDefinition definition, string key, out IReadOnlyList<T> value)
    {
        value = [];

        if (element.Reader is not ScalarReader<T> scalar)
        {
            reader.Report(DiagnosticSeverity.Error, key, "Delimited() requires a single-value element definition", definition);
            return ReadStatus.Failed;
        }

        var raw = reader.Source.GetValue(key);
        if (raw is null)
        {
            if (reader.Source.GetChildNames(key).Count > 0)
            {
                reader.Report(DiagnosticSeverity.Warning, key, $"has child entries, but a single '{delimiter}'-delimited value is expected", definition);
            }

            return ReadStatus.Missing;
        }

        if (!scalar.TryResolveConverter(reader, definition, key, out var converter))
        {
            return ReadStatus.Failed;
        }

        if (!Converters.List(converter, delimiter).TryParse(raw, out var items))
        {
            reader.Report(DiagnosticSeverity.Error, key, $"'{raw}' is not a valid '{delimiter}'-delimited list of {scalar.TypeDescription}", definition);
            return ReadStatus.Failed;
        }

        var failed = false;
        var elements = new List<T>(items.Count);

        for (var i = 0; i < items.Count; i++)
        {
            if (element.TryProcess(reader, $"{key}[{i}]", items[i], out var item))
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
