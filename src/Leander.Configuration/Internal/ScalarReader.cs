using System.Diagnostics.CodeAnalysis;
using Leander.Parsing;

namespace Leander.Configuration.Internal;

internal sealed class ScalarReader<T>(IConverter<T>? converter, string? converterKey) : ValueReader<T>
{
    public string TypeDescription => converterKey is null ? TypeNames.Get(typeof(T)) : $"{TypeNames.Get(typeof(T))} ({converterKey})";

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

        if (!TryResolveConverter(reader, definition, key, out var resolved))
        {
            return ReadStatus.Failed;
        }

        if (!resolved.TryParse(raw, out value))
        {
            reader.Report(DiagnosticSeverity.Error, key, $"'{raw}' is not a valid {TypeDescription}", definition);
            return ReadStatus.Failed;
        }

        return ReadStatus.Read;
    }

    public bool TryResolveConverter(
        ConfigurationReader reader,
        ConfigurationDefinition definition,
        string key,
        [NotNullWhen(true)] out IConverter<T>? resolved) =>
        reader.TryResolveConverter(converter, converterKey, key, definition, out resolved);
}
