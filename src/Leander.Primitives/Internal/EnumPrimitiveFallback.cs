using System.Reflection;
using Leander.Primitives.Parsing;
using Leander.Primitives.Parsing.Internal;

namespace Leander.Primitives.Internal;

internal sealed class EnumPrimitiveFallback : IPrimitiveFallback
{
    public PrimitiveDefinition<T>? Define<T>()
    {
        if (!typeof(T).IsEnum)
        {
            return null;
        }

        // Converters.Enum<TEnum>() is constrained to enums, which T is not, so the cached converter is fetched by reflection.
        var cacheType = typeof(EnumConverterCache<>).MakeGenericType(typeof(T));
        var field = cacheType.GetField(nameof(EnumConverterCache<DayOfWeek>.Instance), BindingFlags.Public | BindingFlags.Static)!;
        return PrimitiveDefinition.Define((IConverter<T>)field.GetValue(null)!);
    }
}
