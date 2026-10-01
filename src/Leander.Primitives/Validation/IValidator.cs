namespace Leander.Primitives.Validation;

// Invariant, so its texts can hold values of T, e.g. a bound, for the primitive to format.
// A rule for many types is a generic method returning an exactly typed instance, e.g. Validators.Collections.NotEmpty<T>().
public interface IValidator<T>
{
    // What the rule requires, in one line, e.g. for documentation.
    public IFormattableText<T> Description { get; }

    // Every failure of the value; empty when it is valid. A list, not a lazy sequence, so the rule runs inside the call.
    public IReadOnlyList<IFormattableText<T>> Validate(T value);

    // Override only for speed, e.g. when building the failures is expensive.
    public bool IsValid(T value) => Validate(value).Count == 0;
}
