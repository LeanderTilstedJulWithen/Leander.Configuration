using System.Text.Json;
using Leander.Primitives.Internal;

namespace Leander.Primitives.Parsing.Internal;

internal sealed class JsonValueConverter<T>(JsonSerializerOptions? options) : IConverter<T>
{
    private readonly JsonSerializerOptions? _options = options;

    public string Description => $"{TypeNames.Get(typeof(T))} as JSON.";

    public string Format(T value) => JsonSerializer.Serialize(value, _options);

    public bool TryParse(string input, out T result)
    {
        try
        {
            result = JsonSerializer.Deserialize<T>(input, _options)!;
            return true;
        }
        catch (JsonException)
        {
            result = default!;
            return false;
        }
    }
}
