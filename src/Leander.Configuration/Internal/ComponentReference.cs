namespace Leander.Configuration.Internal;

// A validator or normalizer given either directly or by registry key.
internal readonly record struct ComponentReference<TComponent>(TComponent? Instance, string? Key)
    where TComponent : class;
