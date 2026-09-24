namespace Leander.Primitives.Parsing.Internal;

internal static class EnumConverterCache<TEnum> where TEnum : struct, Enum
{
    public static readonly IConverter<TEnum> Instance = new EnumConverter<TEnum>();
}
