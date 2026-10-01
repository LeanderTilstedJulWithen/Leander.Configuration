using Leander.Primitives.Parsing;

namespace Leander.Primitives.Internal;

internal sealed class DelegateText<T>(Func<IFormatter<T>, string> format) : IFormattableText<T>
{
    private readonly Func<IFormatter<T>, string> _format = format;

    public string FormatWith(IFormatter<T> formatter) => _format(formatter);

    // For debugging and code that doesn't know the type; not part of the contract.
    public override string ToString() => _format(InvariantFormatter<T>.Instance);
}
