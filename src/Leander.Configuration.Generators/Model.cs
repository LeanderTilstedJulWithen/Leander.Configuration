using System.Collections;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Leander.Configuration.Generators;

// What the generator produces for one method: plain, equatable data, so the incremental pipeline can tell when nothing changed.
internal sealed record OptionsResult(string? HintName, string? Source, EquatableArray<DiagnosticInfo> Diagnostics);

// A diagnostic without a Location, which holds on to a syntax tree.
internal sealed record DiagnosticInfo(DiagnosticDescriptor Descriptor, LocationInfo? Location, EquatableArray<string> Arguments)
{
    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, Location? location, params string[] arguments) =>
        new(descriptor, LocationInfo.From(location), new EquatableArray<string>([.. arguments]));

    public Diagnostic ToDiagnostic() =>
        Diagnostic.Create(Descriptor, Location?.ToLocation(), [.. Arguments]);
}

internal sealed record LocationInfo(string FilePath, TextSpan Span, LinePositionSpan LineSpan)
{
    public static LocationInfo? From(Location? location) =>
        location is { IsInSource: true } ? new(location.SourceTree!.FilePath, location.SourceSpan, location.GetLineSpan().Span) : null;

    public Location ToLocation() => Location.Create(FilePath, Span, LineSpan);
}

// An immutable array compared by its items.
internal readonly struct EquatableArray<T>(ImmutableArray<T> items) : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    private readonly ImmutableArray<T> _items = items;

    public bool Equals(EquatableArray<T> other) => _items.AsSpan().SequenceEqual(other._items.AsSpan());

    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)(_items.IsDefault ? [] : _items)).GetEnumerator();

    public override int GetHashCode()
    {
        var hash = 17;
        foreach (var item in this)
        {
            hash = (hash * 31) + item.GetHashCode();
        }

        return hash;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
