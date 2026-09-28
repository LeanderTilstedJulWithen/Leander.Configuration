using Leander.Primitives;
using Leander.Primitives.Normalization;
using Leander.Primitives.Parsing;
using Leander.Primitives.Validation;

namespace Leander.Configuration.Sample;

// Reusable kinds of values. The rules live here, not on the configuration keys that use them.
public static class SamplePrimitives
{
    public static readonly Primitive<string> Email =
        Primitive.Create("Email", Converters.String)
            .Normalize(Normalizers.Trim)
            .Validate(Validators.Create<string>("must contain @", value => value.Contains('@')))
            .Describe("An e-mail address.");

    public static readonly Primitive<int> Port =
        Primitive.Create("Port", Converters.Int32)
            .Validate(Validators.InRange(1, 65535))
            .Describe("A TCP port.");
}
