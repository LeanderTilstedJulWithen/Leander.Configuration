namespace Leander.Primitives.Validation.Internal;

internal sealed class DelegateValidator<T>(string description, Func<T, bool> isValid) : IValidator<T>
{
    private readonly string _description = description;
    private readonly Func<T, bool> _isValid = isValid;

    public string Description => _description;

    public string? Validate(T value) => _isValid(value) ? null : _description;
}
