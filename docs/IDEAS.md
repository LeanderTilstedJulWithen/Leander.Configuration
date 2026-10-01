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
- Verdict: implemented as `new(name, base) { … }`, see DESIGN.md (Deriving). Cycles can't happen: a base must exist before the primitive derived from it.

### Explicit primitives, no registry

Most of the growth in Primitives comes from implicit defaults, not from the converters themselves (those are small). Once `Define<int>("Port")` has to work without naming a primitive, a chain follows:

1. something must find "the int primitive": a registry keyed by type
2. the registry must be filled: `RegisterDefaults()` and a build step
3. types that can't be listed up front (enums) need fallbacks, reflection and a cache
4. a primitive is only complete once combined with its type's default: `PrimitiveDefinition` vs `Primitive`, a nullable converter meaning "use the default", and the "append vs replace" question
5. resolution can fail: `PrimitiveFailure`, the contract build step, `ContractResolver`, `InternalsVisibleTo`
6. with a registry in place, lookup by name (`"Hex"`) is nearly free, and brings its own failures
7. documentation must work out whether a primitive "is the registered default" (instance identity, `IsEmpty`)

The alternative: a primitive is always explicit and always complete. The library says how to create one and supplies tools, but doesn't do every type for you.

```csharp
public static readonly Primitive<int> Port =
    Primitive.Create("Port", Converters.Int32)
        .Validate(Validators.InRange(1, 65535))
        .Describe("A TCP port.");

public static readonly Primitive<int> AdminPort = Port.Validate(...);  // derived: adds rules, never removes them

ConfigurationDefinition.Define("Server:Port", Port)
```

- One type instead of two. No definition/resolved split, no nullable converter, no merge rules.
- Sharing is a C# field: the compiler is the registry. No name lookup, no fallbacks, no build failures.
- The name is for display only (`Int32 (Port)` in messages and documentation).
- Enums use `Converters.Enum<T>()`, which is constrained, so no reflection.
- Deriving is "a primitive plus rules", which is the inheritance idea above without a new concept.
- Documentation: "shared" means the same instance used by several keys. No registry needed to tell.
- The library becomes a toolbox: the interfaces, converters for common types, validators, normalizers, and perhaps a static class of ready-made primitives (`Primitives.Int32`, `Primitives.String`) as plain fields. Convenience, not policy.

Would go away: `PrimitiveRegistry`, `PrimitiveRegistryBuilder`, `IPrimitiveFallback`, `EnumPrimitiveFallback`, `EnumConverterCache`, `PrimitiveFailure`, the `PrimitiveDefinition`/`Primitive` split, name-based `Define<T>(key, "Hex")`, most of `ContractResolver`, and the "append vs replace" and "enum fallback visibility" open questions. The contract build step shrinks to duplicate keys and presence.

Notes:
- The cost is terseness: `Define<int>("Port")` becomes `Define("Port", Primitives.Int32)`.
- The one real argument for the registry is app-wide policy in one place: every string trimmed, every `DateTime` UTC. Without a registry, the application defines its own `MyPrimitives.String` and uses it everywhere. That is explicit, and visible in the definitions.
- Named variants like Hex become plain fields (`Primitives.Int32Hex`), not registered names.
- This addresses how Primitives grew and why the contract needs a build step. It doesn't address the other source of small rules: combining modifiers in Configuration (`Optional` × `Default` × `Indexed` × `Delimited` × `Sensitive`). That is a separate question, see Configuration below.
- Verdict: accepted, see DESIGN.md (Primitives, Deriving, Ready-made primitives). It removes more than it adds, and it keeps Primitives as the interesting part rather than a support library for Configuration.

### Formatting bounds in rule descriptions

A rule doesn't know the primitive's converter, but its bounds should be formatted with it: `0xFF` on a Hex primitive.

Tried, in order:
- Placeholders in descriptions (`"upper bound {0}"`) and an `Arguments` list on the public interfaces, filled in by the primitive. Dropped: string formatting and loose arguments on the public interface.
- An internal `IFormattableRule<out T>` with `Describe(IFormatter<T>)`, implemented by the built-in rules. It had to be covariant while `IValidator<in T>` was contravariant, so a rule used through contravariance silently fell back to its `Description`. Replaced by the next one.
- `Describe(IFormatter<T>)` on `IValidator<T>` and `INormalizer<T>` as a default interface member, with validators made invariant. It left two texts that must agree (`Description` and `Describe`), and a convention: a failure message equal to `Description` was formatted, any other wasn't. Replaced by the next one.
- `IFormattableText<T>` for both descriptions and failures. Accepted, see Structured validation failures and formattable text.

Also considered:
- A structured `RuleText` (template and arguments) as the description. Tidier, but still formatting on the public interface.
- The formatter passed in: `Validators.LessThanOrEqual(255, Converters.Int32Hex)`. Nothing hidden, but it repeats the converter.
- Rules bound when the primitive is built: `Validators = [v => v.LessThanOrEqual(255)]`. Always exact, but it reopens how primitives are constructed.

### Structured validation failures and formattable text

`Validate` returns a string, so a failure message is final when the validator writes it. A structured failure would carry its parts: the text, values of `T` such as bounds, and the checked value. The primitive could then:

- **format every value with its converter:** `must be at most 0xFF, but was 0x100`;
- **redact only the checked value** for sensitive definitions. Today redaction relies on validators never echoing the value, which is a convention, not a guarantee;
- **keep the failure structured** for tooling or localization.

It is the same idea as an opt-in `IDiagnosticParser<T>` for parse errors (see DESIGN.md, Parse error messages).

Notes:
- It became possible when validators were made invariant: a failure carrying values of `T` produces `T`, which a contravariant `IValidator<in T>` couldn't return.
- One shape: an interpolated-string handler, so a custom rule stays one line. `Bound(…)` and `Value(…)` mark the parts the primitive formats, and redacts in the case of `Value`:
  ```csharp
  Validators.Create<int>(v => v <= 255, v => $"must be at most {Bound(255)}, but was {Value(v)}");
  ```
- Changing what `Validate` returns is breaking after 1.0.0. Adding a second, opt-in method alongside it (as a default interface member) is not.
- The current rule, "a failure message equal to `Description` is formatted", is a convention the library imposes and then relies on. A user's own message behaves differently without the interface saying so. Structure in the return type removes the convention.
- **Principle: interfaces allow the general case, and conventions live in the helpers.** Our own habits, such as one failure per rule, belong in `Validators.*`, not in the contract every implementer must follow.

Proposed shape, for before 1.0.0:

```csharp
public interface IValidator<T>
{
    IFormattableText<T> Description { get; }
    IReadOnlyList<IFormattableText<T>> Validate(T value);   // empty when valid
    bool IsValid(T value) => Validate(value).Count == 0;    // optional fast path, e.g. TryParse without errors
}

public interface INormalizer<T>
{
    IFormattableText<T> Description { get; }
    T Normalize(T value);
}

public interface IFormattableText<out T>
{
    string FormatWith(IFormatter<T> formatter);
}
```

- **The primitive is the only consumer.** It holds both the rules and the converter, so it knows how to format the text, and it turns it into a string at its boundary: `TryParse` errors, diagnostics and descriptors stay strings. Only rule authors see the new type. Callers of primitives never do.
- **One type for descriptions and failures.** A description in the documentation and a failure message are the same kind of text. A property, not `Describe(formatter)`: the verb blurred "describe it" and "get its description".
- **A validator may report several failures**, e.g. a password policy reporting each unmet requirement. One failure per validator was our convention, from one documentation line per rule, and the interface shouldn't make it a rule for everyone. The built-in rules and `Validators.Create` keep returning one. `Description` still describes the rule in one line.
- **Empty means valid**, not null: one way to say it.
- **`IReadOnlyList`, not `IEnumerable`.** A lazy `yield` enumeration would run the validator after `Validate` returned, outside the primitive's `try/catch`, so its exceptions would escape instead of becoming errors. A list also makes `Count` cheap. Valid values return a shared empty array.
- **`IsValid` is a default interface member**, so the two can't disagree unless a rule overrides it for speed.
- **Redaction is conservative.** For a sensitive definition, every value of `T` in the text is hidden, bounds included. Telling bounds from the checked value can come later, when there is a use case.
- **Covariant text.** `IFormattableText<out T>` only hands values of `T` to the formatter.
- **`Validators.Create(description, isValid)` stays one line.** Its text formats as the plain description.
- **Localization later:** e.g. `ILocalizableText<T> : IFormattableText<T>` with a key and arguments, and `FormatWith` as the fallback. It should adapt to `Microsoft.Extensions.Localization` rather than become its own vocabulary.
- **Naming:** not `FormattableString`, which `System` already has, with object arguments and a format-string mini-language.
- `ToString()` can't be required by an interface. The library's own texts override it with an invariant formatter, for debugging only.
- Verdict: accepted and implemented, see DESIGN.md (Primitives). Still open: telling bounds from the checked value for redaction, the list item index as structure instead of the `item 2:` prefix, and localization.

## Configuration

### Constructors for configuration definitions

After primitives moved to constructors, the same could be done for configuration definitions.

Notes:
- The problem that made fluent primitives go away doesn't exist here: a definition's identity is its key, and the key doesn't change along the chain.
- `Optional()` changes the type (`T` to `T?`), which a constructor can't do. It would need a public class per shape, two of them for optional value and reference types.
- C# allows an object initializer only after `new`, so factory methods and `init` properties can't be combined.
- The real problem was the order of `.Default(x).Indexed()` (element or list?). Choosing the layout at creation (`Indexed(key, list)`) and moving list rules to list primitives solves that without constructors.
- Verdict: dropped. Definitions stay fluent, see DESIGN.md (Configuration definitions, Collections).

### Nested indexed keys

`.Indexed().Indexed()` read `Key:0:0`, `Key:0:1`, … With list primitives, `Indexed(key, list)` reads one level. An indexed list of delimited lists still works (the element is a list primitive), but an indexed list of indexed lists doesn't.

Notes:
- Nothing used it. It could come back as an `Indexed(key, list)` overload whose element is itself indexed.
- Verdict: dropped for now.

### Optional items in indexed lists

A gap in the indices (`Key:0`, `Key:2`) could become a `null` item instead of a warning, with the items compacted as they are now.

Notes:
- It needs a list primitive with nullable items, which isn't designed. Primitives never see null (see DESIGN.md, Defaults and presence), so the null would have to be handled by the list, not the element.
- Nothing asks for it. The current behaviour, a warning and compacted items, is a reasonable answer.
- Verdict: not planned.

### A configurable contract builder

The contract builder could take policies before definitions are registered, e.g.:

```csharp
new ConfigurationContractBuilder()
    .RequireDescriptions()
    .SetDiagnosticLevel(DiagnosticSeverity.Warning)
    .Register(...)
    .Build();
```

Notes:
- The design has been simplified since this came up: primitives are explicit, builder methods never throw, and `Build()` reports every failure at once. There may be nothing left to configure.
- Policies on reading (unknown keys, treating warnings as errors) are planned separately (see TODO.md, Design). If a build-time policy is still wanted, it should follow the same shape.
- A policy like "every definition has a description" can be checked by the application from the descriptor, without the builder knowing about it.
- Verdict: probably not needed. Revisit if the read options leave a gap.

## Tooling

### A UI for writing configuration values

Given an existing contract, a UI could let someone write configuration values: one field per key, with its description, type, default and rules, and each value checked as it is typed.

Notes:
- The contract file may be enough to build the form: keys, presence, defaults, forms and descriptions. Checking values needs the primitives, which are code, so live validation needs the contract's assembly.
- Sensitive values would need a masked field, and a decision on where they are written.
- The output could be the example configuration (see TODO.md, Documentation) filled in with real values.
- Verdict: open.

### Breaking differences between contracts

`ContractComparison` reports what differs, not what it means. A difference could be classified as breaking or not: a new required key breaks existing configuration, a new default doesn't.

Notes:
- It needs a direction: which side is the source of truth. For drift, left is old and right is new. For two programs sharing configuration, neither is.
- Most aspects have a clear answer in one direction: removing a default, adding a validator or changing the type is breaking, and descriptions never are. Rule changes can't be judged from their descriptions alone, e.g. a changed range may be wider or narrower.
- It can be added on top of the aspects without changing the comparison.
- Verdict: open.

### A command-line tool

A `Leander.Configuration.Tool` (a `dotnet tool`) could take a built assembly, find the contract in it, and write the documentation, contract file or example configuration, or compare against a committed contract file, without code in the application.

Notes:
- Finding the contract needs a convention, e.g. definitions as public static members. That is how the samples declare them, but applications may declare them differently, and the library shouldn't assume it.
- Loading the assembly with its dependencies runs application code, since static initializers build the definitions.
- There is no need yet: a few library calls in the application or a test do the same.
- Verdict: not before 1.0.0. Tooling on top of the library waits until the library itself is stable.
