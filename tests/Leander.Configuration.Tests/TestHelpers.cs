namespace Leander.Configuration.Tests;

internal static class TestHelpers
{
    public static ConfigurationReader Reader(params (string Key, string? Value)[] values) =>
        new(ValueSource.FromDictionary(values.Select(v => KeyValuePair.Create(v.Key, v.Value))));

    public static ConfigurationDiagnostic SingleError(ConfigurationReader reader) =>
        Assert.Single(reader.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);

    public static IEnumerable<ConfigurationDiagnostic> Errors(ConfigurationReader reader) =>
        reader.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);
}
