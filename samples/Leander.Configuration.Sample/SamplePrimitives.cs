using Leander.Primitives;
using Leander.Primitives.Normalization;
using Leander.Primitives.Validation;

namespace Leander.Configuration.Sample;

// Reusable kinds of values. The rules live here, not on the configuration keys that use them.
public static class SamplePrimitives
{
    public static readonly PrimitiveDefinition<string> Email =
        PrimitiveDefinition.Define<string>("Email")
            .Normalize(Normalizers.Trim)
            .Validate(Validators.Create<string>("must contain @", value => value.Contains('@')))
            .Describe("An e-mail address.");

    public static readonly PrimitiveDefinition<int> Port =
        PrimitiveDefinition.Define<int>("Port")
            .Validate(Validators.InRange(1, 65535))
            .Describe("A TCP port.");
}
