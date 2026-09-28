using Leander.Primitives.Internal;
using Leander.Primitives.Parsing;

namespace Leander.Primitives;

public abstract class Primitive
{
    private protected Primitive()
    {
    }

    // Null for an unnamed primitive.
    public abstract string? Name { get; }

    public abstract Type ValueType { get; }

    public abstract string? Description { get; }

    // The primitive this one was derived from with DeriveFrom. Null otherwise.
    public abstract Primitive? Base { get; }

    // Ready-made primitives: unnamed, except the variants. Convenience, not policy.
    public static Primitive<string> String { get; } = Create(Converters.String);

    public static Primitive<bool> Boolean { get; } = Create(Converters.Boolean);

    public static Primitive<byte> Byte { get; } = Create(Converters.Byte);

    public static Primitive<sbyte> SByte { get; } = Create(Converters.SByte);

    public static Primitive<short> Int16 { get; } = Create(Converters.Int16);

    public static Primitive<ushort> UInt16 { get; } = Create(Converters.UInt16);

    public static Primitive<int> Int32 { get; } = Create(Converters.Int32);

    public static Primitive<int> Int32Hex { get; } = Create("Hex", Converters.Int32Hex);

    public static Primitive<uint> UInt32 { get; } = Create(Converters.UInt32);

    public static Primitive<uint> UInt32Hex { get; } = Create("Hex", Converters.UInt32Hex);

    public static Primitive<long> Int64 { get; } = Create(Converters.Int64);

    public static Primitive<ulong> UInt64 { get; } = Create(Converters.UInt64);

    public static Primitive<float> Single { get; } = Create(Converters.Single);

    public static Primitive<double> Double { get; } = Create(Converters.Double);

    public static Primitive<decimal> Decimal { get; } = Create(Converters.Decimal);

    public static Primitive<Guid> Guid { get; } = Create(Converters.Guid);

    public static Primitive<Uri> Uri { get; } = Create(Converters.Uri);

    public static Primitive<TimeSpan> TimeSpan { get; } = Create(Converters.TimeSpan);

    // UTC.
    public static Primitive<DateTime> DateTime { get; } = Create(Converters.DateTimeUtc);

    public static Primitive<DateTime> DateTimeLocal { get; } = Create("Local", Converters.DateTimeLocal);

    public static Primitive<DateTimeOffset> DateTimeOffset { get; } = Create(Converters.DateTimeOffset);

    // A new instance on every call: keep it in a field to share it.
    public static Primitive<TEnum> Enum<TEnum>() where TEnum : struct, Enum => Create(Converters.Enum<TEnum>());

    public static Primitive<T> Create<T>(IConverter<T> converter) =>
        new(name: null, description: null, converter, [], [], @base: null);

    public static Primitive<T> Create<T>(string name, IConverter<T> converter) =>
        new(name, description: null, converter, [], [], @base: null);

    // Keeps the base's converter and rules; rules added afterwards are appended. The description is not inherited.
    public static Primitive<T> DeriveFrom<T>(string name, Primitive<T> @base) =>
        new(name, description: null, @base.Converter, @base.Normalizers, @base.Validators, @base);

    // The type and name for messages, e.g. "Int32 (Hex)".
    internal string DisplayName => Name is null ? TypeNames.Get(ValueType) : $"{TypeNames.Get(ValueType)} ({Name})";
}
