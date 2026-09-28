using Leander.Primitives.Parsing;

namespace Leander.Primitives.Internal;

// Holds the one primitive per enum type returned by Primitive.Enum<TEnum>().
internal static class EnumPrimitive<TEnum> where TEnum : struct, Enum
{
    public static readonly Primitive<TEnum> Instance = new(TypeNames.Get(typeof(TEnum)), Converters.Enum<TEnum>());
}
