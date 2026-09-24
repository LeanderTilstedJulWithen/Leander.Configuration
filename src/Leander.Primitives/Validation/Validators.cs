using System.Collections;
using Leander.Primitives.Validation.Internal;

namespace Leander.Primitives.Validation;

public static class Validators
{
    public static IValidator<string> NotEmpty { get; } = Create<string>("must not be empty", value => !string.IsNullOrEmpty(value));

    public static IValidator<T> Create<T>(string description, Func<T, bool> isValid) =>
        new DelegateValidator<T>(description, isValid);

    public static IValidator<T> GreaterThan<T>(T bound) where T : IComparable<T> =>
        Create<T>($"must be greater than {bound}", value => value.CompareTo(bound) > 0);

    public static IValidator<T> GreaterThanOrEqual<T>(T bound) where T : IComparable<T> =>
        Create<T>($"must be greater than or equal to {bound}", value => value.CompareTo(bound) >= 0);

    public static IValidator<T> LessThan<T>(T bound) where T : IComparable<T> =>
        Create<T>($"must be less than {bound}", value => value.CompareTo(bound) < 0);

    public static IValidator<T> LessThanOrEqual<T>(T bound) where T : IComparable<T> =>
        Create<T>($"must be less than or equal to {bound}", value => value.CompareTo(bound) <= 0);

    public static IValidator<T> InRange<T>(T minimum, T maximum) where T : IComparable<T> =>
        Create<T>($"must be between {minimum} and {maximum}", value => value.CompareTo(minimum) >= 0 && value.CompareTo(maximum) <= 0);

    public static class Collections
    {
        // Contravariance lets this apply to any collection, e.g. IReadOnlyList<string>.
        public static IValidator<IEnumerable> NotEmpty { get; } = Create<IEnumerable>("must not be empty", HasElements);

        private static bool HasElements(IEnumerable values)
        {
            if (values is IList list)
            {
                return list.Count > 0;
            }

            var enumerator = values.GetEnumerator();
            try
            {
                return enumerator.MoveNext();
            }
            finally
            {
                (enumerator as IDisposable)?.Dispose();
            }
        }
    }
}
