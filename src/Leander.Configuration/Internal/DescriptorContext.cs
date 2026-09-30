using Leander.Configuration.Descriptors;
using Leander.Primitives;
using Leander.Primitives.Internal;

namespace Leander.Configuration.Internal;

// The state of describing one contract: the primitives found so far, in order of first use, each followed by its elements and bases.
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
    public ValueDescriptor DescribeValue(Primitive primitive, ValueForm form)
    {
        Add(primitive);

        return new ValueDescriptor(TypeNames.Get(primitive.ValueType), ValuePresence.Required, form)
        {
            Primitive = Reference(primitive),
        };
    }

    private void Add(Primitive primitive)
    {
        for (var current = primitive; current is not null && _described.Add(current); current = current.Base)
        {
            _primitives.Add(Describe(current));

            if (current is IListPrimitive list)
            {
                Add(list.Element);
            }
        }
    }

    private static PrimitiveReference Reference(Primitive primitive) => new(TypeNames.Get(primitive.ValueType), primitive.Name);

    // Only its own rules: the base's rules are on the base's descriptor.
    private static PrimitiveDescriptor Describe(Primitive primitive) =>
        new(TypeNames.Get(primitive.ValueType), primitive.Name, primitive.Description)
        {
            Base = primitive.Base is { } @base ? Reference(@base) : null,
            Element = primitive is IListPrimitive list ? Reference(list.Element) : null,
            Delimiter = primitive is IListPrimitive { Delimiter: var delimiter } ? delimiter : null,
            Normalizers = primitive.NormalizerDescriptions,
            Validators = primitive.ValidatorDescriptions,
            Values = primitive.ValueType.IsEnum ? Enum.GetNames(primitive.ValueType) : null,
        };
}
