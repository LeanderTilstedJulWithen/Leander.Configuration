namespace Leander.Primitives.Validation.Internal;

// One failure: the description.
internal sealed class DelegateValidator<T>(IFormattableText<T> description, Func<T, bool> isValid) : IValidator<T>
{
    private readonly Func<T, bool> _isValid = isValid;
    private readonly IReadOnlyList<IFormattableText<T>> _failure = Array.AsReadOnly([description]);

    public IFormattableText<T> Description { get; } = description;

    public IReadOnlyList<IFormattableText<T>> Validate(T value) => _isValid(value) ? [] : _failure;

    public bool IsValid(T value) => _isValid(value);
}
