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

### Parameterized primitives

Status: out of scope for now (2026-10-02). Interesting, but it breaks too much: see Scope at the end. Kept for later, with the points that were leaning one way marked **Leaning**.

#### The problem

The documentation shows it best. Every list needs its own named primitive (`IReadOnlyList<Uri> (Origins)`, `IReadOnlyList<String> (Features)`), although they are the same thing with a different item. Every bound needs its own named primitive too (`Int32 (Port)`, `Int32 (ConnectionLimit)`), although they are the same rule with different numbers. A name is required because identity is the instance, and two instances with the same name clash in a contract. That is also why DESIGN.md (List primitives) forbids a shortcut like `Primitive.List(Primitive.String)`.

What is missing:

- **Generic primitives**: `List<T>`, where `T` is any primitive. `NonEmptyList<T>` is derived from `List<T>` and adds a rule.
- **Parameterized primitives**: `Range<T>(min, max)` instead of a named primitive per pair of bounds.

#### One concept: a primitive family

Both are the same thing: **a primitive with parameters**. A parameter is either a primitive (the `T` of `List<T>`) or a value (`min`, `max`, the delimiter). `ListPrimitive<T>` is already halfway there: it takes an element primitive and a delimiter. What it lacks is that the parameters are part of its identity.

- **A family** has a name, a description, and a factory from arguments to a primitive. `List`, `NonEmptyList` and `Range` are families. A primitive made by one is an **instance**.
- **Identity is family + arguments.** An instance is named after them: `List<Uri>`, `Range<Int32>(1, 65535)`. The same arguments give the same primitive, so two keys can both write `Primitive.List(Primitive.Uri)` and nothing clashes. Instances are cached per arguments, or compared by them.
- **Value arguments are formatted with the primitive they belong to**, like rule bounds: `Range<Int32 (Hex)>(0x1, 0xFF)`. A delimiter is formatted as itself. A default argument is left out of the name: `List<Uri>`, but `List<Uri>(';')`.
- **Named primitives stay.** A family says what is valid; a name says what the value *is*. "A TCP port" means more than "an integer from 1 to 65535", so they compose:

```csharp
public static readonly Primitive<int> Port = new("Port", Primitive.Range(Primitive.Int32, 1, 65535))
{
    Description = "A TCP port.",
};
```

  A key without a domain meaning uses the instance directly, e.g. `MaxConnections` with `Primitive.AtLeast(Primitive.Int32, 1)`. Most single-use named primitives would go away, and those that remain mean something.

**Leaning:**

- **The family is a non-generic object.** C# has no generic fields, so a family over a primitive parameter is a generic method, e.g. `Primitive.List<T>(Primitive<T> item, char delimiter = ',')`. For `List<Uri>` and `List<String>` to be recognised as the same family, the family itself must be one non-generic object that the method's instances refer to.
- **Family texts are written by the family's author**, with the parameter names: "A value from min to max." The rules of an instance stay concrete ("must be between 1 and 65535"), so error messages don't change. The alternative is rules built from parameter objects that format as their name in documentation and as their value when checking. It's more automatic, but it puts a second mode into every rule.
- **The delimiter is an argument of `List`**, defaulting to `,`. Reading a list as indexed keys (`Key:0`, `Key:1`) is the key's choice, a convention of `Indexed`, not part of the primitive. An indexed key ignores the delimiter, as today.

#### Documentation

Families are documented once, and instances need no section of their own: the type name is the arguments, and each part links to its section.

```markdown
| Key | Type | Presence | Default |
|-----|------|----------|---------|
| `Server:Port` | [Int32 (Port)](#int32-port) | default | `8080` |
| `Server:MaxConnections` | [AtLeast](#atleastt-min)<[Int32](#int32)>(1) | optional |  |
| `Server:AllowedOrigins` | [NonEmptyList](#nonemptylistt)<[Uri](#uri)> | required |  |

### Int32 (Port)

A TCP port.

- **Derived from:** [Range](#ranget-min-max)<[Int32](#int32)>(1, 65535)

### NonEmptyList<T>

A list with at least one item.

- **Derived from:** [List<T>](#listt-delimiter)
- **Used by:** `Server:AllowedOrigins` (Uri)

### Range<T>(min, max)

A value from min to max.

- **Used by:** [Int32 (Port)](#int32-port) (Int32, 1, 65535)
```

Contract comparison gets better too: `Range<Int32>(1, 65535)` becoming `Range<Int32>(1, 1024)` is an argument change, not "validators changed".

#### Open questions

- **Families deriving from families.** `NonEmptyList<T>` derives from `List<T>` at the family level, so the documentation can say so. The instance `NonEmptyList<Uri>` derives from `List<Uri>`. Does the family declare its base family, or is it inferred from what the factory returns?
- **Argument equality.** Primitives compare by identity (or family + arguments, recursively). Values could compare by `Equals`, or by their formatted text, which is what the contract file sees anyway. Formatted text is simpler and matches the documentation, but two values that format alike would be the same instance.
- **Which families the library supplies.** `List`, `NonEmptyList`, `Range`, `AtLeast`, `AtMost`, `MaxLength`, … are candidates. This overlaps with `Validators`: is `Primitive.Range(...)` just `Validators.InRange` with an identity?
- **User-defined families.** Do applications define their own families, or only use the library's? Defining one needs a factory, parameter names and a description, which is more API than a primitive.
- **Descriptor and contract file.** `ContractDescriptor` gets families, and a `PrimitiveReference` becomes family + arguments, recursively. The JSON format changes (still `FormatVersion` 1 while pre-release).
- **`ListPrimitive<T>`.** Does it become the `List` family's instance type, and does `new ListPrimitive<T>(name, …)` stay as a way to name a list?

#### For and against

For:
- Rules become reusable with different values, without inventing a name for each.
- Lists of any item come for free, and `Primitive.List(...)` stops being a clash.
- The documentation shows the structure: what is a list, what is bounded, by what.
- Comparing contracts reports arguments, not rule texts.

Against:
- It changes identity, the core of the primitive model: "sharing is a C# field" no longer holds for instances.
- It adds a concept (family, instance, arguments) that every reader of the documentation and every author of a family has to learn.
- The strongest gain is in the documentation. At runtime, a named primitive with `Validators.InRange(1, 65535)` already does the job.
- It's a breaking change, so it has to happen before 1.0.0 or wait for 2.0.

A smaller first step would be identity by arguments for `ListPrimitive<T>` only (`List<Uri>`, no name needed), without user-defined families. It tests the idea where the pain is largest.

#### Scope: a type system of our own

This is where the idea stops. A primitive is a refinement type (see Framing): a base type plus a predicate. Deriving fits that: a derived primitive is a subset of its base and adds no behaviour. Families go further:

- **Type constructors**: `List<T>` takes a primitive and returns one.
- **Values in types**: `Range(1, 65535)` puts numbers into an identity.
- **Structural identity**: same family and arguments, same primitive.

The next questions are already waiting: is `NonEmptyList<Port>` a `List<Int32>`? Is `Range(1, 10)` a subset of `Range(1, 100)`? Can families take families? Those are type-system questions. Answering them builds a small type system next to C#'s, and C#'s will always be the better one. It's the same reason "A primitive is a class" stopped short of being a feature.

#### Alternative: families are C# methods

C# already has parameters, reuse and generics: methods. A family is a helper that returns a primitive:

```csharp
public static Primitive<int> Range(Primitive<int> @base, int min, int max) =>
    new($"Range({min}, {max})", @base) { Validators = [Validators.InRange(min, max)] };
```

No new concept, and it follows "conventions are not rules": habits live in helpers. Only one thing gets in the way: two calls with the same arguments give two instances with the same name, which the contract builder rejects (and why DESIGN.md forbids `Primitive.List(...)`).

The minimal change is to **relax the name clash**: two primitives with the same type and name are the same primitive if their descriptions are equal (base, element, delimiter, format, rules, values). Only primitives that really differ clash. Then:

- `Range`, `NonEmptyList` and `Primitive.List(item)` are plain methods, in the library or the application.
- The documentation shows instances, e.g. `Int32 (Range(1, 65535))`, derived from Int32 with its rule. There is no family section with symbolic texts: that part was only for documentation anyway.
- Identity stays the instance, except that equal descriptions are allowed to share a name.

Open: whether comparing descriptions is the right equality (two different validators with the same text would merge), and whether the descriptor should then list such a primitive once.

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

### Reading a section of the contract

`IConfiguration.GetSection("Server")` gives a view of one part of the configuration. The contract could have the same: read only the definitions under a section, e.g. `contract.Read(source, section: "Server")`, or a sub-contract from `contract.GetSection("Server")`.

Notes:
- It may become relevant with reloading: a change under `Logging` shouldn't rebuild options built from `Server`, and an invalid value under `Admin` shouldn't block a reload of `Server`.
- Open: whether keys stay absolute (`Server:Port`) or become relative to the section, like `IConfiguration`. Definitions are identified by their absolute key, so relative keys would be a view, not new definitions.
- Open: what a section's snapshot is. A smaller `ConfigurationSnapshot` whose `Get` throws for definitions outside the section, or the same snapshot type with a filtered contract.
- `ReadOptions.CheckedSections` already names sections, for unknown keys. The two should use the same notion of a section.
- Verdict: undecided. Revisit with reloading.

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

## Structural rewrite

### Everything needs to be accessable by keys.
Each converter gets a key (perhaps split into parser and formatter each getting a key), Normalizers get a key, Validators get a key.

A primitive is then defined by it's converter keys, normalizer keys, validators keys and then resolved by registry (yes, this reintroduces the registry)

What this does? First of all documentation no longer needs to live on the object. The documentation can then live on a separate metadata registry, and metadata can be changed without touching the core library functionality. This fixes an issue where a feature like markdown generation doesn't meddle with the core library.

This also enables contract import, since importing a primitive is just importing a set of keys (the keys themselves need to resolved in code of course.)

Main drawback is the full rewrite, which may cause too much project, but if we feel like rewriting this from scratch .. :)