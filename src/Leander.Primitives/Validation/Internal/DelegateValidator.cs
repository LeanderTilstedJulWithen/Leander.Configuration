namespace Leander.Primitives.Validation.Internal;

internal sealed class DelegateValidator<T>(string description, Func<T, bool> isValid) : IValidator<T>
{
    public string Description => description;

    public string? Validate(T value) => isValid(value) ? null : description;
}
