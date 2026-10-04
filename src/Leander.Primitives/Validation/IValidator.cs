namespace Leander.Primitives.Validation;

/// <summary>
/// A rule a value must follow, checked after normalization.
/// </summary>
/// <remarks>
/// Invariant, so its texts can hold values of <typeparamref name="T"/>, e.g. a bound, for the primitive to format.
/// A rule for many types is a generic method returning an exactly typed instance,
/// e.g. <see cref="Validators.Collections.NotEmpty{T}"/>.
/// </remarks>
public interface IValidator<T>
{
    /// <summary>
    /// What the rule requires, in one line, e.g. for documentation.
    /// </summary>
    public IFormattableText<T> Description { get; }

    /// <summary>
    /// Whether the value is valid. Override only for speed, e.g. when building the failures is expensive.
    /// </summary>
    public bool IsValid(T value) => Validate(value).Count == 0;

    /// <summary>
    /// Every failure of the value; empty when it is valid.
    /// </summary>
    // A list, not a lazy sequence, so the rule runs inside the call.
    public IReadOnlyList<IFormattableText<T>> Validate(T value);
}
