using Leander.Configuration.Descriptors;
using Leander.Primitives;
using Leander.Primitives.Internal;

namespace Leander.Configuration.Internal;

// The state of describing one contract: the registered primitives found so far, in order of first use.
internal sealed class DescriptorContext(ConfigurationContract contract)
{
    private readonly ConfigurationContract _contract = contract;
    private readonly HashSet<Primitive> _described = new(ReferenceEqualityComparer.Instance);
    private readonly List<PrimitiveDescriptor> _primitives = [];

    public ConfigurationContract Contract => _contract;

    public IReadOnlyList<PrimitiveDescriptor> Primitives => _primitives;

    // Whether the contract definition being described is sensitive; its defaults are left out.
    // Set per contract definition, like ReadContext.IsSensitive.
    public bool IsSensitive { get; set; }

    // Registered primitives are described once and referenced; any other primitive is described inline.
    public ValueDescriptor DescribeScalar<T>(Primitive<T> primitive)
    {
        var descriptor = new ValueDescriptor(TypeNames.Get(typeof(T)), ValuePresence.Required, ValueForm.Scalar);

        if (!_contract.Primitives.IsRegistered(primitive))
        {
            return descriptor with { InlinePrimitive = Describe(primitive) };
        }

        if (_described.Add(primitive))
        {
            _primitives.Add(Describe(primitive));
        }

        return descriptor with { Primitive = new PrimitiveReference(TypeNames.Get(typeof(T)), primitive.Name) };
    }

    private static PrimitiveDescriptor Describe<T>(Primitive<T> primitive) =>
        new(TypeNames.Get(typeof(T)), primitive.Name, primitive.Description)
        {
            Normalizers = [.. primitive.Normalizers.Select(normalizer => normalizer.Description)],
            Validators = [.. primitive.Validators.Select(validator => validator.Description)],
            Values = typeof(T).IsEnum ? Enum.GetNames(typeof(T)) : null,
        };
}
