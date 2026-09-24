namespace Leander.Primitives.Parsing.Internal;

internal sealed class UriConverter : IConverter<Uri>
{
    public bool TryParse(string input, out Uri result)
    {
        var success = Uri.TryCreate(input, UriKind.Absolute, out var uri);
        result = uri!;
        return success;
    }

    public string Format(Uri value) => value.AbsoluteUri;
}
