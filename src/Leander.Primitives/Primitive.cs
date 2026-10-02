using Leander.Primitives.Internal;
using Leander.Primitives.Parsing;

namespace Leander.Primitives;

/// <summary>
/// A named kind of value: how it is read from a string, and the rules it must follow.
/// See <see cref="Primitive{T}"/>.
/// </summary>
/// <remarks>
/// The static members are ready-made primitives, named after their type, except the variants.
/// They are a convenience, not policy: derive your own from them or create new ones.
/// </remarks>
public abstract class Primitive
{
    private protected Primitive(string name)
    {
        Name = name;
    }

    /// <summary>
    /// The name, used in messages and documentation.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The type of the values.
    /// </summary>
    public abstract Type ValueType { get; }

    /// <summary>
    /// What the values are, for documentation. Optional.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The primitive this one was derived from, or <see langword="null"/>.
    /// </summary>
    public abstract Primitive? Base { get; }

    // The descriptions of its converter and its own rules, for code that doesn't know T, e.g. descriptors.
    internal abstract string? ConverterDescription { get; }

    internal abstract IReadOnlyList<string> NormalizerDescriptions { get; }

    internal abstract IReadOnlyList<string> ValidatorDescriptions { get; }

    /// <summary>
    /// Any string, as it is.
    /// </summary>
    public static Primitive<string> String { get; } = new("String", Converters.String);

    /// <summary>
    /// <c>true</c> or <c>false</c>, ignoring case.
    /// </summary>
    public static Primitive<bool> Boolean { get; } = new("Boolean", Converters.Boolean);

    /// <summary>
    /// A <see cref="byte"/> in decimal.
    /// </summary>
    public static Primitive<byte> Byte { get; } = new("Byte", Converters.Byte);

    /// <summary>
    /// An <see cref="sbyte"/> in decimal.
    /// </summary>
    public static Primitive<sbyte> SByte { get; } = new("SByte", Converters.SByte);

    /// <summary>
    /// A <see cref="short"/> in decimal.
    /// </summary>
    public static Primitive<short> Int16 { get; } = new("Int16", Converters.Int16);

    /// <summary>
    /// A <see cref="ushort"/> in decimal.
    /// </summary>
    public static Primitive<ushort> UInt16 { get; } = new("UInt16", Converters.UInt16);

    /// <summary>
    /// An <see cref="int"/> in decimal.
    /// </summary>
    public static Primitive<int> Int32 { get; } = new("Int32", Converters.Int32);

    /// <summary>
    /// An <see cref="int"/> in hexadecimal, named "Hex". See <see cref="Converters.Int32Hex"/>.
    /// </summary>
    public static Primitive<int> Int32Hex { get; } = new("Hex", Converters.Int32Hex);

    /// <summary>
    /// A <see cref="uint"/> in decimal.
    /// </summary>
    public static Primitive<uint> UInt32 { get; } = new("UInt32", Converters.UInt32);

    /// <summary>
    /// A <see cref="uint"/> in hexadecimal, named "Hex". See <see cref="Converters.UInt32Hex"/>.
    /// </summary>
    public static Primitive<uint> UInt32Hex { get; } = new("Hex", Converters.UInt32Hex);

    /// <summary>
    /// A <see cref="long"/> in decimal.
    /// </summary>
    public static Primitive<long> Int64 { get; } = new("Int64", Converters.Int64);

    /// <summary>
    /// A <see cref="ulong"/> in decimal.
    /// </summary>
    public static Primitive<ulong> UInt64 { get; } = new("UInt64", Converters.UInt64);

    /// <summary>
    /// A <see cref="float"/>.
    /// </summary>
    public static Primitive<float> Single { get; } = new("Single", Converters.Single);

    /// <summary>
    /// A <see cref="double"/>.
    /// </summary>
    public static Primitive<double> Double { get; } = new("Double", Converters.Double);

    /// <summary>
    /// A <see cref="decimal"/>.
    /// </summary>
    public static Primitive<decimal> Decimal { get; } = new("Decimal", Converters.Decimal);

    /// <summary>
    /// A <see cref="System.Guid"/>.
    /// </summary>
    public static Primitive<Guid> Guid { get; } = new("Guid", Converters.Guid);

    /// <summary>
    /// An absolute <see cref="System.Uri"/>.
    /// </summary>
    public static Primitive<Uri> Uri { get; } = new("Uri", Converters.Uri);

    /// <summary>
    /// A <see cref="System.TimeSpan"/>. See <see cref="Converters.TimeSpan"/>.
    /// </summary>
    public static Primitive<TimeSpan> TimeSpan { get; } = new("TimeSpan", Converters.TimeSpan);

    /// <summary>
    /// A <see cref="System.DateTime"/> in UTC. See <see cref="Converters.DateTimeUtc"/>.
    /// </summary>
    public static Primitive<DateTime> DateTime { get; } = new("DateTime", Converters.DateTimeUtc);

    /// <summary>
    /// A local <see cref="System.DateTime"/>, named "Local". See <see cref="Converters.DateTimeLocal"/>.
    /// </summary>
    public static Primitive<DateTime> DateTimeLocal { get; } = new("Local", Converters.DateTimeLocal);

    /// <summary>
    /// A <see cref="System.DateTimeOffset"/>. See <see cref="Converters.DateTimeOffset"/>.
    /// </summary>
    public static Primitive<DateTimeOffset> DateTimeOffset { get; } = new("DateTimeOffset", Converters.DateTimeOffset);

    /// <summary>
    /// A defined value of <typeparamref name="TEnum"/>, named after the enum type. See <see cref="Converters.Enum{TEnum}"/>.
    /// </summary>
    /// <remarks>
    /// Returns the same instance on every call, so keys using it share one primitive.
    /// </remarks>
    public static Primitive<TEnum> Enum<TEnum>() where TEnum : struct, Enum => EnumPrimitive<TEnum>.Instance;

    // The type and name for messages, e.g. "Int32 (Hex)", or just "Int32" when the name is the type name.
    internal string DisplayName => Name == TypeNames.Get(ValueType) ? Name : $"{TypeNames.Get(ValueType)} ({Name})";
}
