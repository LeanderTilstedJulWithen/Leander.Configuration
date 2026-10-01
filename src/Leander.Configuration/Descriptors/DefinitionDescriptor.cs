namespace Leander.Configuration.Descriptors;

/// <summary>
/// A text-only description of a configuration definition.
/// </summary>
/// <param name="Key">The configuration key.</param>
/// <param name="Description">What the value is for, or <see langword="null"/>.</param>
/// <param name="IsSensitive">Whether the value is sensitive; its default is then left out.</param>
/// <param name="Value">How the value is read.</param>
public sealed record DefinitionDescriptor(
    string Key,
    string? Description,
    bool IsSensitive,
    ValueDescriptor Value);
