using Leander.Configuration.Descriptors;

namespace Leander.Configuration.Tooling;

// Compares two contract descriptors, e.g. the committed contract file against the current contract,
// or two programs that share configuration. Differences are per key: a changed primitive is reported
// on every key that uses it, so each key answers "do both sides agree on this key?" on its own.
// Keys match case-insensitively, like IConfiguration reads them.
public static class ContractComparison
{
    // Keys in the left contract's order, then the keys only the right contract has.
    // Two programs sharing configuration care about Changed: the keys both read.
    public static IReadOnlyList<ContractDifference> Compare(ContractDescriptor left, ContractDescriptor right)
    {
        var comparer = new Comparer(left, right);
        comparer.CompareContracts();
        return comparer.Differences;
    }

    private sealed class Comparer(ContractDescriptor left, ContractDescriptor right)
    {
        private readonly ContractDescriptor _left = left;
        private readonly ContractDescriptor _right = right;
        private readonly Dictionary<PrimitiveReference, PrimitiveDescriptor> _leftPrimitives = Index(left);
        private readonly Dictionary<PrimitiveReference, PrimitiveDescriptor> _rightPrimitives = Index(right);

        public List<ContractDifference> Differences { get; } = [];

        public void CompareContracts()
        {
            var rightByKey = new Dictionary<string, DefinitionDescriptor>(StringComparer.OrdinalIgnoreCase);
            foreach (var definition in _right.Definitions)
            {
                rightByKey.TryAdd(definition.Key, definition);
            }

            var leftKeys = new HashSet<string>(_left.Definitions.Select(definition => definition.Key), StringComparer.OrdinalIgnoreCase);

            foreach (var definition in _left.Definitions)
            {
                if (rightByKey.TryGetValue(definition.Key, out var other))
                {
                    CompareDefinitions(definition, other);
                }
                else
                {
                    Differences.Add(new ContractDifference(definition.Key, DifferenceKind.Removed));
                }
            }

            foreach (var definition in _right.Definitions.Where(definition => !leftKeys.Contains(definition.Key)))
            {
                Differences.Add(new ContractDifference(definition.Key, DifferenceKind.Added));
            }
        }

        private void CompareDefinitions(DefinitionDescriptor left, DefinitionDescriptor right)
        {
            var key = left.Key;
            var (leftValue, rightValue) = (left.Value, right.Value);

            Compare(key, DifferenceAspect.Key, null, left.Key, right.Key);
            Compare(key, DifferenceAspect.Type, null, leftValue.Type, rightValue.Type);
            Compare(key, DifferenceAspect.Presence, null, Lower(leftValue.Presence), Lower(rightValue.Presence));
            Compare(key, DifferenceAspect.Default, null, DefaultText(leftValue), DefaultText(rightValue));
            Compare(key, DifferenceAspect.Form, null, Lower(leftValue.Form), Lower(rightValue.Form));
            Compare(key, DifferenceAspect.Sensitive, null, Lower(left.IsSensitive), Lower(right.IsSensitive));
            Compare(key, DifferenceAspect.Description, null, left.Description, right.Description);
            ComparePrimitives(key, DifferenceAspect.Primitive, null, leftValue.Primitive, rightValue.Primitive, []);
        }

        // Compares the references, and when both sides refer to the same primitive, the primitives themselves,
        // down through their bases and elements.
        private void ComparePrimitives(
            string key,
            DifferenceAspect aspect,
            string? owner,
            PrimitiveReference? left,
            PrimitiveReference? right,
            HashSet<PrimitiveReference> compared)
        {
            Compare(key, aspect, owner, Name(left), Name(right));

            if (left is null || left != right || !compared.Add(left)
                || !_leftPrimitives.TryGetValue(left, out var leftPrimitive)
                || !_rightPrimitives.TryGetValue(left, out var rightPrimitive))
            {
                return;
            }

            var name = Name(left);
            Compare(key, DifferenceAspect.Description, name, leftPrimitive.Description, rightPrimitive.Description);
            Compare(key, DifferenceAspect.Delimiter, name, leftPrimitive.Delimiter?.ToString(), rightPrimitive.Delimiter?.ToString());
            Compare(key, DifferenceAspect.Normalizers, name, List(leftPrimitive.Normalizers), List(rightPrimitive.Normalizers));
            Compare(key, DifferenceAspect.Validators, name, List(leftPrimitive.Validators), List(rightPrimitive.Validators));
            Compare(key, DifferenceAspect.Values, name, List(leftPrimitive.Values), List(rightPrimitive.Values));
            ComparePrimitives(key, DifferenceAspect.Base, name, leftPrimitive.Base, rightPrimitive.Base, compared);
            ComparePrimitives(key, DifferenceAspect.Element, name, leftPrimitive.Element, rightPrimitive.Element, compared);
        }

        private void Compare(string key, DifferenceAspect aspect, string? primitive, string? left, string? right)
        {
            if (!string.Equals(left, right, StringComparison.Ordinal))
            {
                Differences.Add(new ContractDifference(key, DifferenceKind.Changed)
                {
                    Aspect = aspect,
                    Primitive = primitive,
                    Left = left,
                    Right = right,
                });
            }
        }

        private static Dictionary<PrimitiveReference, PrimitiveDescriptor> Index(ContractDescriptor contract)
        {
            var primitives = new Dictionary<PrimitiveReference, PrimitiveDescriptor>();
            foreach (var primitive in contract.Primitives)
            {
                primitives.TryAdd(new PrimitiveReference(primitive.Type, primitive.Name), primitive);
            }

            return primitives;
        }

        // An indexed default is compared by its items, which are exact even when an item contains the delimiter.
        private static string? DefaultText(ValueDescriptor value) => List(value.DefaultItems) ?? value.Default;

        // Like Primitive.DisplayName: "Int32 (Port)", or just "Int32" when the name is the type name.
        private static string? Name(PrimitiveReference? reference) =>
            reference is null ? null : reference.Name == reference.Type ? reference.Type : $"{reference.Type} ({reference.Name})";

        private static string? List(IReadOnlyList<string>? items) => items is null ? null : $"[{string.Join(", ", items)}]";

        private static string Lower<T>(T value) where T : notnull => value.ToString()!.ToLowerInvariant();
    }
}
