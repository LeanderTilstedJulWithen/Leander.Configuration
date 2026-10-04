namespace Leander.Primitives.Validation.Internal;

// One failure: the description.
internal sealed class DelegateValidator<T>(IFormattableText<T> description, Func<T, bool> isValid) : IValidator<T>
{
    private readonly IReadOnlyList<IFormattableText<T>> _failure = Array.AsReadOnly([description]);
    private readonly Func<T, bool> _isValid = isValid;

    public IFormattableText<T> Description { get; } = description;

    public bool IsValid(T value) => _isValid(value);

    public IReadOnlyList<IFormattableText<T>> Validate(T value) => _isValid(value) ? [] : _failure;
}
