using Leander.Primitives.Normalization;
using Leander.Primitives.Validation;

namespace Leander.Configuration;

// Rules on a whole list, e.g. after Indexed() or Delimited(). Rules on individual values belong on the primitive.
public static class ConfigurationDefinitionExtensions
{
    public static ConfigurationDefinition<IReadOnlyList<T>> Normalize<T>(
        this ConfigurationDefinition<IReadOnlyList<T>> definition,
        INormalizer<IReadOnlyList<T>> normalizer) =>
        definition.AddNormalizer(normalizer);

    public static ConfigurationDefinition<IReadOnlyList<T>> Validate<T>(
        this ConfigurationDefinition<IReadOnlyList<T>> definition,
        IValidator<IReadOnlyList<T>> validator) =>
        definition.AddValidator(validator);
}
