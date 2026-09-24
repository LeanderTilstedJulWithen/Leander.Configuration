using System.Text.Json;

namespace Leander.Primitives.Parsing.Internal;

internal sealed class JsonValueConverter<T>(JsonSerializerOptions? options) : IConverter<T>
{
    public bool TryParse(string input, out T result)
    {
        try
        {
            result = JsonSerializer.Deserialize<T>(input, options)!;
            return true;
        }
        catch (JsonException)
        {
            result = default!;
            return false;
        }
    }

    public string Format(T value) => JsonSerializer.Serialize(value, options);
}
