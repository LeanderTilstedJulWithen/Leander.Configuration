using System.Text.Json;

namespace Leander.Primitives.Parsing.Internal;

internal sealed class JsonValueConverter<T>(JsonSerializerOptions? options) : IConverter<T>
{
    private readonly JsonSerializerOptions? _options = options;

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

    public string Format(T value) => JsonSerializer.Serialize(value, _options);
}
