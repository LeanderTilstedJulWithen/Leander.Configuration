using Leander.Primitives;
using Leander.Primitives.Normalization;
using Leander.Primitives.Validation;

namespace Leander.Configuration.Tooling.Sample;

// Reusable kinds of values. Their formats and rules show up on every key that uses them.
public static class SamplePrimitives
{
    public static readonly Primitive<int> ConnectionLimit = new("ConnectionLimit", Primitive.Int32)
    {
        Validators = [Validators.GreaterThan(0)],
        Description = "A maximum number of connections.",
    };

    public static readonly Primitive<string> Email = new("Email", Primitive.String)
    {
        Normalizers = [Normalizers.Strings.Trim],
        Validators = [Validators.Create<string>("must contain @", value => value.Contains('@'))],
        Description = "An e-mail address.",
    };

    public static readonly ListPrimitive<string> Features = new("Features", Primitive.String)
    {
        Description = "Feature names.",
    };

    // Rule texts are formatted with the converter, so the documentation says 0xFF, not 255.
    public static readonly Primitive<int> Mask = new("Mask", Primitive.Int32Hex)
    {
        Validators = [Validators.LessThanOrEqual(0xFF)],
        Description = "A bit mask of 8 bits.",
    };

    public static readonly ListPrimitive<Uri> Origins = new("Origins", Primitive.Uri)
    {
        Validators = [Validators.Collections.NotEmpty<Uri>()],
        Description = "Origins, at least one.",
    };

    public static readonly Primitive<int> Port = new("Port", Primitive.Int32)
    {
        Validators = [Validators.InRange(1, 65535)],
        Description = "A TCP port.",
    };

    // Derived: Port's rules run first, then its own. Declared after Port, which it needs when initialized.
    public static readonly Primitive<int> UnprivilegedPort = new("UnprivilegedPort", Port)
    {
        Validators = [Validators.GreaterThanOrEqual(1024)],
        Description = "A TCP port that doesn't need administrator rights.",
    };
}
