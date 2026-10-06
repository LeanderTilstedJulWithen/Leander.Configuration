namespace Leander.Primitives.Parsing.Internal;

internal sealed class UriConverter : IConverter<Uri>
{
    public string Description => "An absolute URI, e.g. https://example.com.";

    // As written, so a default documents as it was declared: AbsoluteUri would add a trailing / to "https://example.com".
    public string Format(Uri value) => value.OriginalString;

    public bool TryParse(string input, out Uri result, out IReadOnlyList<IFormattableText<string>> errors)
    {
        errors = [];
        // The scheme must be written: .NET reads local paths as file URIs, and what looks like a path
        // depends on the OS ("/etc/app" on Unix, "C:\app" on Windows).
        var success = Uri.TryCreate(input, UriKind.Absolute, out var uri)
            && input.StartsWith(uri.Scheme + ":", StringComparison.OrdinalIgnoreCase);
        result = uri!;
        return success;
    }
}
