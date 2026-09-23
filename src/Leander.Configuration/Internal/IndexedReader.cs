using System.Globalization;

namespace Leander.Configuration.Internal;

// Reads one entry per element: Key:0, Key:1, ... Each element goes through the element definition.
internal sealed class IndexedReader<T>(ConfigurationDefinition<T> element) : ValueReader<IReadOnlyList<T>>
{
    public override ReadStatus Read(ConfigurationReader reader, ConfigurationDefinition definition, string key, out IReadOnlyList<T> value)
    {
        value = [];

        var names = reader.Source.GetChildNames(key);
        var hasValue = reader.Source.GetValue(key) is not null;

        if (names.Count == 0)
        {
            if (hasValue)
            {
                reader.Report(DiagnosticSeverity.Warning, key, $"has a single value, but indexed entries ({key}:0, {key}:1, ...) are expected", definition);
            }

            return ReadStatus.Missing;
        }

        if (hasValue)
        {
            reader.Report(DiagnosticSeverity.Warning, key, "has a single value that is ignored, because indexed entries are expected", definition);
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
                reader.Report(DiagnosticSeverity.Error, $"{key}:{name}", $"'{name}' is not a valid index", definition);
                failed = true;
            }
        }

        entries.Sort((left, right) => left.Index.CompareTo(right.Index));

        for (var i = 1; i < entries.Count; i++)
        {
            if (entries[i].Index == entries[i - 1].Index)
            {
                reader.Report(DiagnosticSeverity.Error, key, $"index {entries[i].Index} is defined more than once", definition);
                failed = true;
            }
        }

        if (entries.Count > 0 && entries[^1].Index != entries.Count - 1)
        {
            reader.Report(DiagnosticSeverity.Warning, key, "indices are not contiguous from 0", definition);
        }

        var elements = new List<T>(entries.Count);
        foreach (var (_, name) in entries)
        {
            if (element.TryRead(reader, $"{key}:{name}", out var item))
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
