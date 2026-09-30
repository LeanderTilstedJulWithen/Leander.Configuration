using System.Globalization;
using Leander.Configuration.Descriptors;
using Leander.Primitives;

namespace Leander.Configuration.Internal;

// Reads one entry per item: Key:0, Key:1, ... Each item goes through the list's element primitive,
// then the list's own rules run on the list. The delimiter isn't used.
internal sealed class IndexedReader<T>(ListPrimitive<T> list) : ValueReader<IReadOnlyList<T>>
{
    private readonly ListPrimitive<T> _list = list;
    private readonly ScalarReader<T> _element = new(list.Element);

    public override void Check(ContractChecker checker, ConfigurationDefinition definition) =>
        checker.Check(definition.Key, _list);

    public override ValueDescriptor Describe(DescriptorContext context) => context.DescribeValue(_list, ValueForm.Indexed);

    // Like any list default: formatted by the list's converter.
    public override string Format(DescriptorContext context, IReadOnlyList<T> value) => _list.Converter.Format(value);

    public override ReadStatus Read(ReadContext context, ConfigurationDefinition definition, string key, out IReadOnlyList<T> value)
    {
        value = [];

        var names = context.Source.GetChildNames(key);
        var hasValue = context.Source.GetValue(key) is not null;

        if (names.Count == 0)
        {
            if (hasValue)
            {
                context.Report(DiagnosticSeverity.Warning, key, $"has a single value, but indexed entries ({key}:0, {key}:1, ...) are expected", definition);
            }

            return ReadStatus.Missing;
        }

        if (hasValue)
        {
            context.Report(DiagnosticSeverity.Warning, key, "has a single value that is ignored, because indexed entries are expected", definition);
        }

        var failed = false;
        var entries = new List<(int Index, string Name)>(names.Count);

        foreach (var name in names)
        {
            if (int.TryParse(name, NumberStyles.None, CultureInfo.InvariantCulture, out var index))
            {
                entries.Add((index, name));
            }
            else
            {
                context.Report(DiagnosticSeverity.Error, $"{key}:{name}", $"'{name}' is not a valid index", definition);
                failed = true;
            }
        }

        entries.Sort((left, right) => left.Index.CompareTo(right.Index));

        for (var i = 1; i < entries.Count; i++)
        {
            if (entries[i].Index == entries[i - 1].Index)
            {
                context.Report(DiagnosticSeverity.Error, key, $"index {entries[i].Index} is defined more than once", definition);
                failed = true;
            }
        }

        if (entries.Count > 0 && entries[^1].Index != entries.Count - 1)
        {
            context.Report(DiagnosticSeverity.Warning, key, "indices are not contiguous from 0", definition);
        }

        var items = new List<T>(entries.Count);
        foreach (var (_, name) in entries)
        {
            var itemKey = $"{key}:{name}";
            switch (_element.Read(context, definition, itemKey, out var item))
            {
                case ReadStatus.Read:
                    items.Add(item);
                    break;

                case ReadStatus.Missing:
                    context.Report(DiagnosticSeverity.Error, itemKey, "value is required", definition);
                    failed = true;
                    break;

                default:
                    failed = true;
                    break;
            }
        }

        if (failed)
        {
            return ReadStatus.Failed;
        }

        // Only the list's rules: the items went through the element primitive already.
        var errors = new List<string>();
        var success = _list.TryApplyRules(items, out value, errors, context.IsSensitive);
        Pipeline.Report(context, definition, key, errors);
        return success ? ReadStatus.Read : ReadStatus.Failed;
    }

    public override bool TryProcess(ReadContext context, ConfigurationDefinition definition, string key, IReadOnlyList<T> value, out IReadOnlyList<T> result)
    {
        var success = _list.TryAccept(value, context.IsSensitive, out result, out var errors);
        Pipeline.Report(context, definition, key, errors);
        return success;
    }
}
