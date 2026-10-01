using Leander.Primitives.Validation.Internal;

namespace Leander.Primitives.Validation;

// Every rule here reports one failure, its description. That is the library's habit, not the interface's rule:
// a custom IValidator<T> may report several.
public static class Validators
{
    public static IValidator<string> NotEmpty { get; } = Create<string>("must not be empty", value => !string.IsNullOrEmpty(value));

    public static IValidator<T> Create<T>(string description, Func<T, bool> isValid) =>
        Create(FormattableText.Create<T>(description), isValid);

    // For a description with values of T: FormattableText.Create<int>(formatter => $"must be at most {formatter.Format(255)}").
    public static IValidator<T> Create<T>(IFormattableText<T> description, Func<T, bool> isValid) =>
        new DelegateValidator<T>(description, isValid);

    // Bounds are formatted by the primitive's converter.
    public static IValidator<T> GreaterThan<T>(T bound) where T : IComparable<T> =>
        Create(FormattableText.Create<T>(formatter => $"must be greater than {formatter.Format(bound)}"), value => value.CompareTo(bound) > 0);

    public static IValidator<T> GreaterThanOrEqual<T>(T bound) where T : IComparable<T> =>
        Create(FormattableText.Create<T>(formatter => $"must be greater than or equal to {formatter.Format(bound)}"), value => value.CompareTo(bound) >= 0);

    public static IValidator<T> LessThan<T>(T bound) where T : IComparable<T> =>
        Create(FormattableText.Create<T>(formatter => $"must be less than {formatter.Format(bound)}"), value => value.CompareTo(bound) < 0);

    public static IValidator<T> LessThanOrEqual<T>(T bound) where T : IComparable<T> =>
        Create(FormattableText.Create<T>(formatter => $"must be less than or equal to {formatter.Format(bound)}"), value => value.CompareTo(bound) <= 0);

    public static IValidator<T> InRange<T>(T minimum, T maximum) where T : IComparable<T> =>
        Create(
            FormattableText.Create<T>(formatter => $"must be between {formatter.Format(minimum)} and {formatter.Format(maximum)}"),
            value => value.CompareTo(minimum) >= 0 && value.CompareTo(maximum) <= 0);

    // For lists, e.g. the rules of a ListPrimitive<T>. Validators are invariant, so each takes the item type:
    // Validators.Collections.NotEmpty<Uri>().
    public static class Collections
    {
        public static IValidator<IReadOnlyList<T>> NotEmpty<T>() =>
            Create<IReadOnlyList<T>>("must not be empty", list => list.Count > 0);
    }
}
