using Leander.Primitives;
using Leander.Primitives.Parsing;
using Leander.Primitives.Validation;

namespace Leander.Configuration.Generators.Sample;

// Reusable kinds of values. The rules live here, not on the configuration keys that use them.
public static class SamplePrimitives
{
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

    public static readonly Primitive<int> Seconds = new("Seconds", Converters.Int32)
    {
        Validators = [Validators.GreaterThan(0)],
        Description = "A number of seconds.",
    };
}
