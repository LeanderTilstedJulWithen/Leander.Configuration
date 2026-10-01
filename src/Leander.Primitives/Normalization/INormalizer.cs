namespace Leander.Primitives.Normalization;

/// <summary>
/// A rule that changes a parsed value before it is validated, e.g. trimming whitespace.
/// </summary>
public interface INormalizer<T>
{
    /// <summary>
    /// What the rule does, in one line, e.g. for documentation.
    /// </summary>
    public IFormattableText<T> Description { get; }

    /// <summary>
    /// Returns the normalized value.
    /// </summary>
    public T Normalize(T value);
}
