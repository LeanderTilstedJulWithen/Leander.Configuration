using Leander.Primitives.Normalization.Internal;

namespace Leander.Primitives.Normalization;

public static class Normalizers
{
    public static INormalizer<string> Trim { get; } = Create<string>("trim whitespace", value => value.Trim());

    public static INormalizer<string> FullPath { get; } = Create<string>("full path", Path.GetFullPath);

    public static INormalizer<T> UpperBound<T>(T upperBound) where T : IComparable<T>
        => Create<T>("upper bound {0}", value => value.CompareTo(upperBound) > 0 ? upperBound : value);

    public static INormalizer<T> LowerBound<T>(T lowerBound) where T : IComparable<T>
        => Create<T>("lower bound {0}", value => value.CompareTo(lowerBound) < 0 ? lowerBound : value);

    public static INormalizer<T> Create<T>(string description, Func<T, T> normalize) =>
        new DelegateNormalizer<T>(description, normalize);
}
