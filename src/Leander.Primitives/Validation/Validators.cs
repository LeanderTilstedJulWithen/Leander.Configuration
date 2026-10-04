using Leander.Primitives.Validation.Internal;

namespace Leander.Primitives.Validation;

/// <summary>
/// Ready-made validators, and <see cref="Create{T}(string, Func{T, bool})"/> for your own.
/// </summary>
/// <remarks>
/// <para>
/// Validators for any comparable type are here; validators for one kind of value are in a nested class named after it,
/// e.g. <see cref="Strings"/> and <see cref="Collections"/>.
/// </para>
/// <para>
/// Each of these reports one failure: its description. A custom <see cref="IValidator{T}"/> may report several.
/// Bounds are formatted by the primitive's converter.
/// </para>
/// </remarks>
public static class Validators
{
    /// <summary>
    /// Creates a validator from a description and a check. The description is also the failure.
    /// </summary>
    public static IValidator<T> Create<T>(string description, Func<T, bool> isValid) =>
        Create(FormattableText.Create<T>(description), isValid);

    /// <summary>
    /// Creates a validator from a description with values of <typeparamref name="T"/> and a check.
    /// The description is also the failure.
    /// </summary>
    /// <example>
    /// <code>FormattableText.Create&lt;int&gt;(formatter => $"must be at most {formatter.Format(255)}")</code>
    /// </example>
    public static IValidator<T> Create<T>(IFormattableText<T> description, Func<T, bool> isValid) =>
        new DelegateValidator<T>(description, isValid);

    /// <summary>
    /// The value must be greater than <paramref name="bound"/>.
    /// </summary>
    public static IValidator<T> GreaterThan<T>(T bound) where T : IComparable<T> =>
        Create(FormattableText.Create<T>(formatter => $"must be greater than {formatter.Format(bound)}"), value => value.CompareTo(bound) > 0);

    /// <summary>
    /// The value must be greater than or equal to <paramref name="bound"/>.
    /// </summary>
    public static IValidator<T> GreaterThanOrEqual<T>(T bound) where T : IComparable<T> =>
        Create(FormattableText.Create<T>(formatter => $"must be greater than or equal to {formatter.Format(bound)}"), value => value.CompareTo(bound) >= 0);

    /// <summary>
    /// The value must be between <paramref name="minimum"/> and <paramref name="maximum"/>, both included.
    /// </summary>
    public static IValidator<T> InRange<T>(T minimum, T maximum) where T : IComparable<T> =>
        Create(
            FormattableText.Create<T>(formatter => $"must be between {formatter.Format(minimum)} and {formatter.Format(maximum)}"),
            value => value.CompareTo(minimum) >= 0 && value.CompareTo(maximum) <= 0);

    /// <summary>
    /// The value must be less than <paramref name="bound"/>.
    /// </summary>
    public static IValidator<T> LessThan<T>(T bound) where T : IComparable<T> =>
        Create(FormattableText.Create<T>(formatter => $"must be less than {formatter.Format(bound)}"), value => value.CompareTo(bound) < 0);

    /// <summary>
    /// The value must be less than or equal to <paramref name="bound"/>.
    /// </summary>
    public static IValidator<T> LessThanOrEqual<T>(T bound) where T : IComparable<T> =>
        Create(FormattableText.Create<T>(formatter => $"must be less than or equal to {formatter.Format(bound)}"), value => value.CompareTo(bound) <= 0);

    /// <summary>
    /// Validators for lists, e.g. the rules of a <see cref="ListPrimitive{T}"/>.
    /// </summary>
    /// <remarks>
    /// Validators are invariant, so each takes the item type: <c>Validators.Collections.NotEmpty&lt;Uri&gt;()</c>.
    /// </remarks>
    public static class Collections
    {
        /// <summary>
        /// The list must have at least one item.
        /// </summary>
        public static IValidator<IReadOnlyList<T>> NotEmpty<T>() =>
            Create<IReadOnlyList<T>>("must not be empty", list => list.Count > 0);
    }

    /// <summary>
    /// Validators for strings.
    /// </summary>
    public static class Strings
    {
        /// <summary>
        /// The string must not be empty.
        /// </summary>
        public static IValidator<string> NotEmpty { get; } = Create<string>("must not be empty", value => !string.IsNullOrEmpty(value));
    }
}
