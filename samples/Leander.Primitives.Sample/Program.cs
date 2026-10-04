using Leander.Primitives;
using Leander.Primitives.Sample;

// Leander.Primitives on its own: reading values from strings, with rules, without any configuration.

Console.WriteLine("Ready-made primitives:");
Show(Primitive.Int32, "42");
Show(Primitive.Int32, "forty-two");
Show(Primitive.Boolean, "TRUE");
Show(Primitive.Uri, "https://example.com/path");
Show(Primitive.Uri, "/relative");
Show(Primitive.TimeSpan, "00:00:30");
Show(Primitive.DateTime, "2026-10-04");
Show(Primitive.Enum<DayOfWeek>(), "friday");
Show(Primitive.Enum<DayOfWeek>(), "Caturday");
Console.WriteLine();

Console.WriteLine("Primitives of our own:");
Show(SamplePrimitives.Percent, "0.24");
Show(SamplePrimitives.Percent, "78%");
Show(SamplePrimitives.Email, "  ops@example.com ");
Show(SamplePrimitives.Email, "ops.example.com");
Console.WriteLine();

// UnprivilegedPort is derived from Port, which is derived from Int32: Int32 reads the text, then Port's rules
// run, then UnprivilegedPort's.
Console.WriteLine("Derived primitives:");
Show(SamplePrimitives.Port, "80");
Show(SamplePrimitives.UnprivilegedPort, "80");
Show(SamplePrimitives.UnprivilegedPort, "8080");
Show(SamplePrimitives.UnprivilegedPort, "70000");
Console.WriteLine();

// Errors name the item, counted from 0.
Console.WriteLine("List primitives:");
Show(SamplePrimitives.Ports, "80, 443, 8080");
Show(SamplePrimitives.Ports, "80, 0, http");
Show(SamplePrimitives.Ports, "");
Console.WriteLine();

// TryParse reads text. TryAccept takes a value that is already typed, e.g. a default in code, and applies the
// same rules.
Console.WriteLine("Parsing and accepting:");
if (SamplePrimitives.Port.TryParse("8080", out var port))
{
    Console.WriteLine($"  TryParse(\"8080\")   {port}");
}

if (!SamplePrimitives.Port.TryAccept(0, out _, out var errors))
{
    Console.WriteLine($"  TryAccept(0)       {string.Join("; ", errors)}");
}

Console.WriteLine();

// The converter formats values back, in the same form it reads. Rule texts use it too.
Console.WriteLine("Formatting:");
Show(Primitive.Int32Hex, "ff");
Show(SamplePrimitives.Mask, "0x1FF");
ShowFormatted(Primitive.TimeSpan, "90 minutes", TimeSpan.FromMinutes(90));
ShowFormatted(Primitive.DateTime, "noon, 4 October 2026", new DateTime(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc));
ShowFormatted(SamplePrimitives.Ports, "[80, 443]", [80, 443]);
Console.WriteLine();

// A primitive of a new kind. Each end goes through Port, then the range must be in order.
Console.WriteLine("A primitive wrapping another:");
Show(SamplePrimitives.PortRange, "1024..2048");
Show(SamplePrimitives.PortRange, "2048..1024");
Show(SamplePrimitives.PortRange, "1024..70000");
Console.WriteLine($"  {"Description",-17} {SamplePrimitives.PortRange.Converter.Description}");

// Parses the input and prints the value, formatted back by the converter, or every error.
static void Show<T>(Primitive<T> primitive, string input)
{
    var result = primitive.TryParse(input, out var value, out var errors)
        ? $"-> {primitive.Converter.Format(value)}"
        : $"error: {string.Join("; ", errors)}";
    Console.WriteLine($"  {primitive.Name,-17} {$"\"{input}\"",-26} {result}");
}

// Prints a typed value as the converter formats it, next to a label saying what the value is.
static void ShowFormatted<T>(Primitive<T> primitive, string label, T value) =>
    Console.WriteLine($"  {primitive.Name,-17} {label,-26} -> {primitive.Converter.Format(value)}");
