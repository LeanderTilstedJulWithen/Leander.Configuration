namespace Leander.Configuration.Descriptors;

public sealed record DefinitionDescriptor(
    string Key,
    string? Description,
    bool IsSensitive,
    ValueDescriptor Value);
