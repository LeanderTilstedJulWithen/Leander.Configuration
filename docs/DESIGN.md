# Leander.Configuration — Design Overview

Status: draft. Leander.Primitives, Leander.Configuration, Leander.Configuration.MicrosoftExtensions and Leander.Configuration.Tooling are implemented and tested. This document records the decisions made so far and the questions still open.

## Purpose

Applications accumulate configuration keys that nobody fully knows about: what they mean, what type they are, what values are valid, and what happens when they are missing.

Leander.Configuration makes every configuration key an explicit, typed **definition** in C#. From that single declaration we get:

- reading and parsing, under our control rather than the Microsoft binder's
- validation that reports *all* problems at startup, not just the first one
- metadata that tooling can export as documentation, example configuration, or a contract file

## Principles

- **Explicit over conventional.** Keys, primitives, validators and normalizers are named in the definition. Nothing is inferred from property names.
- **Code is the authoring format.** Contracts are written in C#, not YAML or JSON. Any machine-readable artifact is a build product, not something humans maintain.
- **Avoid reflection where possible.** It is not forbidden, but it should be the exception and be visible.
- **Report everything at once.** Reading collects diagnostics and never stops at the first failure. Building a contract lists every problem in one exception.
- **Nullable-clean.** With `<Nullable>enable</Nullable>`, using the library needs no `!` and gives no warnings. Whether a value can be null shows in its type. (`IConfiguration` and `IOptions<T>` predate nullable reference types and don't follow this.)
- **Definitions describe, builders resolve.** Definitions are inert rules. Resolution happens in explicit build steps.
- **Stay small.** Microsoft's infrastructure keeps the configuration sources, providers, environment variables, DI and hosting. We replace only the binding step.

## Non-goals

- Replacing configuration sources/providers (JSON, environment variables, command line, secrets).
- Our own DI container or options lifecycle.
- A language-neutral schema system (kept possible, not built — see Documentation and contract files).

## Relationship to Microsoft.Extensions.Configuration

We **replace `.Bind()`**. The binder decides how strings are converted to objects, and we want that under our control.

| Owned by Microsoft                  | Owned by us                                        |
|-------------------------------------|----------------------------------------------------|
| Sources and providers               | Key definitions and the contract                   |
| Key/value storage, `IConfiguration` | Parsing, normalization, validation (Primitives)    |
| DI container, hosting               | Diagnostics and the fail-fast report               |
|                                     | Construction of options objects                    |

`AddConfigurationContract(contract, options)` follows DI conventions: registration never reads or throws. The snapshot is a singleton, read from the container's `IConfiguration` on first resolve. `ConfigurationContractOptions` holds the `ReadOptions` and `ValidateOnStart` (on by default), which makes the host resolve the snapshot in `StartAsync`, so an invalid configuration stops the host before anything runs. It goes through `ValidateOnStart()` on an internal options type, not an own `IStartupValidator`: the host resolves only one, so ours would replace everyone else's or be replaced. `ValidateOnStart` can be turned off, e.g. for a host built only to write documentation. Singleton is our default, not a rule: a lifetime setting belongs in `ConfigurationContractOptions` once reloading is designed.

Options objects can still be exposed as `IOptions<T>`: `AddOptionsFrom` builds them once from the snapshot. Consumers will not notice the difference. The snapshot never reloads, so `IOptionsSnapshot<T>` and `IOptionsMonitor<T>` of that `T` throw on resolve instead of quietly returning values that never change. They are registered with `TryAdd`, so an application that provides them itself keeps its own. Because reading and validating happen together, `ValidateOnStart()` is unnecessary.

## Project layout

```
src/
  Leander.Primitives                  what a value is: parsing, normalization, validation, primitive definitions
                                      no dependencies
  Leander.Configuration               where a value lives: keys, presence, collections, contract, snapshot, diagnostics
                                      depends on Leander.Primitives, no Microsoft.Extensions.*
  Leander.Configuration.MicrosoftExtensions
                                      IConfiguration source adapter, DI / IOptions registration
                                      (not .Microsoft: that namespace would shadow Microsoft.Extensions.* inside it)
  Leander.Configuration.Tooling       renders a contract descriptor: documentation, contract file, example configuration
                                      depends on Leander.Configuration
later:
  Leander.Configuration.Generators    source generator for options construction code
```

- **One package per project, with namespaces as separators.** Leander.Primitives has `Leander.Primitives`, `.Parsing`, `.Normalization` and `.Validation`. It can be extracted to its own repository later if needed.
- **Leander.Parsing was copied in** as `Leander.Primitives.Parsing`, replacing the cross-repository project reference.
- **Leander.Primitives grants `InternalsVisibleTo` to Leander.Configuration**, for the redacting `TryParse`/`TryAccept` overloads (see Sensitive values).

## Leander.Primitives

Parsers, normalizers and validators are all plain operations on values. None of them knows about keys, sources or presence:

| Operation         | Signature             | Variance              |
|-------------------|-----------------------|-----------------------|
| `IParser<T>`      | `string → T`          | invariant (`out` parameter) |
| `IFormatter<in T>`| `T → string`          | contravariant         |
| `IConverter<T>`   | both                  | invariant             |
| `INormalizer<T>`  | `T → T`               | invariant             |
| `IValidator<T>`   | `T → failures`        | invariant             |

**Rule texts are formatted by the primitive.** A rule doesn't know the converter, so a bound can't be written into its text for good when the rule is created: `LessThanOrEqual(255)` should read `0xFF` on a Hex primitive. A rule's description and failures are `IFormattableText<T>`, with `FormatWith(IFormatter<T>)`, and the primitive formats them with its converter.

```csharp
public interface IValidator<T>
{
    IFormattableText<T> Description { get; }                // what the rule requires, in one line
    IReadOnlyList<IFormattableText<T>> Validate(T value);   // every failure; empty when valid
    bool IsValid(T value) => Validate(value).Count == 0;    // override only for speed
}

public interface INormalizer<T>
{
    IFormattableText<T> Description { get; }
    T Normalize(T value);
}
```

- **The primitive is the only consumer of the texts.** It holds the rules and the converter, and turns the texts into strings at its boundary: `TryParse` errors, diagnostics and descriptors are strings. Only rule authors see `IFormattableText<T>`.
- **One type for descriptions and failures.** A line in the documentation and a failure message are the same kind of text. It also leaves room for localization later, e.g. an `ILocalizableText<T>` that adapts to `Microsoft.Extensions.Localization`.
- **A validator may report several failures**, e.g. a password policy reporting each unmet requirement. The built-in rules and `Validators.Create` report one, their description: that is the library's habit, not the interface's rule. The interface allows the general case, and conventions live in the helpers.
- **A list, not a lazy sequence.** A `yield` enumeration would run the rule after `Validate` returned, outside the primitive's `try/catch`. Empty, not null, means valid.
- **`IsValid` is a default interface member**, so it can't disagree with `Validate` unless a rule overrides it for speed. The primitive uses it when no error list is wanted.
- **Custom texts** come from `FormattableText.Create<T>("text")` or `FormattableText.Create<T>(formatter => $"must be at most {formatter.Format(max)}")`. Values of `T` go through the formatter, the checked value included: `must be at most 0xFF, but was 0x100`.
- **Validators are invariant.** A text hands values of `T` to the formatter, so `T` flows out of the rule, which `in T` doesn't allow. A rule for many types is a generic method returning an exactly typed instance, e.g. `Validators.Collections.NotEmpty<Uri>()`. The only cost is the type argument, which C# can't infer from the list it's put in.
- **`ToString()`** of the library's texts formats invariantly, for debugging. It isn't part of the contract.

### Primitives

A **primitive** is a reusable kind of value with its rules, similar to a SQL `CREATE DOMAIN`:

```csharp
public static readonly Primitive<string> Email = new("Email", Converters.String)
{
    Normalizers = [Normalizers.Trim],
    Validators = [Validators.Create<string>("must contain @", v => v.Contains('@'))],
    Description = "An e-mail address.",
};
```

- **Explicit and complete.** A primitive always has its name and converter. There is no registry, no default primitive per type, and nothing is looked up by type or name. Every configuration definition names its primitive.
- **Sharing is a C# field.** A primitive used by several keys is the same instance. The compiler is the registry.
- **Constructors and `init` properties, no fluent methods.** There is one way to build a primitive, and it's plain C#. `Primitive<T>` is an immutable class, not a record: identity matters (see Contract), value equality would get in the way. Its constructors never throw. The normalizer and validator lists are never `null`, only empty.
- **The name is required.** A primitive with different rules is a different kind of value, so it gets a different name. The name is shown in messages and documentation, and refers to the primitive in a contract file. It's displayed as `Int32 (Port)`, or just `Int32` when the name equals the type name.
- **Explicit over convenient.** Out of the box means "the library supplies the parts", not "every type just works". The application decides how a `TimeSpan` or `DateTime` is read, and that decision is visible in the definition.

### Deriving

A derived primitive is a primitive plus rules. The second constructor takes a name and a base:

```csharp
public static readonly Primitive<string> AdminEmail = new("AdminEmail", Email)
{
    Validators = [Validators.Create<string>("must be on our domain", v => v.EndsWith("@example.com"))],
};
```

- **A derived primitive can add rules but never remove them.** It takes the base's converter. `Normalizers` and `Validators` hold only its own rules. The base's rules always run first: all normalizers (base, then own), then all validators (base, then own). Every AdminEmail is a valid Email.
- **The description is not inherited.** A new name deserves its own description.
- **`Base`** refers to the base primitive, for documentation ("derived from Email").
- **Not `with`.** `with` copies the name and lets rules be replaced, which is the opposite of deriving.

### Ready-made primitives

The library supplies converters in `Converters`, and primitives for the same types as static properties on `Primitive`: `string`, `bool`, the integer and floating-point types, `decimal`, `Guid`, `Uri`, `TimeSpan`, `DateTime` (UTC), `DateTimeOffset`.

- **Named after their type**, so `Primitive.Int32` is named "Int32" and displays as `Int32`. The variants have their own names: `Primitive.Int32Hex` and `Primitive.UInt32Hex` ("Hex"), `Primitive.DateTimeLocal` ("Local").
- **Convenience, not policy.** They are plain values. An application that wants every string trimmed defines its own string primitive and uses it everywhere.
- **Enums are explicit.** `Primitive.Enum<T>()` and `Converters.Enum<T>()` are constrained to `where T : struct, Enum`, so no reflection is needed. `Primitive.Enum<T>()` is named after the enum type and gives the same instance on every call (a static field per `T`), so two keys using it don't clash.
- **`Primitives` is not a type name.** A type `Primitives` inside the namespace `Leander.Primitives` would break name lookup for consumers, like `Configuration` would (see Configuration definitions). Hence `Primitive.Int32`.

### List primitives

A list is a kind of value too: "a comma-separated list of URIs, at least one" says what a value is, not where it lives. List rules and the delimited format belong to the primitive:

```csharp
public static readonly ListPrimitive<Uri> Origins = new("Origins", Primitive.Uri, delimiter: ',')   // ',' is the default
{
    Validators = [Validators.Collections.NotEmpty<Uri>()],    // list rules
};
```

- **`ListPrimitive<T> : Primitive<IReadOnlyList<T>>`.** It can be used wherever a primitive can, and exposes `Element` and `Delimiter`. `Primitive<T>` is unsealed for this. Its public constructors can't stop other subclasses, but the hooks are `private protected`, so a subclass outside the library can't change how values are read.
- **The delimiter is a constructor argument**, not an `init` property: the converter is built in the constructor, and a derived list can't change the format.
- **Items go through the element primitive**: parse, normalize and validate, with all its rules. Then the list's own normalizers and validators run on the list. A value that is already a list, e.g. a default, goes through `TryAccept` the same way: each item through the element's `TryAccept`, then the list's rules.
- **The converter is delimited**: parsing splits on the delimiter and trims each item, and empty input is an empty list. Formatting joins the items' formatted values with it. This is `Converters.List` over the element primitive, so item rules are applied, but the converter reports only success or failure.
- **Item errors name the item**, because a primitive knows nothing about keys: `item 2: 'x' is not a valid Uri`, or `item 2: value is not a valid Uri` when redacted. Items are counted from 0, like indexed keys. Every item is checked, and every failure is reported.
- **Nested lists** are a list primitive whose element is another list primitive, e.g. `;` between rows and `,` within them. Using the same delimiter twice is the user's bug, and it isn't checked.
- **Deriving** a list uses `new ListPrimitive<T>(name, baseList)`: it keeps the element and delimiter, and adds list rules. Deriving with the `Primitive<T>` constructor gives a plain primitive without `Element`, which can't be used with `Indexed` (see Collections). It still checks its items through the converter, but its errors don't name the item.
- **No shortcut** like `Primitive.List(Primitive.String)`. Every call would give a new instance with the same name, which clashes in a contract. Lists are declared as fields, like other primitives.

## Leander.Configuration

### Configuration definitions

A configuration definition is **key + presence + primitive**:

```csharp
public static class DatabaseConfiguration
{
    public static readonly ConfigurationDefinition<int> CommandTimeout =
        ConfigurationDefinition.Define("Database:CommandTimeout", Primitive.Int32)   // a ready-made primitive
            .Default(30)
            .Describe("Maximum time in seconds allowed for a database command.");

    public static readonly ConfigurationDefinition<string> AdminEmail =
        ConfigurationDefinition.Define("Admin:Email", AppPrimitives.Email)           // the application's own
            .Describe("Where operational alerts are sent.");                        // no default, so required

    public static readonly ConfigurationDefinition<int> Flags =
        ConfigurationDefinition.Define("Flags", Primitive.Int32Hex);                // a named variant
}
```

- **Every definition names its primitive.** There is no `Define<T>(key)` and no lookup by name. A type without a primitive doesn't compile.
- **All value rules live on the primitive**, including list rules (see List primitives). `ConfigurationDefinition` has no `Validate`/`Normalize`. A one-off rule needs a primitive of its own, e.g. `new("MaxConnections", Primitive.Int32) { Validators = [...] }`.
- **Definitions stay fluent.** `Default`, `Optional`, `Sensitive` and `Describe` are builder methods. Unlike primitives, a definition's identity (its key) doesn't change along the chain, and `Optional()` changes the type (`T` to `T?`), which a constructor can't do.
- The entry point is `ConfigurationDefinition.Define`, not `Configuration.Define`. A type named `Configuration` inside the namespace `Leander.Configuration` would break name lookup for consumers.

**Definitions are inert data.** This rule makes static initialization safe:

- Building a definition reads no configuration, does no I/O and has no side effects.
- **Builders never throw.** An exception in a static initializer becomes a `TypeInitializationException` and breaks the type for the rest of the process. Problems are reported when the contract is built or the definition is read.
- Configuration definitions do not reference each other.

### Definition hierarchy

- `ConfigurationDefinition` (abstract, non-generic): key, value type, description, required, has-default, optional, sensitive. Tooling enumerates this type. It also hosts the `Define` entry points.
- `ConfigurationDefinition<T> : ConfigurationDefinition`: the typed reader and the default value.

### Keys

- The key separator is `:`, for compatibility with Microsoft configuration.
- Keys are plain keys. The layout is chosen by the entry point: `Define` for one entry, `Indexed` for one entry per item.
- A contract may not define the same key twice (case-insensitive).

### The value pipeline

The stages are fixed, regardless of the order builder methods are called in:

```
source lookup ──► presence ──► parse ──► normalize ──► validate ──► value
                  (Required/   (the primitive: converter, normalizers, validators;
                   Default/     for a list, each item through the element primitive, then the list's rules)
                   Optional)
```

- **Presence.** `null` from the source means *missing*. Any other value, **including `""`**, is passed to the parser, and it's the parser's decision whether `""` is valid.
- **Defaults** go through the primitive's normalizers and validators like any other value.
- All validators run, and every failure becomes a diagnostic. An exception thrown by a normalizer or validator becomes an error diagnostic. A failing normalizer skips validation.

### Collections

A list's format and rules are on its list primitive (see List primitives). The definition only says where the list lives:

```csharp
ConfigurationDefinition.Define("Server:AllowedOrigins", AppPrimitives.Origins);    // one entry: "a,b,c"
ConfigurationDefinition.Indexed("Server:AllowedOrigins", AppPrimitives.Origins);   // Key:0, Key:1, …
```

| Entry point                  | Source layout                                               |
|------------------------------|-------------------------------------------------------------|
| `Define(key, list)`          | One entry holding a delimited list, e.g. `"a,b,c"`          |
| `Indexed(key, list)`         | One entry per item: `Key:0`, `Key:1`, …                     |

- **Delimited is not a layout.** A delimited list is one value with a text format, so `Define` reads it like any other primitive.
- **`Indexed(key, list)`** takes a `ListPrimitive<T>` and gives a `ConfigurationDefinition<IReadOnlyList<T>>`. Each entry is read with `list.Element`, then the list's own rules run. The delimiter isn't used. If the element is a list primitive itself, each entry is a delimited list.
- **Default, Optional, Sensitive and Describe apply to the list**, and there is no order to get wrong: nothing applies to elements. A list is required unless it has a default, e.g. `.Default([])`. `.Optional()` makes a missing list `null`, which is different from a supplied, empty list.
- **Index rules.** Index names must be integers and are ordered numerically. A non-integer or duplicate index is an error, and gaps produce a warning.
- **Mismatched form.** A value in the other form (e.g. `Key:0` exists but the definition reads one entry) produces a warning.
- **Diagnostics.** An indexed entry's errors are reported under its own key, `Key:2`. A delimited list's item errors are reported under `Key`, with the item in the message (see List primitives).
- **Removed:** `.Indexed()` and `.Delimited()` as builder methods, list-level `Validate`/`Normalize` on definitions, element defaults and their warnings, the `Delimited()` scalar check, and `.Indexed().Indexed()` (nested indexed keys, `Key:0:0`; see IDEAS.md).
- **Optional items**, where a gap in the indices becomes `null`, are not planned (see IDEAS.md).
- **Dictionaries** (`Key:Name`) are not designed yet.

### Defaults and presence

Presence decides what happens when the source has no value. A definition has exactly one of three modes:

| Definition                                      | Missing value              | Type   |
|-------------------------------------------------|----------------------------|--------|
| `Define("Port", Primitive.Int32)`               | error: "value is required" | `int`  |
| `Define("Port", Primitive.Int32).Default(8080)` | the default                | `int`  |
| `Define("Port", Primitive.Int32).Optional()`    | `null`                     | `int?` |
- **Optional is explicit and shows in the type.** `.Optional()` maps `ConfigurationDefinition<T>` to `ConfigurationDefinition<T?>`. A consumer can't forget that the value may be missing, and a required `int` is never quietly set to `0`.
- **`T?` for both kinds of type.** For a value type, `T?` is `Nullable<T>`. For a reference type, it's the annotated `T?`. C# can't overload on constraints alone, so these are two extension methods (`where T : struct` and `where T : class`) in separate classes, but the caller sees one `.Optional()`.
- **Optional belongs to the definition, not the primitive.** Presence is about where a value lives, not what it is. Primitives stay non-nullable, and `Email` is the same primitive for a required and an optional key.
- **Primitives never see null.** A missing optional value skips the pipeline and gives `null`. A present value goes through the primitive's rules as usual, so rules need no nullable variants.
- **The definition carries an `IsOptional` flag.** `string?` and `string` are the same type at runtime, so the type alone can't tell. `IsRequired` is true when a definition has neither a default nor `.Optional()`.
- **Types keep the rules honest.** After `.Optional()` the type is `T?`, and methods that only fit `T` don't compile. We don't add nullable overloads.
- **Contract build errors.** A `null` default on a non-optional definition fails the contract build: the compiler already warns about `.Default(null)` on a `string` definition, and the build catches it at runtime too. A definition with both a default and `.Optional()` also fails: the default means the value is never null, so `T?` would be misleading.

### Contract

`ConfigurationContractBuilder` collects configuration definitions in one place. Primitives come with them:

```csharp
var contract = new ConfigurationContractBuilder()
    .Register(DatabaseConfiguration.CommandTimeout)
    .Register(DatabaseConfiguration.AdminEmail)
    .Build();
```

- **`Build()` checks, it doesn't resolve.** Primitives are complete, so there is nothing to look up.
- **Primitive names are unique per type.** Two different primitive instances with the same type and name in one contract are an error, e.g. an application's own `new("Int32", …)` next to `Primitive.Int32`. The same name on different types is fine: a "Utc" primitive for both `DateTime` and `DateTimeOffset`. The check covers the element primitives of lists and the bases of derived primitives.
- **Failures.** Build throws one exception listing every failure: duplicate keys, then primitive name clashes, then per definition: invalid presence (see Defaults and presence).
- **`ConfigurationContract`** is the complete list of definitions, which is also what tooling will enumerate.

### Sources

The core depends on a minimal `IValueSource`, not on `IConfiguration`:

- `GetValue(key)`: `null` means missing
- `GetChildNames(key)`: needed for indexed collections

In-memory sources:

- `ValueSource.FromPairs(pairs)`: copies the pairs into a case-insensitive dictionary. When keys differ only in case, the last one wins.
- `ValueSource.FromDictionary(dictionary, keyComparer)`: uses the dictionary as-is, without copying. `keyComparer` must be the dictionary's own comparer, so that child names are matched the same way as values are looked up.

Leander.Configuration.MicrosoftExtensions reads an `IConfiguration` with `configuration.AsValueSource()`. It reads live, without copying, and a section reads its keys relative to the section.

### Snapshot

**A source must satisfy the contract to produce a configuration.** Contract and source are aligned when every definition has a value or a default, and that value parses, normalizes and passes validation.

- `contract.Read(source)` reads *every* definition and returns a `ConfigurationSnapshot`, or throws one `InvalidConfigurationException` listing every error.
- `contract.TryRead(source, out snapshot, out diagnostics)` is the non-throwing variant. It gives no snapshot when there is an error.
- Both take optional `ReadOptions` (see Read options).
- `snapshot.Get(definition)` cannot fail for a definition in the contract. A definition outside the contract throws `ArgumentException`.
- `snapshot.Diagnostics` holds the warnings and traces from reading.
- The name avoids `Configuration`, which would clash with the `Leander.Configuration` namespace. "Snapshot" leaves room for reload: a new read gives a new snapshot.

The exception message lists every error:

```
Configuration is invalid:

  Database:CommandTimeout    'abc' is not a valid Int32
  Admin:Email                must contain @
  Flags                      'xyz' is not a valid Int32 (Hex)
```

### Diagnostics

Each diagnostic has a severity (`Error`, `Warning`, `Trace`), a key, a message and the definition it concerns, or `null` for a key the contract doesn't define. Only errors prevent a snapshot. Warnings and traces go to a logger or can be inspected in the debugger.

### Read options

A read is aligned with the contract beyond presence when asked to be: `contract.Read(source, options)`, `TryRead(source, options, …)` and `AddConfigurationContract(contract, new() { ReadOptions = options })`.

```csharp
contract.Read(source, new ReadOptions
{
    CheckedSections = ["Server", "Admin"],   // Server:Prot  is not in the configuration contract
    WarningsAsErrors = true,
});
```

- **Options are per read, not per contract.** The same contract can be read strictly in CI and loosely in development, and the builder stays about the definitions.
- **Unknown keys are checked only in the named sections.** An `IConfiguration` holds much more than the application's keys: `Logging`, `AllowedHosts`, every environment variable. Nothing is inferred from the contract, because a section can be shared, e.g. `Logging:Verbosity` next to Microsoft's `Logging:LogLevel`.
- **An unknown key is a warning**, reported under its own key, with no definition. A misspelled key isn't wrong by itself: the intended key falls back to its default. The walk stops at defined keys, whose readers already report entries in the wrong form.
- **`WarningsAsErrors`** turns every warning into an error, so it prevents a snapshot and shows in the exception message. There is no option to ignore warnings: they never block, and callers can filter `Diagnostics`.

### Parse error messages

`IParser<T>.TryParse` stays `bool`-only: fast, allocation-light, reusable. Messages are built from the type and the primitive name, e.g. "'abc' is not a valid Int32 (Hex)". If richer errors are needed later, an opt-in interface (e.g. `IDiagnosticParser<T> : IParser<T>`) can be detected with `is`.

### Sensitive values

Diagnostics echo raw values (`'Server=…;Password=…' is not valid`), and documentation shows defaults. A definition can mark itself sensitive:

```csharp
ConfigurationDefinition.Define("Api:Key", Primitive.String)
    .Sensitive()
    .Describe("Key for the payment provider.");
```

- **Sensitive belongs to the definition.** Whether a value is secret depends on what it's used for, not on its type: a plain `string` can be an API key. `IsSensitive` is on `ConfigurationDefinition`. A sensitive primitive (e.g. a connection string that is always secret) may come later.
- **It carries over** to `.Optional()`, like the description, and covers every item of a list.
- **Diagnostics never contain a sensitive value.** `'abc' is not a valid Int32` becomes `value is not a valid Int32`. That covers parse errors, delimited list errors, and exception messages from normalizers and validators, which may contain the value too. Rule texts are formatted with a redacting formatter: every value of `T` in them is hidden, e.g. `must be between (hidden) and (hidden)`. That is conservative, because a text can't tell a bound from the checked value.
- **Primitives know nothing about keys**, so they can't decide on redaction. `Primitive<T>` has internal `TryParse`/`TryAccept` overloads with a `redact` flag, which Leander.Configuration passes. With it, the value and exception messages are left out. The public overloads still echo the input.
- **The contract definition decides.** Sensitivity is taken from the definition in the contract, and passed down to everything read for it: items, and an optional definition's inner value.
- **Descriptors and documentation** mark the definition as sensitive and never show its default. Example configuration uses a placeholder.

## Options objects

Options classes are written by the application. Construction is explicit:

```csharp
new DatabaseOptions
{
    CommandTimeout = snapshot.Get(DatabaseConfiguration.CommandTimeout),
    AdminEmail = snapshot.Get(DatabaseConfiguration.AdminEmail),
};
```

This is handwritten at first. A source generator may later emit exactly this code. It generates explicit construction, never convention-based mapping, and never invents application types.

## Documentation and contract files

Documentation is a key motivation for the project: every key, its type, its rules and its default, generated from the same definitions the application reads with.

### One model, several renderers

A built contract produces a **contract descriptor**: plain data describing every definition and primitive. Every output is rendered from it:

```
ConfigurationContract ──► ContractDescriptor ──┬──► Markdown documentation
                                               ├──► contract file (JSON)
                                               └──► example configuration
```

- **The descriptor lives in `Leander.Configuration`.** It needs the contract's internals (readers, resolved primitives, default values), and building it inside the core keeps those internals internal. The descriptor itself is public, so anyone can write a renderer.
- **Renderers live in `Leander.Configuration.Tooling`.** The core stays about reading configuration, and output formats can change without touching it.
- **Text only.** The descriptor holds strings, not `Type`s or delegates: type names, formatted defaults, rule descriptions. A descriptor built from a running contract and one read back from a contract file are the same kind of object, so they can be compared.
- **"Descriptor", not "Description".** `Description` is already the string property on definitions and primitives, and `Describe(...)` is the builder method that sets it.
- **Library calls only.** The application or a test gets the descriptor from the contract and writes the files. A command-line tool would have to find the contract in an assembly, which assumes how applications declare it (see IDEAS.md).

```csharp
var descriptor = contract.CreateDescriptor();                       // Leander.Configuration.Descriptors
File.WriteAllText("configuration.md", Documentation.WriteMarkdown(descriptor, "Server configuration"));
File.WriteAllText("configuration.contract.json", ContractSerializer.WriteJson(descriptor));
ContractDescriptor committed = ContractSerializer.ReadJson(File.ReadAllText("configuration.contract.json"));
```

### The descriptor

```
ContractDescriptor
  Definitions        every definition in the contract, in registration order
  Primitives         the primitives that definitions use, with their bases and elements

DefinitionDescriptor
  Key                "Database:ConnectionStrings"
  Description
  IsSensitive
  Value              ValueDescriptor

ValueDescriptor
  Type               display name without Nullable<>, e.g. "Int32", "IReadOnlyList<String>"
  Presence           Required | Default | Optional
  Default            Scalar only: formatted with the primitive's converter; null when there is none or the definition is sensitive
  DefaultItems       Indexed only: each item of the default, formatted with the element's converter; null like Default
  Form               Scalar | Indexed
  Primitive          a PrimitiveReference (type, name) to a primitive in ContractDescriptor.Primitives;
                     for Indexed, the list primitive

PrimitiveDescriptor
  Type, Name
  Base               a PrimitiveReference for a derived primitive; null otherwise
  Element            a PrimitiveReference for a list primitive; null otherwise
  Delimiter          list primitives only
  Description
  Normalizers        descriptions of its own rules; the base's rules are on the base
  Validators
  Values             the names of an enum type; null for other types
```

The descriptor types are records, so a renderer or a comparison can use `with` and value equality (except for the lists).

- **Values are flat.** A list is described by its primitive, which refers to its element primitive. There are no element values with their own presence or default.
- **Defaults are formatted by the converter**, so a `TimeSpan` default reads `00:00:30`, the same text that would be written in the source. A list default is formatted by the list's converter. An indexed default also has its items, each formatted by the element's converter, because splitting the formatted list again is wrong when an item contains the delimiter.
- **Primitives are referenced.** A definition refers to its primitive by type and name, and the primitive is described once. Names are unique per type within a contract (see Contract), so the reference is unambiguous.
- **Primitives are listed in order of first use**, each followed by its bases and elements, and only those some definition uses. That includes ready-made primitives such as `Int32`, so the contract file is complete.

### Documentation

`Documentation.WriteMarkdown(descriptor, title)` renders Markdown, because it renders on GitHub and diffs well when committed. Definitions are grouped by their first key segment, in order of first appearance. Each group has a summary table, followed by a section per key. Named primitives are described once, at the end, and linked from the keys that use them:

```markdown
## Server

| Key | Type | Presence | Default |
|-----|------|----------|---------|
| [`Server:Port`](#serverport) | [Int32 (Port)](#int32-port) | default | `8080` |
| [`Server:AllowedOrigins`](#serverallowedorigins) | [IReadOnlyList<Uri> (Origins)](#ireadonlylisturi-origins) | required |  |

### `Server:AllowedOrigins`

Origins allowed to call the server.

- **Type:** [IReadOnlyList<Uri> (Origins)](#ireadonlylisturi-origins)
- **Presence:** required
- **Form:** indexed: `Server:AllowedOrigins:0`, `Server:AllowedOrigins:1`, …

## Primitives

### Int32 (Port)

A TCP port.

- **Validated:** must be between 1 and 65535

### IReadOnlyList<Uri> (Origins)

- **Element:** Uri
- **Delimiter:** `,`
- **Validated:** must not be empty
```

- **Headings and type names use the display name**, `Int32 (Port)`, like diagnostics do.
- **A key shows every rule that applies**, so it reads on its own: the primitive's description on the type line (`Int32 (Port): A TCP port.`), the delimiter (for a scalar list), the items with the element's rules nested, the values of an enum, and the normalizers and validators of the primitive and its bases, the bases' first. Some of this repeats the primitive's section, which the type still links to.
- **List primitives** show their element (linked when it's listed) and delimiter, then their own rules. They always have something to say, so they are always listed.
- **Angle brackets are escaped** in text (`IReadOnlyList\<Uri\>`), or GitHub reads `<Uri>` as an HTML tag. The example above leaves that out for readability.
- **Primitives with nothing to say** (no description, rules, base or values), such as `Primitive.String`, are not listed or linked. The key shows only the type.
- **Derived primitives** say so in their section: "**Derived from:** [String (Email)](…)", followed by their own rules. The base's rules are in the base's section. A base with nothing to say, as in `new("Port", Primitive.Int32)`, is named without a link: "**Derived from:** Int32".
- **Sensitive definitions** get a **Sensitive** line, and a default shows as *hidden*.
- Lines end in `\n` on every platform, so the committed file doesn't change with the machine that wrote it.

### Contract file

The contract file is the descriptor as JSON, with a format version: `ContractSerializer.WriteJson(descriptor)` and `ContractSerializer.ReadJson(json)`. The format is owned by Tooling: private JSON types in the internal `JsonContract` define it, so renaming or restructuring a descriptor never changes the file by accident. Definitions are flat (`key`, `description`, `type`, `primitive`, `presence`, `default`, `form`, `sensitive`). `default` is a string, or for an indexed value an array of its items. A primitive is named without its type where the type is implied: a value's primitive and a primitive's base. Defaults are left out: nulls, `sensitive` unless true, and empty rule lists. `Read` checks the format version first and throws `FormatException` for another one, because another version may have another shape. It throws `JsonException` for malformed JSON, a missing required property, or an unknown `presence` or `form`.

- **It is descriptive.** It is never imported and run. Normalizers and validators are code, and JSON can't hold them. This follows from "code is the authoring format".
- **Programs that share configuration share code**, i.e. a library with the primitives and definitions. Each program builds its own contract from the definitions it uses, which may be a subset.
- **The file is for comparing.** It is committed, so CI can detect drift. Two contract files can be compared without loading either program, e.g. "both read `Database:CommandTimeout`, but one says Int32 with default 30 and the other says Int32 (Hex), required" (see Comparing contracts).

### Comparing contracts

`ContractDiff.Compare(left, right)` lists what differs between two descriptors, e.g. the committed contract file against the current contract, or two programs that share configuration. Comparing the files as text says *that* something changed. The comparison says *what* changed, per key.

```
Server:Port: validators of Int32 (Port): [must be between 1 and 65535] → [must be between 1024 and 65535]
Server:Timeout: presence: default → required
Server:Timeout: default: 00:00:30 → none
Admin:Email: removed
Admin:Contact: added
```

- **One method for both use cases.** Every difference has a kind: `Added` (only in the right contract), `Removed` (only in the left) or `Changed`. Drift is any difference at all. Two programs sharing configuration care about `Changed`, the keys both read.
- **A changed key has one difference per aspect**: key spelling, type, presence, default, form, sensitive, description, primitive. The aspect lets a caller filter, e.g. leave out descriptions.
- **Primitives are compared on every key that uses them**, down through their bases and elements. The same primitive change shows up on each key, so every key answers "do both sides agree on this key?" on its own. When the keys refer to different primitives, only the reference is reported.
- **Keys match case-insensitively**, like `IConfiguration` reads them. A difference in spelling is its own aspect.
- **An indexed default is compared by its items.**
- **What a difference means is not decided here.** Whether it's breaking depends on which side is the source of truth, and on the use case (see IDEAS.md).

### Example configuration

`ConfigurationGenerator.WriteJson(descriptor)` renders an `appsettings.json`-style file with every key in the contract, to copy and fill in:

```json
{
  "Server": {
    "Host": "localhost",
    "Port": "<Port>",
    "MaxConnections": "<optional MaxConnections>",
    "AllowedOrigins": [ "<Uri>" ],
    "Features": ""
  },
  "Database": {
    "Password": "<secret>"
  }
}
```

- **Keys are split on `:`** into nested objects. Sections merge case-insensitively, like `IConfiguration` reads them, and keep the spelling of their first key.
- **Every value is a string**, the text the converter formats. `IConfiguration` reads strings anyway, so there is no guessing from type names.
- **A default is written as is.** An indexed list becomes a JSON array of its default items.
- **Without a default, a placeholder** says what is expected: `<Port>` for a required value, `<optional Port>` for an optional one, named after the primitive. An indexed list gets an array with one placeholder item, named after the element. JSON has no comments, so the placeholder is the only place to say it. The file doesn't read cleanly until it's filled in, which is the point.
- **Sensitive values are always `<secret>`**, with or without a default, and an indexed one is `[ "<secret>" ]`.
- **Optional keys are included**, so the example is a complete map of the configuration.
- **A key can't be both a value and a section**, e.g. `Server` and `Server:Port`. JSON can't hold both, so `Write` throws.

## Open questions

- **Replacing the converter** in a derived primitive, e.g. Email with a different parser. Not allowed for now.
- **Validators organisation.** One `Validators` class, or one class per type (`StringValidation`, …).
- **Converter descriptions.** Validators and normalizers have a `Description`, but converters don't. Documentation needs one for named converters like "Hex" and custom date formats. `PrimitiveDescriptor` gets a `Converter` property once they do.
- **Type names in contract files** are short (`Int32`, `Verbosity`). Two application types with the same name in different namespaces can't be told apart.
- **Defaults.** Typed vs string defaults.
- **Collections.** Dictionaries.
- **Reload.** `IOptionsSnapshot<T>` and `IOptionsMonitor<T>` support. 1.0 reads once at startup. Open: what an invalid reload does (keep the last valid snapshot, and how anyone finds out without a logging dependency), whether the `ConfigurationSnapshot` singleton stays the startup snapshot or gets a holder of its own, and that options types built from one snapshot come from the same read.

## Milestones

1. **Core.** Done: definitions, source abstraction, reader, pipeline, diagnostics, exception with report.
2. **Primitives.** Done: parsing, normalization, validation, explicit primitives with deriving and ready-made primitives, contract builder.
3. **Microsoft adapter.** Done: `IConfiguration` source, DI and `IOptions<T>` registration.
4. **Documentation.** Done: sensitive values, contract descriptor, Markdown documentation, contract file, example configuration, comparing contracts.
5. **Generator.** Options construction code.
