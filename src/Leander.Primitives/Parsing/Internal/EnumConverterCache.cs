namespace Leander.Primitives.Parsing.Internal;

internal static class EnumConverterCache<TEnum> where TEnum : struct, Enum
{
    // [Flags] changes how the enum itself behaves (e.g. ToString), so parsing follows it too.
    public static readonly IConverter<TEnum> Instance =
        new EnumConverter<TEnum>(isFlags: typeof(TEnum).IsDefined(typeof(FlagsAttribute), inherit: false));
}
