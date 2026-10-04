using Leander.Configuration.Descriptors;

namespace Leander.Configuration.Tooling;

/// <summary>
/// Compares two contract descriptors, e.g. the committed contract file against the current contract,
/// or two programs that share configuration.
/// </summary>
/// <remarks>
/// Differences are per key: a changed primitive is reported on every key that uses it,
/// so each key answers "do both sides agree on this key?" on its own.
/// Keys match case-insensitively, like <c>IConfiguration</c> reads them.
/// Type names match when one ends with the other at a namespace boundary, e.g. <c>Status</c> and <c>Billing.Status</c>,
/// because a contract names its types as briefly as it can, and that depends on the other types in it.
/// </remarks>
public static class ContractDiff
{
    /// <summary>
    /// Returns every difference between <paramref name="left"/> and <paramref name="right"/>:
    /// keys in the left contract's order, then the keys only the right contract has.
    /// </summary>
    /// <remarks>
    /// Two programs sharing configuration care about <see cref="DifferenceKind.Changed"/>: the keys both read.
    /// </remarks>
    public static IReadOnlyList<ContractDifference> Compare(ContractDescriptor left, ContractDescriptor right)
    {
        var comparer = new Comparer(left, right);
        comparer.CompareContracts();
        return comparer.Differences;
    }

    private sealed class Comparer(ContractDescriptor left, ContractDescriptor right)
    {
        private readonly ContractDescriptor _left = left;
        private readonly Dictionary<PrimitiveReference, PrimitiveDescriptor> _leftPrimitives = Index(left);
        private readonly ContractDescriptor _right = right;
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

        private void Add(string key, DifferenceAspect aspect, string? primitive, string? left, string? right) =>
            Differences.Add(new ContractDifference(key, DifferenceKind.Changed)
            {
                Aspect = aspect,
                Primitive = primitive,
                Left = left,
                Right = right,
            });

        private void Compare(string key, DifferenceAspect aspect, string? primitive, string? left, string? right)
        {
            if (!string.Equals(left, right, StringComparison.Ordinal))
            {
                Add(key, aspect, primitive, left, right);
            }
        }

        private void CompareDefinitions(DefinitionDescriptor left, DefinitionDescriptor right)
        {
            var key = left.Key;
            var (leftValue, rightValue) = (left.Value, right.Value);

            Compare(key, DifferenceAspect.Key, null, left.Key, right.Key);
            if (!SameType(leftValue.Type, rightValue.Type))
            {
                Add(key, DifferenceAspect.Type, null, leftValue.Type, rightValue.Type);
            }

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
            if (!SameReference(left, right))
            {
                Add(key, aspect, owner, Name(left), Name(right));
                return;
            }

            if (left is null || right is null || !compared.Add(left)
                || !_leftPrimitives.TryGetValue(left, out var leftPrimitive)
                || !_rightPrimitives.TryGetValue(right, out var rightPrimitive))
            {
                return;
            }

            var name = Name(left);
            Compare(key, DifferenceAspect.Description, name, leftPrimitive.Description, rightPrimitive.Description);
            Compare(key, DifferenceAspect.Converter, name, leftPrimitive.Converter, rightPrimitive.Converter);
            Compare(key, DifferenceAspect.Delimiter, name, leftPrimitive.Delimiter?.ToString(), rightPrimitive.Delimiter?.ToString());
            Compare(key, DifferenceAspect.Normalizers, name, List(leftPrimitive.Normalizers), List(rightPrimitive.Normalizers));
            Compare(key, DifferenceAspect.Validators, name, List(leftPrimitive.Validators), List(rightPrimitive.Validators));
            Compare(key, DifferenceAspect.Values, name, List(leftPrimitive.Values), List(rightPrimitive.Values));
            ComparePrimitives(key, DifferenceAspect.Base, name, leftPrimitive.Base, rightPrimitive.Base, compared);
            ComparePrimitives(key, DifferenceAspect.Element, name, leftPrimitive.Element, rightPrimitive.Element, compared);
        }

        // An indexed default is compared by its items, which are exact even when an item contains the delimiter.
        private static string? DefaultText(ValueDescriptor value) => List(value.DefaultItems) ?? value.Default;

        private static Dictionary<PrimitiveReference, PrimitiveDescriptor> Index(ContractDescriptor contract)
        {
            var primitives = new Dictionary<PrimitiveReference, PrimitiveDescriptor>();
            foreach (var primitive in contract.Primitives)
            {
                primitives.TryAdd(new PrimitiveReference(primitive.Type, primitive.Name), primitive);
            }

            return primitives;
        }

        private static string? List(IReadOnlyList<string>? items) => items is null ? null : $"[{string.Join(", ", items)}]";

        private static string Lower<T>(T value) where T : notnull => value.ToString()!.ToLowerInvariant();

        // Like Primitive.DisplayName: "Int32 (Port)", or just "Int32" when the name is the type name.
        private static string? Name(PrimitiveReference? reference) =>
            reference is null ? null : reference.Name == reference.Type ? reference.Type : $"{reference.Type} ({reference.Name})";

        // Whether one name ends with the other at a namespace boundary: "Status" and "Billing.Status", but not "Billing.Status"
        // and "Shipping.Status", nor "Status" and "OrderStatus".
        private static bool SameName(string left, string right)
        {
            var (shorter, longer) = left.Length <= right.Length ? (left.Split('.'), right.Split('.')) : (right.Split('.'), left.Split('.'));
            return longer.AsSpan()[^shorter.Length..].SequenceEqual(shorter);
        }

        private static bool SameReference(PrimitiveReference? left, PrimitiveReference? right) =>
            left is null || right is null ? left == right : left.Name == right.Name && SameType(left.Type, right.Type);

        // A type's name depends on the rest of its contract, which names it as briefly as it can, so names match by suffix:
        // "IReadOnlyList<Status>" and "IReadOnlyList<Billing.Status>" are the same type. Generic arguments are compared one by one.
        private static bool SameType(string left, string right)
        {
            var (leftTokens, rightTokens) = (Tokens(left), Tokens(right));
            return leftTokens.Count == rightTokens.Count && leftTokens.Zip(rightTokens).All(pair => SameName(pair.First, pair.Second));
        }

        // Names and the characters between them: "IReadOnlyList<Billing.Status>" is ["IReadOnlyList", "<", "Billing.Status", ">", ""].
        private static List<string> Tokens(string type)
        {
            var tokens = new List<string>();
            var start = 0;
            for (var index = 0; index < type.Length; index++)
            {
                if (type[index] is '<' or '>' or ',' or '[' or ']')
                {
                    tokens.Add(type[start..index].Trim());
                    tokens.Add(type[index].ToString());
                    start = index + 1;
                }
            }

            tokens.Add(type[start..].Trim());
            return tokens;
        }
    }
}
