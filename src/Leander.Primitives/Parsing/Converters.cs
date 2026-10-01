using System.Globalization;
using System.Text.Json;
using Leander.Primitives.Parsing.Internal;

namespace Leander.Primitives.Parsing;

/// <summary>
/// Ready-made converters. Numbers, dates and times use the invariant culture.
/// </summary>
public static class Converters
{
    /// <summary>
    /// Takes the input as it is.
    /// </summary>
    public static IConverter<string> String { get; } = new StringConverter();

    /// <summary>
    /// <c>true</c> or <c>false</c>, ignoring case.
    /// </summary>
    public static IConverter<bool> Boolean { get; } = new BooleanConverter();

    /// <summary>
    /// A <see cref="byte"/> in decimal.
    /// </summary>
    public static IConverter<byte> Byte { get; } = new ByteConverter();

    /// <summary>
    /// An <see cref="sbyte"/> in decimal.
    /// </summary>
    public static IConverter<sbyte> SByte { get; } = new SByteConverter();

    /// <summary>
    /// A <see cref="short"/> in decimal.
    /// </summary>
    public static IConverter<short> Int16 { get; } = new Int16Converter();

    /// <summary>
    /// A <see cref="ushort"/> in decimal.
    /// </summary>
    public static IConverter<ushort> UInt16 { get; } = new UInt16Converter();

    /// <summary>
    /// An <see cref="int"/> in decimal.
    /// </summary>
    public static IConverter<int> Int32 { get; } = new IntConverter();

    /// <summary>
    /// An <see cref="int"/> in hexadecimal, with or without <c>0x</c>; formatted with <c>0x</c>.
    /// </summary>
    public static IConverter<int> Int32Hex { get; } = new HexInt32Converter();

    /// <summary>
    /// A <see cref="uint"/> in decimal.
    /// </summary>
    public static IConverter<uint> UInt32 { get; } = new UInt32Converter();

    /// <summary>
    /// A <see cref="uint"/> in hexadecimal, with or without <c>0x</c>; formatted with <c>0x</c>.
    /// </summary>
    public static IConverter<uint> UInt32Hex { get; } = new HexUInt32Converter();

    /// <summary>
    /// A <see cref="long"/> in decimal.
    /// </summary>
    public static IConverter<long> Int64 { get; } = new Int64Converter();

    /// <summary>
    /// A <see cref="ulong"/> in decimal.
    /// </summary>
    public static IConverter<ulong> UInt64 { get; } = new UInt64Converter();

    /// <summary>
    /// A <see cref="float"/>.
    /// </summary>
    public static IConverter<float> Single { get; } = new SingleConverter();

    /// <summary>
    /// A <see cref="double"/>.
    /// </summary>
    public static IConverter<double> Double { get; } = new DoubleConverter();

    /// <summary>
    /// A <see cref="decimal"/>.
    /// </summary>
    public static IConverter<decimal> Decimal { get; } = new DecimalConverter();

    /// <summary>
    /// A <see cref="System.Guid"/>.
    /// </summary>
    public static IConverter<Guid> Guid { get; } = new GuidConverter();

    /// <summary>
    /// An absolute <see cref="System.Uri"/>.
    /// </summary>
    public static IConverter<Uri> Uri { get; } = new UriConverter();

    /// <summary>
    /// A <see cref="System.TimeSpan"/> in the constant format <c>[-][d.]hh:mm:ss[.fffffff]</c>.
    /// </summary>
    public static IConverter<TimeSpan> TimeSpan { get; } = new TimeSpanConverterBuilder()
        .AddFormat("c")
        .Build();

    /// <summary>
    /// A defined value of <typeparamref name="TEnum"/> by name or number, ignoring case.
    /// </summary>
    /// <remarks>
    /// For a <see cref="FlagsAttribute"/> enum, comma-separated names are combined; a combination must only use defined flags.
    /// </remarks>
    // [Flags] changes how the enum itself behaves (e.g. ToString), so parsing follows it too.
    public static IConverter<TEnum> Enum<TEnum>() where TEnum : struct, Enum =>
        new EnumConverter<TEnum>(isFlags: typeof(TEnum).IsDefined(typeof(FlagsAttribute), inherit: false));

    /// <summary>
    /// A list split at <paramref name="delimiter"/>, each item trimmed and parsed by <paramref name="elementConverter"/>.
    /// Empty input is an empty list.
    /// </summary>
    /// <remarks>
    /// Applies no rules to the items; <see cref="ListPrimitive{T}"/> reads items through a primitive with its rules.
    /// </remarks>
    public static IConverter<IReadOnlyList<T>> List<T>(IConverter<T> elementConverter, char delimiter = ',') =>
        new ListConverter<T>(elementConverter, delimiter);

    /// <summary>
    /// A value read from JSON with <see cref="JsonSerializer"/>.
    /// </summary>
    public static IConverter<T> Json<T>(JsonSerializerOptions? options = null) => new JsonValueConverter<T>(options);

    /// <summary>
    /// A dictionary of entries split at <paramref name="entryDelimiter"/>, each a key and value split at the first
    /// <paramref name="keyValueDelimiter"/>, e.g. <c>a=1,b=2</c>. Keys and values are trimmed; a duplicate key fails.
    /// Empty input is an empty dictionary.
    /// </summary>
    public static IConverter<IReadOnlyDictionary<TKey, TValue>> Dictionary<TKey, TValue>(
        IConverter<TKey> keyConverter,
        IConverter<TValue> valueConverter,
        char entryDelimiter = ',',
        char keyValueDelimiter = '=') where TKey : notnull =>
        new DictionaryConverter<TKey, TValue>(keyConverter, valueConverter, entryDelimiter, keyValueDelimiter);

    /// <summary>
    /// A <see cref="System.DateTime"/> in UTC, as <c>yyyy-MM-ddTHH:mm:ssK</c> or <c>yyyy-MM-dd</c>.
    /// Values without an offset are taken as UTC.
    /// </summary>
    public static IConverter<DateTime> DateTimeUtc { get; } = new DateTimeConverterBuilder()
        .AddFormat("yyyy-MM-ddTHH:mm:ssK")
        .AddFormat("yyyy-MM-dd")
        .WithStyles(DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal)
        .Build();

    /// <summary>
    /// A local <see cref="System.DateTime"/>, as <c>yyyy-MM-ddTHH:mm:ssK</c> or <c>yyyy-MM-dd</c>.
    /// Values without an offset are taken as local time.
    /// </summary>
    public static IConverter<DateTime> DateTimeLocal { get; } = new DateTimeConverterBuilder()
        .AddFormat("yyyy-MM-ddTHH:mm:ssK")
        .AddFormat("yyyy-MM-dd")
        .WithStyles(DateTimeStyles.AssumeLocal)
        .Build();

    /// <summary>
    /// A <see cref="System.DateTimeOffset"/>, as <c>yyyy-MM-ddTHH:mm:ssK</c> or <c>yyyy-MM-dd</c>.
    /// Values without an offset are taken as UTC.
    /// </summary>
    public static IConverter<DateTimeOffset> DateTimeOffset { get; } = new DateTimeOffsetConverterBuilder()
        .AddFormat("yyyy-MM-ddTHH:mm:ssK")
        .AddFormat("yyyy-MM-dd")
        .WithStyles(DateTimeStyles.AssumeUniversal)
        .Build();
}
