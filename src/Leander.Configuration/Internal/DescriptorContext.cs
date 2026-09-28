using Leander.Configuration.Descriptors;
using Leander.Primitives;
using Leander.Primitives.Internal;

namespace Leander.Configuration.Internal;

// The state of describing one contract: the named primitives found so far, in order of first use,
// each followed by its bases.
internal sealed class DescriptorContext
{
    private readonly HashSet<Primitive> _described = new(ReferenceEqualityComparer.Instance);
    private readonly List<PrimitiveDescriptor> _primitives = [];

    public IReadOnlyList<PrimitiveDescriptor> Primitives => _primitives;

    // Whether the contract definition being described is sensitive; its defaults are left out.
    // Set per contract definition, like ReadContext.IsSensitive.
    public bool IsSensitive { get; set; }

    // Named primitives are described once and referenced; unnamed primitives are described inline.
    // Names are unique per type within a contract (see ContractChecker), so the reference is unambiguous.
    public ValueDescriptor DescribeScalar<T>(Primitive<T> primitive)
    {
        var descriptor = new ValueDescriptor(TypeNames.Get(typeof(T)), ValuePresence.Required, ValueForm.Scalar);

        if (primitive.Name is null)
        {
            return descriptor with { InlinePrimitive = Describe(primitive) };
        }

        // An unnamed base, e.g. DeriveFrom("Port", Primitive.Int32), is not listed: its rules are part of the derived primitive.
        for (Primitive<T>? current = primitive; current is { Name: not null } && _described.Add(current); current = current.Base)
        {
            _primitives.Add(Describe(current));
        }

        return descriptor with { Primitive = Reference(primitive)! };
    }

    private static PrimitiveReference? Reference<T>(Primitive<T>? primitive) =>
        primitive is { Name: { } name } ? new PrimitiveReference(TypeNames.Get(typeof(T)), name) : null;

    private static PrimitiveDescriptor Describe<T>(Primitive<T> primitive) =>
        new(TypeNames.Get(typeof(T)), primitive.Name, primitive.Description)
        {
            Base = Reference(primitive.Base),
            Normalizers = [.. primitive.Normalizers.Select(normalizer => normalizer.Description)],
            Validators = [.. primitive.Validators.Select(validator => validator.Description)],
            Values = typeof(T).IsEnum ? Enum.GetNames(typeof(T)) : null,
        };
}
