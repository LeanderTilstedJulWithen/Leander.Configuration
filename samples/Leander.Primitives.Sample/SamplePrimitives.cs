using Leander.Primitives.Normalization;
using Leander.Primitives.Parsing;
using Leander.Primitives.Validation;


namespace Leander.Primitives.Sample;

// Reusable kinds of values: a name, a converter, then normalizers and validators.
public static class SamplePrimitives
{
    // A custom validator. Normalizers run first, so " ops@example.com " is trimmed before it's checked.
    public static readonly Primitive<string> Email = new("Email", Primitive.String)
    {
        Normalizers = [Normalizers.Trim],
        Validators = [Validators.Create<string>("must contain @", value => value.Contains('@'))],
        Description = "An e-mail address.",
    };

    // Rule texts are formatted with the converter, so a Hex primitive says 0xFF, not 255.
    public static readonly Primitive<int> Mask = new("Mask", Primitive.Int32Hex)
    {
        Validators = [Validators.LessThanOrEqual(0xFF)],
        Description = "A bit mask of 8 bits.",
    };

    // A primitive with a custom converter. Normalizers and validators are optional.
    public static readonly Primitive<double> Percent = new("Percent", new PercentageConverter())
    {
        Description = "A percentage.",
    };

    // Derived from a ready-made primitive: it takes its converter and rules, and adds its own.
    public static readonly Primitive<int> Port = new("Port", Primitive.Int32)
    {
        Validators = [Validators.InRange(1, 65535)],
        Description = "A TCP port.",
    };

    // A primitive of our own kind, wrapping another: each end of the range goes through Port.
    public static readonly RangePrimitive<int> PortRange = new("PortRange", Port)
    {
        Description = "A range of ports.",
    };

    // A list: every item goes through the element primitive with all its rules, then the list's own rules run.
    public static readonly ListPrimitive<int> Ports = new("Ports", Port)
    {
        Validators = [Validators.Collections.NotEmpty<int>()],
        Description = "Ports, at least one.",
    };

    // Derived again: Port's rules run first, then these.
    public static readonly Primitive<int> UnprivilegedPort = new("UnprivilegedPort", Port)
    {
        Validators = [Validators.GreaterThanOrEqual(1024)],
        Description = "A TCP port that doesn't need administrator rights.",
    };
}
