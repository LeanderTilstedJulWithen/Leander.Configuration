namespace Leander.Configuration;

public interface IValidator<in T>
{
    public string Description { get; }

    // Returns null when the value is valid, otherwise a message describing the failure.
    public string? Validate(T value);
}
