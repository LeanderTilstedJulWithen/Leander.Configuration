using Leander.Configuration.Descriptors;
using Leander.Primitives;
using Leander.Primitives.Internal;

namespace Leander.Configuration.Internal;

// The state of describing one contract: the primitives found so far, in order of first use, each followed by its elements and bases.
internal sealed class DescriptorContext(ContractTypeNames typeNames)
{
    private readonly HashSet<Primitive> _described = new(ReferenceEqualityComparer.Instance);
    private readonly List<PrimitiveDescriptor> _primitives = [];
    private readonly ContractTypeNames _typeNames = typeNames;

    // Whether the contract definition being described is sensitive; its defaults are left out.
    // Set per contract definition, like ReadContext.IsSensitive.
    public bool IsSensitive { get; set; }

    public IReadOnlyList<PrimitiveDescriptor> Primitives => _primitives;

    // Primitives are described once and referenced. Names are unique per type within a contract (see ContractChecker),
    // and types are named so they can be told apart (see ContractTypeNames), so the reference is unambiguous.
    public ValueDescriptor DescribeValue(Primitive primitive, ValueForm form)
    {
        Add(primitive);

        return new ValueDescriptor(TypeName(primitive.ValueType), ValuePresence.Required, form)
        {
            Primitive = Reference(primitive),
        };
    }

    // The type's name in this contract, e.g. "Billing.Status" when another Status is in it too.
    public string TypeName(Type type) => _typeNames.Get(type);

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

    // Only its own rules: the base's rules are on the base's descriptor, and so is the converter it takes.
    private PrimitiveDescriptor Describe(Primitive primitive) =>
        new(TypeName(primitive.ValueType), primitive.Name, primitive.Description)
        {
            Base = primitive.Base is { } @base ? Reference(@base) : null,
            Converter = primitive.Base is null ? primitive.ConverterDescription : null,
            Element = primitive is IListPrimitive list ? Reference(list.Element) : null,
            Delimiter = primitive is IListPrimitive { Delimiter: var delimiter } ? delimiter : null,
            Normalizers = primitive.NormalizerDescriptions,
            Validators = primitive.ValidatorDescriptions,
            Values = primitive.ValueType.IsEnum ? Enum.GetNames(primitive.ValueType) : null,
        };

    private PrimitiveReference Reference(Primitive primitive) => new(TypeName(primitive.ValueType), primitive.Name);
}
