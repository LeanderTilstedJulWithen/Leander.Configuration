using Leander.Primitives.Internal;
using Leander.Primitives.Parsing;

namespace Leander.Primitives;

public abstract class Primitive
{
    private protected Primitive(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public abstract Type ValueType { get; }

    public string? Description { get; init; }

    // The primitive this one was derived from. Null otherwise.
    public abstract Primitive? Base { get; }

    // The descriptions of its own rules, for code that doesn't know T, e.g. descriptors.
    internal abstract IReadOnlyList<string> NormalizerDescriptions { get; }

    internal abstract IReadOnlyList<string> ValidatorDescriptions { get; }

    // Ready-made primitives, named after their type, except the variants. Convenience, not policy.
    public static Primitive<string> String { get; } = new("String", Converters.String);

    public static Primitive<bool> Boolean { get; } = new("Boolean", Converters.Boolean);

    public static Primitive<byte> Byte { get; } = new("Byte", Converters.Byte);

    public static Primitive<sbyte> SByte { get; } = new("SByte", Converters.SByte);

    public static Primitive<short> Int16 { get; } = new("Int16", Converters.Int16);

    public static Primitive<ushort> UInt16 { get; } = new("UInt16", Converters.UInt16);

    public static Primitive<int> Int32 { get; } = new("Int32", Converters.Int32);

    public static Primitive<int> Int32Hex { get; } = new("Hex", Converters.Int32Hex);

    public static Primitive<uint> UInt32 { get; } = new("UInt32", Converters.UInt32);

    public static Primitive<uint> UInt32Hex { get; } = new("Hex", Converters.UInt32Hex);

    public static Primitive<long> Int64 { get; } = new("Int64", Converters.Int64);

    public static Primitive<ulong> UInt64 { get; } = new("UInt64", Converters.UInt64);

    public static Primitive<float> Single { get; } = new("Single", Converters.Single);

    public static Primitive<double> Double { get; } = new("Double", Converters.Double);

    public static Primitive<decimal> Decimal { get; } = new("Decimal", Converters.Decimal);

    public static Primitive<Guid> Guid { get; } = new("Guid", Converters.Guid);

    public static Primitive<Uri> Uri { get; } = new("Uri", Converters.Uri);

    public static Primitive<TimeSpan> TimeSpan { get; } = new("TimeSpan", Converters.TimeSpan);

    // UTC.
    public static Primitive<DateTime> DateTime { get; } = new("DateTime", Converters.DateTimeUtc);

    public static Primitive<DateTime> DateTimeLocal { get; } = new("Local", Converters.DateTimeLocal);

    public static Primitive<DateTimeOffset> DateTimeOffset { get; } = new("DateTimeOffset", Converters.DateTimeOffset);

    // Named after the enum type. The same instance on every call, so keys using it share one primitive.
    public static Primitive<TEnum> Enum<TEnum>() where TEnum : struct, Enum => EnumPrimitive<TEnum>.Instance;

    // The type and name for messages, e.g. "Int32 (Hex)", or just "Int32" when the name is the type name.
    internal string DisplayName => Name == TypeNames.Get(ValueType) ? Name : $"{TypeNames.Get(ValueType)} ({Name})";
}
