using Leander.Primitives.Normalization.Internal;

namespace Leander.Primitives.Normalization;

public static class Normalizers
{
    public static INormalizer<string> Trim { get; } = Create<string>("trim whitespace", value => value.Trim());

    public static INormalizer<string> FullPath { get; } = Create<string>("full path", Path.GetFullPath);

    // Bounds are formatted by the primitive's converter.
    public static INormalizer<T> UpperBound<T>(T upperBound) where T : IComparable<T>
        => Create(
            FormattableText.Create<T>(formatter => $"upper bound {formatter.Format(upperBound)}"),
            value => value.CompareTo(upperBound) > 0 ? upperBound : value);

    public static INormalizer<T> LowerBound<T>(T lowerBound) where T : IComparable<T>
        => Create(
            FormattableText.Create<T>(formatter => $"lower bound {formatter.Format(lowerBound)}"),
            value => value.CompareTo(lowerBound) < 0 ? lowerBound : value);

    public static INormalizer<T> Create<T>(string description, Func<T, T> normalize) =>
        Create(FormattableText.Create<T>(description), normalize);

    // For a description with values of T.
    public static INormalizer<T> Create<T>(IFormattableText<T> description, Func<T, T> normalize) =>
        new DelegateNormalizer<T>(description, normalize);
}
