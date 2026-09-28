using Leander.Configuration.Descriptors;
using Leander.Primitives;
using Leander.Primitives.Internal;

namespace Leander.Configuration.Internal;

// The state of describing one contract: the primitives found so far, in order of first use, each followed by its bases.
internal sealed class DescriptorContext
{
    private readonly HashSet<Primitive> _described = new(ReferenceEqualityComparer.Instance);
    private readonly List<PrimitiveDescriptor> _primitives = [];

    public IReadOnlyList<PrimitiveDescriptor> Primitives => _primitives;

    // Whether the contract definition being described is sensitive; its defaults are left out.
    // Set per contract definition, like ReadContext.IsSensitive.
    public bool IsSensitive { get; set; }

    // Primitives are described once and referenced. Names are unique per type within a contract (see ContractChecker),
    // so the reference is unambiguous.
    public ValueDescriptor DescribeScalar<T>(Primitive<T> primitive)
    {
        for (Primitive<T>? current = primitive; current is not null && _described.Add(current); current = current.Base)
        {
            _primitives.Add(Describe(current));
        }

        return new ValueDescriptor(TypeNames.Get(typeof(T)), ValuePresence.Required, ValueForm.Scalar)
        {
            Primitive = Reference(primitive),
        };
    }

    private static PrimitiveReference Reference<T>(Primitive<T> primitive) => new(TypeNames.Get(typeof(T)), primitive.Name);

    // Only its own rules: the base's rules are on the base's descriptor.
    private static PrimitiveDescriptor Describe<T>(Primitive<T> primitive) =>
        new(TypeNames.Get(typeof(T)), primitive.Name, primitive.Description)
        {
            Base = primitive.Base is { } @base ? Reference(@base) : null,
            Normalizers = [.. primitive.Normalizers.Select(normalizer => normalizer.Description)],
            Validators = [.. primitive.Validators.Select(validator => validator.Description)],
            Values = typeof(T).IsEnum ? Enum.GetNames(typeof(T)) : null,
        };
}
