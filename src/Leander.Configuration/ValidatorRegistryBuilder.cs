namespace Leander.Configuration;

public sealed class ValidatorRegistryBuilder
{
    private readonly Dictionary<(Type Type, string Key), object> _validators = [];

    public ValidatorRegistryBuilder Register<T>(IValidator<T> validator, string key)
    {
        _validators[(typeof(T), key)] = validator;
        return this;
    }

    public ValidatorRegistry Build() => new(new Dictionary<(Type Type, string Key), object>(_validators));
}
