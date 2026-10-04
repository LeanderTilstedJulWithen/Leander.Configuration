using Leander.Primitives;
using Leander.Primitives.Normalization;
using Leander.Primitives.Parsing;
using Leander.Primitives.Validation;

public class PercentageConverter : IConverter<double>
{
    public string Description => "A percentage between 0 and 100%.";

    public string Format(double value) => value * 100.0 + "%";

    public bool TryParse(string input, out double result, out IReadOnlyList<IFormattableText<string>> errors)
    {
        errors = [];
        result = default;
        if (input is null) return false;

        input = input.Trim();
        if (input.EndsWith("%", StringComparison.Ordinal))
        {
            input = input[..^1].Trim();
            if (Converters.Double.TryParse(input, out result))
            {
                result /= 100.0;
                if (result >= 0.0 && result <= 1.0) return true;
            }
        }
        else if (Converters.Double.TryParse(input, out result) && result >= 0.0 && result <= 1.0)
        {
            return true;
        }

        result = default;
        return false;
    }
}
