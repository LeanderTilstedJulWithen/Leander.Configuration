namespace Leander.Primitives.Parsing.Internal;

internal sealed class UriConverter : IConverter<Uri>
{
    public string Description => "An absolute URI, e.g. https://example.com.";

    // As written, so a default documents as it was declared: AbsoluteUri would add a trailing / to "https://example.com".
    public string Format(Uri value) => value.OriginalString;

    public bool TryParse(string input, out Uri result, out IReadOnlyList<IFormattableText<string>> errors)
    {
        errors = [];
        var success = Uri.TryCreate(input, UriKind.Absolute, out var uri);
        result = uri!;
        return success;
    }
}
