using Leander.Primitives.Internal;
using Leander.Primitives.Parsing;
using Leander.Primitives.Parsing.Internal;

namespace Leander.Primitives;

/// <summary>
/// A list read from one delimited string.
/// </summary>
/// <remarks>
/// Each item goes through the element primitive with all its rules, then the list's own rules run on the list.
/// Item errors name the item, counted from 0: "item 2: ...".
/// </remarks>
public sealed class ListPrimitive<T> : Primitive<IReadOnlyList<T>>, IListPrimitive
{
    /// <summary>
    /// Creates a list primitive whose items are read by <paramref name="element"/>, split at <paramref name="delimiter"/>.
    /// </summary>
    // The converter applies the element's rules too, so a plain primitive derived from this list still checks its items.
    public ListPrimitive(string name, Primitive<T> element, char delimiter = ',')
        : base(name, Converters.List(new PrimitiveConverter<T>(element), delimiter))
    {
        Element = element;
        Delimiter = delimiter;
    }

    /// <summary>
    /// Derives a list primitive from <paramref name="base"/>, keeping its element and delimiter, to add list rules.
    /// </summary>
    public ListPrimitive(string name, ListPrimitive<T> @base)
        : base(name, @base)
    {
        Element = @base.Element;
        Delimiter = @base.Delimiter;
    }

    /// <summary>
    /// The character the input is split at.
    /// </summary>
    public char Delimiter { get; }

    /// <summary>
    /// The primitive each item goes through.
    /// </summary>
    public Primitive<T> Element { get; }

    Primitive IListPrimitive.Element => Element;

    private protected override bool TryAcceptItems(IReadOnlyList<T> value, out IReadOnlyList<T> result, List<string>? errors, bool redact) =>
        TryItems(
            value,
            (T input, out T item, List<string>? itemErrors) => Element.TryAccept(input, out item, itemErrors, redact),
            out result,
            errors);

    private protected override bool TryConvert(string input, out IReadOnlyList<T> value, List<string>? errors, bool redact) =>
        TryItems(
            ListConverter<T>.Split(input, Delimiter),
            (string segment, out T item, List<string>? itemErrors) => Element.TryParse(segment, out item, itemErrors, redact),
            out value,
            errors);

    // Every item is checked when there is an error list; without one, the first failure stops.
    private static bool TryItems<TInput>(IReadOnlyList<TInput> inputs, ItemStep<TInput> step, out IReadOnlyList<T> result, List<string>? errors)
    {
        var items = new List<T>(inputs.Count);
        var success = true;

        for (var index = 0; index < inputs.Count; index++)
        {
            var itemErrors = errors is null ? null : new List<string>();
            if (step(inputs[index], out var item, itemErrors))
            {
                items.Add(item);
                continue;
            }

            success = false;
            if (errors is null)
            {
                break;
            }

            errors.AddRange(itemErrors!.Select(error => $"item {index}: {error}"));
        }

        result = success ? items : default!;
        return success;
    }

    private delegate bool ItemStep<TInput>(TInput input, out T item, List<string>? errors);
}
