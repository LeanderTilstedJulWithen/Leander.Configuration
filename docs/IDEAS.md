# Ideas

Ideas, suggestions, thoughts and experiments collected along the way. Nothing here is part of the design. An idea moves to DESIGN.md or TODO.md only once it is accepted, and may just as well be dropped.

## Primitives

### Framing: a primitive is a refinement type

Primitives should feel like a project of its own, not a dependency of Configuration. A one-sentence purpose:

> A primitive is a named rule for a value that crosses a text boundary: how it's parsed and formatted, what its canonical form is, and what makes it valid.

That is close to a refinement type: a base type plus a predicate. "Port" is an `int` in 1..65535. This gives a test for new ideas:

- **Good:** the idea strengthens the rule (parse, format, canonicalize, validate) or makes rules easier to reuse.
- **Bad:** the idea gives the value behaviour (compare, hash, equality). C# already has classes and records for that.

### Split `IConverter<T>` into parser and formatter

Bundling parser and formatter in a single `IConverter<T>` may have been a mistake, because it gets in the way of variance. Kept separate, each side could vary on its own:

- A `ToStringFormatter` implementing `IFormatter<object>` would work for any type as a fallback.
- An `IParser<IReadOnlyList<string>>` would work as an `IParser<IEnumerable<string>>`.

Notes:
- `IParser<out T>` is not possible with `bool TryParse(string, out T)`, because an `out` parameter counts as a ref. Covariance needs `T Parse(string)` (throwing) or a reference-type result such as `IParseResult<out T>`.
- Variance does not apply to value types: an `IFormatter<object>` is never an `IFormatter<int>`. The fallback would have to be a generic `ToStringFormatter<T>`.
- The stronger argument is independence. Parse-only uses (reading configuration, CLI arguments) and format-only uses (documentation, example configuration) are both real. `Primitive<T>` could hold `Parser` and `Formatter` separately, with `IConverter<T>` kept as a convenience that implements both.
- Verdict: worth doing, but not for the variance.

### Comparer and equality comparer on the primitive

Validators like `GreaterThanOrEqual` already need a way to compare values. A `Primitive.Comparer` property could say how, with a fallback when `T` is `IComparable<T>`.

With a comparer, an `EqualityComparer` follows naturally. For example, two paths pointing to the same directory are the same path.

This grows the `Primitive` class quite a bit, but users only define what they need.

Notes:
- The actual need behind the comparer is that the comparison validators require `T : IComparable<T>`. Overloads taking an `IComparer<T>` solve that without making validators depend on the primitive they belong to.
- Normalization already covers the path example: `FullPath` gives a canonical form, and equal canonical forms are equal values.
- Nothing consumes an equality comparer yet. Revisit when something does, e.g. a "distinct items" validator for collections.
- Verdict: not yet.

### A primitive is a class

The comparer idea starts to look like a class. A primitive overrides `ToString()` (formatting), `Compare()`, `GetHashCode()` and `Equals()`, and parsing, normalization and validation together act as a constructor.

Unclear whether this is great or terrible: are we reinventing the wheel, or extending its usability? Either way, the name fits better: a primitive is conceptually a class.

Notes:
- The comparison is accurate, which is the reason to stop short of it. A user who wants custom `ToString`, `Equals` and `Compare` should write `readonly record struct Email`. Doing that inside Primitives would reinvent the wheel.
- The niche of Primitives is the opposite: types you don't own or don't want to wrap. Same `string`, different rules.
- Worth keeping: good support for wrapper types, e.g. `Primitive<Email>` where `Email` is the user's own record, so classes and primitives complement each other.
- Verdict: keep as framing (see the refinement type section), not as a feature.

### Inheritance between primitives

If a primitive is a class, inheritance sounds like a wanted feature:

```csharp
var adminEmail = Primitive.BasedOn(Email)
    .Rename("Admin:Email")
    .Validate(StringValidators.StartsWith("admin", StringComparison.OrdinalIgnoreCase));
```

Notes:
- One level of this already exists: resolving a named primitive appends the type's default normalizers and validators to its own. `BasedOn` would extend that to any base primitive instead of adding a new concept.
- Rule to keep it sound: a derived primitive can add rules but never remove them. Every AdminEmail is a valid Email.
- Open: whether a derived primitive may replace the parser (probably, as long as the base validators still run), and cycle detection when `BasedOn` refers to a registered name.
- Verdict: the most promising of the four.
