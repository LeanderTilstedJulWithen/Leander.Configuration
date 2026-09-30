using Leander.Primitives;
using Leander.Primitives.Normalization;
using Leander.Primitives.Parsing;
using Leander.Primitives.Validation;

namespace Leander.Configuration.Sample;

// Reusable kinds of values. The rules live here, not on the configuration keys that use them.
public static class SamplePrimitives
{
    public static readonly Primitive<string> Email = new("Email", Converters.String)
    {
        Normalizers = [Normalizers.Trim],
        Validators = [Validators.Create<string>("must contain @", value => value.Contains('@'))],
        Description = "An e-mail address.",
    };

    public static readonly Primitive<int> Port = new("Port", Converters.Int32)
    {
        Validators = [Validators.InRange(1, 65535)],
        Description = "A TCP port.",
    };

    // Derived from the ready-made Int32: it takes its converter and rules, and adds its own.
    public static readonly Primitive<int> ConnectionLimit = new("ConnectionLimit", Primitive.Int32)
    {
        Validators = [Validators.GreaterThan(0)],
        Description = "A maximum number of connections.",
    };

    // A list is a primitive too: its rules are on the list, the item rules on the element.
    public static readonly ListPrimitive<Uri> Origins = new("Origins", Primitive.Uri)
    {
        Validators = [Validators.Collections.NotEmpty],
        Description = "Origins, at least one.",
    };

    public static readonly ListPrimitive<string> Features = new("Features", Primitive.String)
    {
        Description = "Comma-separated feature names.",
    };
}
