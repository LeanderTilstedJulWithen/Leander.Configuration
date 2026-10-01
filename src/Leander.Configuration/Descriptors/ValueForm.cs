namespace Leander.Configuration.Descriptors;

/// <summary>
/// Where a value lives in the source.
/// </summary>
public enum ValueForm
{
    /// <summary>
    /// One entry at the key. A delimited list is one entry, so it's scalar.
    /// </summary>
    Scalar,

    /// <summary>
    /// One entry per item under the key: <c>Key:0</c>, <c>Key:1</c>, ...
    /// </summary>
    Indexed,
}
