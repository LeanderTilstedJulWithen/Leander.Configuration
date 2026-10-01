using Leander.Primitives.Normalization.Internal;

namespace Leander.Primitives.Normalization;

/// <summary>
/// Ready-made normalizers, and <see cref="Create{T}(string, Func{T, T})"/> for your own.
/// </summary>
public static class Normalizers
{
    /// <summary>
    /// Removes leading and trailing whitespace.
    /// </summary>
    public static INormalizer<string> Trim { get; } = Create<string>("trim whitespace", value => value.Trim());

    /// <summary>
    /// Turns a path into an absolute path with <see cref="Path.GetFullPath(string)"/>.
    /// </summary>
    public static INormalizer<string> FullPath { get; } = Create<string>("full path", Path.GetFullPath);

    /// <summary>
    /// Replaces values above <paramref name="upperBound"/> with <paramref name="upperBound"/>.
    /// </summary>
    /// <remarks>The bound is formatted by the primitive's converter.</remarks>
    public static INormalizer<T> UpperBound<T>(T upperBound) where T : IComparable<T>
        => Create(
            FormattableText.Create<T>(formatter => $"upper bound {formatter.Format(upperBound)}"),
            value => value.CompareTo(upperBound) > 0 ? upperBound : value);

    /// <summary>
    /// Replaces values below <paramref name="lowerBound"/> with <paramref name="lowerBound"/>.
    /// </summary>
    /// <remarks>The bound is formatted by the primitive's converter.</remarks>
    public static INormalizer<T> LowerBound<T>(T lowerBound) where T : IComparable<T>
        => Create(
            FormattableText.Create<T>(formatter => $"lower bound {formatter.Format(lowerBound)}"),
            value => value.CompareTo(lowerBound) < 0 ? lowerBound : value);

    /// <summary>
    /// Creates a normalizer from a description and a function.
    /// </summary>
    public static INormalizer<T> Create<T>(string description, Func<T, T> normalize) =>
        Create(FormattableText.Create<T>(description), normalize);

    /// <summary>
    /// Creates a normalizer from a description with values of <typeparamref name="T"/> and a function.
    /// </summary>
    public static INormalizer<T> Create<T>(IFormattableText<T> description, Func<T, T> normalize) =>
        new DelegateNormalizer<T>(description, normalize);
}
