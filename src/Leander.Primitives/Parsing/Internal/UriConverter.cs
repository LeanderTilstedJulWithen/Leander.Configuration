namespace Leander.Primitives.Parsing.Internal;

internal sealed class UriConverter : IConverter<Uri>
{
    public string Description => "An absolute URI, e.g. https://example.com.";

    public bool TryParse(string input, out Uri result)
    {
        var success = Uri.TryCreate(input, UriKind.Absolute, out var uri);
        result = uri!;
        return success;
    }

    // As written, so a default documents as it was declared: AbsoluteUri would add a trailing / to "https://example.com".
    public string Format(Uri value) => value.OriginalString;
}
