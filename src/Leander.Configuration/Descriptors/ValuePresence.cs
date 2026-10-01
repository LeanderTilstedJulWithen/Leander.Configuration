namespace Leander.Configuration.Descriptors;

/// <summary>
/// Whether a value must be present in the source.
/// </summary>
public enum ValuePresence
{
    /// <summary>
    /// A missing value is an error.
    /// </summary>
    Required,

    /// <summary>
    /// A missing value is replaced by the default.
    /// </summary>
    Default,

    /// <summary>
    /// A missing value is <see langword="null"/>.
    /// </summary>
    Optional,
}
