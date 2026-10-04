namespace Leander.Primitives.Parsing.Internal;

internal sealed class EnumConverter<TEnum>(bool isFlags) : IConverter<TEnum> where TEnum : struct, Enum
{
    private readonly bool _isFlags = isFlags;

    public string Description => _isFlags
        ? "One or more of the names, separated by commas, or a number, ignoring case."
        : "One of the names, or its number, ignoring case.";

    public string Format(TEnum value) => value.ToString();

    public bool TryParse(string input, out TEnum result)
    {
        // Enum.TryParse ORs comma-separated names together, which only makes sense for flags.
        if (!_isFlags && input.Contains(','))
        {
            result = default;
            return false;
        }

        if (!Enum.TryParse(input, ignoreCase: true, out result))
        {
            return false;
        }

        return _isFlags ? IsValidFlagsCombination(result) : Enum.IsDefined(result);
    }

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
