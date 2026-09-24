using System.Globalization;
using System.Text.Json;
using Leander.Primitives.Parsing.Internal;

namespace Leander.Primitives.Parsing;

public static class Converters
{
    public static IConverter<string> String { get; } = new StringConverter();

    public static IConverter<bool> Boolean { get; } = new BooleanConverter();

    public static IConverter<byte> Byte { get; } = new ByteConverter();

    public static IConverter<sbyte> SByte { get; } = new SByteConverter();

    public static IConverter<short> Int16 { get; } = new Int16Converter();

    public static IConverter<ushort> UInt16 { get; } = new UInt16Converter();

    public static IConverter<int> Int32 { get; } = new IntConverter();

    public static IConverter<int> Int32Hex { get; } = new HexInt32Converter();

    public static IConverter<uint> UInt32 { get; } = new UInt32Converter();

    public static IConverter<uint> UInt32Hex { get; } = new HexUInt32Converter();

    public static IConverter<long> Int64 { get; } = new Int64Converter();

    public static IConverter<ulong> UInt64 { get; } = new UInt64Converter();

    public static IConverter<float> Single { get; } = new SingleConverter();

    public static IConverter<double> Double { get; } = new DoubleConverter();

    public static IConverter<decimal> Decimal { get; } = new DecimalConverter();

    public static IConverter<Guid> Guid { get; } = new GuidConverter();

    public static IConverter<Uri> Uri { get; } = new UriConverter();

    public static IConverter<TimeSpan> TimeSpan { get; } = new TimeSpanConverterBuilder()
        .AddFormat("c")
        .Build();

    public static IConverter<TEnum> Enum<TEnum>() where TEnum : struct, Enum => EnumConverterCache<TEnum>.Instance;

    public static IConverter<IReadOnlyList<T>> List<T>(IConverter<T> elementConverter, char delimiter = ',') =>
        new ListConverter<T>(elementConverter, delimiter);

    public static IConverter<T> Json<T>(JsonSerializerOptions? options = null) => new JsonValueConverter<T>(options);

    public static IConverter<IReadOnlyDictionary<TKey, TValue>> Dictionary<TKey, TValue>(
        IConverter<TKey> keyConverter,
        IConverter<TValue> valueConverter,
        char entryDelimiter = ',',
        char keyValueDelimiter = '=') where TKey : notnull =>
        new DictionaryConverter<TKey, TValue>(keyConverter, valueConverter, entryDelimiter, keyValueDelimiter);

    public static IConverter<DateTime> DateTimeUtc { get; } = new DateTimeConverterBuilder()
        .AddFormat("yyyy-MM-ddTHH:mm:ssK")
        .AddFormat("yyyy-MM-dd")
        .WithStyles(DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal)
        .Build();

    public static IConverter<DateTime> DateTimeLocal { get; } = new DateTimeConverterBuilder()
        .AddFormat("yyyy-MM-ddTHH:mm:ssK")
        .AddFormat("yyyy-MM-dd")
        .WithStyles(DateTimeStyles.AssumeLocal)
        .Build();

    public static IConverter<DateTimeOffset> DateTimeOffset { get; } = new DateTimeOffsetConverterBuilder()
        .AddFormat("yyyy-MM-ddTHH:mm:ssK")
        .AddFormat("yyyy-MM-dd")
        .WithStyles(DateTimeStyles.AssumeUniversal)
        .Build();
}
