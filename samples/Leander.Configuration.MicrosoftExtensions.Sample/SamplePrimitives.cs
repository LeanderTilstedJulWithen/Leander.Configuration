using Leander.Primitives;
using Leander.Primitives.Normalization;
using Leander.Primitives.Parsing;
using Leander.Primitives.Validation;

namespace Leander.Configuration.MicrosoftExtensions.Sample;

// Reusable kinds of values. The rules live here, not on the configuration keys that use them.
public static class SamplePrimitives
{
    public static readonly ListPrimitive<string> Features = new("Features", Primitive.String)
    {
        Description = "Comma-separated feature names.",
    };

    public static readonly Primitive<string> FilePath = new("FilePath", Primitive.String)
    {
        Normalizers = [Normalizers.FullPath],
        Description = "A file path, made absolute.",
    };

    public static readonly ListPrimitive<Uri> Origins = new("Origins", Primitive.Uri)
    {
        Validators = [Validators.Collections.NotEmpty<Uri>()],
        Description = "Origins, at least one.",
    };

    public static readonly Primitive<int> Port = new("Port", Converters.Int32)
    {
        Validators = [Validators.InRange(1, 65535)],
        Description = "A TCP port.",
    };
}
