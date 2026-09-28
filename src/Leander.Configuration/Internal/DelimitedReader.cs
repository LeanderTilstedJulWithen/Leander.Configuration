using Leander.Configuration.Descriptors;
using Leander.Primitives.Internal;
using Leander.Primitives.Parsing;

namespace Leander.Configuration.Internal;

// Reads a single entry holding a delimited list. Each item goes through the element's primitive.
internal sealed class DelimitedReader<T>(ConfigurationDefinition<T> element, char delimiter) : ValueReader<IReadOnlyList<T>>
{
    private readonly ConfigurationDefinition<T> _element = element;
    private readonly char _delimiter = delimiter;

    public override void Check(ContractChecker checker, ConfigurationDefinition definition)
    {
        if (_element.Reader is ScalarReader<T>)
        {
            _element.Check(checker);
        }
        else
        {
            checker.Failures.Add($"{definition.Key}: Delimited() requires a single-value element definition.");
        }
    }

    public override ValueDescriptor Describe(DescriptorContext context) =>
        new(TypeNames.Get(typeof(IReadOnlyList<T>)), ValuePresence.Required, ValueForm.Delimited)
        {
            Delimiter = _delimiter,
            Element = _element.CreateValueDescriptor(context),
        };

    public override string Format(DescriptorContext context, IReadOnlyList<T> value) =>
        string.Join(_delimiter, value.Select(item => _element.Reader.Format(context, item)));

    public override ReadStatus Read(ReadContext context, ConfigurationDefinition definition, string key, out IReadOnlyList<T> value)
    {
        value = [];

        if (_element.HasDefault)
        {
            context.Report(DiagnosticSeverity.Warning, key, "Default() before Delimited() is not used. Did you mean Delimited().Default(...)?", definition);
        }

        var raw = context.Source.GetValue(key);
        if (raw is null)
        {
            if (context.Source.GetChildNames(key).Count > 0)
            {
                context.Report(DiagnosticSeverity.Warning, key, $"has child entries, but a single '{_delimiter}'-delimited value is expected", definition);
            }

            return ReadStatus.Missing;
        }

        // The contract check guarantees a scalar element.
        var scalar = (ScalarReader<T>)_element.Reader;
        var primitive = scalar.Primitive;

        if (!Converters.List(primitive.Converter, _delimiter).TryParse(raw, out var items))
        {
            var shown = context.IsSensitive ? "value" : $"'{raw}'";
            context.Report(DiagnosticSeverity.Error, key, $"{shown} is not a valid '{_delimiter}'-delimited list of {primitive.DisplayName}", definition);
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
