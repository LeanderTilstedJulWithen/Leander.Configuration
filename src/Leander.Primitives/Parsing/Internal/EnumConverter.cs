namespace Leander.Primitives.Parsing.Internal;

internal sealed class EnumConverter<TEnum> : IConverter<TEnum> where TEnum : struct, Enum
{
    public bool TryParse(string input, out TEnum result)
    {
        if (!Enum.TryParse(input, ignoreCase: true, out result))
        {
            return false;
        }

        return typeof(TEnum).IsDefined(typeof(FlagsAttribute), inherit: false)
            ? IsValidFlagsCombination(result)
            : Enum.IsDefined(result);
    }

    public string Format(TEnum value) => value.ToString();

    private static bool IsValidFlagsCombination(TEnum value)
    {
        var bits = Convert.ToUInt64(value);

        var allFlags = 0UL;
        foreach (var flag in Enum.GetValues<TEnum>())
        {
            allFlags |= Convert.ToUInt64(flag);
        }

        return (bits & ~allFlags) == 0;
    }
}
