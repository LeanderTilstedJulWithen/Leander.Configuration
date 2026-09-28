# Leander.Configuration — Design Overview

Status: draft. Leander.Primitives and the Leander.Configuration core are implemented. There are no tests yet. This document records the decisions made so far and the questions still open.

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

Options objects can still be exposed as `IOptions<T>` by registering a factory. Consumers will not notice the difference. Because reading and validating happen together, `ValidateOnStart()` is unnecessary.

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
  Leander.Configuration.Tool          command-line tool over Leander.Configuration.Tooling
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
| `IValidator<in T>`| `T → failure message?`| contravariant         |

Normalizers and validators carry a `Description`. Contravariance lets a single validator apply to many types, e.g. `Validators.Collections.NotEmpty` is an `IValidator<IEnumerable>` and works for any list.

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
- **Value rules live on the primitive.** `ConfigurationDefinition` has no per-value `Validate`/`Normalize`. A one-off rule needs a primitive of its own, e.g. `new("MaxConnections", Primitive.Int32) { Validators = [...] }`.
- The entry point is `ConfigurationDefinition.Define`, not `Configuration.Define`. A type named `Configuration` inside the namespace `Leander.Configuration` would break name lookup for consumers.

**Definitions are inert data.** This rule makes static initialization safe:

- Building a definition reads no configuration, does no I/O and has no side effects.
- **Builders never throw.** An exception in a static initializer becomes a `TypeInitializationException` and breaks the type for the rest of the process. Problems are reported when the contract is built or the definition is read.
- Configuration definitions do not reference each other.

### Definition hierarchy

- `ConfigurationDefinition` (abstract, non-generic): key, value type, description, required, has-default, optional, sensitive. Tooling enumerates this type. It also hosts the `Define` entry points.
- `ConfigurationDefinition<T> : ConfigurationDefinition`: the typed reader, the default value, and list-level rules.

### Keys

- The key separator is `:`, for compatibility with Microsoft configuration.
- Keys are plain keys. The collection form is chosen with a builder method.
- A contract may not define the same key twice (case-insensitive).

### The value pipeline

The stages are fixed, regardless of the order builder methods are called in:

```
source lookup ──► presence ──► parse ──► normalize ──► validate ──► [list normalize ──► list validate] ──► value
                  (Required/   (primitive converter, normalizers, validators)     (only after Indexed/Delimited)
                   Default)
```

- **Presence.** `null` from the source means *missing*. Any other value, **including `""`**, is passed to the parser, and it's the parser's decision whether `""` is valid.
- **Defaults** go through the primitive's normalizers and validators like any other value.
- All validators run, and every failure becomes a diagnostic. An exception thrown by a normalizer or validator becomes an error diagnostic. A failing normalizer skips validation.

### Collections

A collection is written as a definition of its **element**, followed by an explicit method that maps it to a list definition:

```csharp
ConfigurationDefinition.Define("Database:ConnectionStrings", AppPrimitives.ConnectionString)  // element rules on the primitive
    .Indexed()                                    // ConfigurationDefinition<string> → ConfigurationDefinition<IReadOnlyList<string>>
    .Validate(Validators.Collections.NotEmpty);   // list rule
```

| Method         | Source shape                                                   |
|----------------|----------------------------------------------------------------|
| `.Indexed()`   | One entry per element: `Key:0`, `Key:1`, …                     |
| `.Delimited()` | One entry holding a delimited list, e.g. `"a,b,c"`             |

- **List-level `Validate`/`Normalize`** are extension methods on `ConfigurationDefinition<IReadOnlyList<T>>`, because a list has no primitive of its own.
- **The description carries over** to the list. A list is required unless it has its own default, e.g. `.Delimited().Default([])`.
- **`.Indexed()` composes**: `.Indexed().Indexed()` reads `Key:0:0`, `Key:0:1`, …
- **`.Delimited()` requires a scalar element.** Otherwise the contract fails to build. Item diagnostics use `Key[i]`.
- **Index rules.** Index names must be integers and are ordered numerically. A non-integer or duplicate index is an error, and gaps produce a warning.
- **Mismatched form.** A value in the other form (e.g. `Key:0` exists but the definition is delimited or scalar) produces a warning.
- **Element defaults.** `.Default(x).Indexed()` is allowed, but the default belongs to each element, not the list. It only applies to an entry that exists without a value, so reading warns and suggests `.Indexed().Default(...)`. For `.Delimited()` an element default is never used, with the same kind of warning.
- **Optional lists.** `.Indexed().Optional()` is allowed: a missing list is `null`. That's different from a supplied, empty list.
- **Optional elements** (planned). `.Optional().Indexed()` gives `IReadOnlyList<T?>`, and a gap in the indices becomes `null` instead of a warning.
- **`.Optional().Delimited()`** (planned) fails the contract build with a message that names `Optional()`.
- **Dictionaries** (`Key:Name`) are not designed yet.

### Defaults and presence

Presence decides what happens when the source has no value. A definition has exactly one of three modes:

| Definition                                      | Missing value              | Type   |
|-------------------------------------------------|----------------------------|--------|
| `Define("Port", Primitive.Int32)`               | error: "value is required" | `int`  |
| `Define("Port", Primitive.Int32).Default(8080)` | the default                | `int`  |
| `Define("Port", Primitive.Int32).Optional()`    | `null`                     | `int?` |
- **Optional is explicit and shows in the type.** `.Optional()` maps `ConfigurationDefinition<T>` to `ConfigurationDefinition<T?>`, like `.Indexed()` maps to a list. A consumer can't forget that the value may be missing, and a required `int` is never quietly set to `0`.
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
- **Failures.** Build throws one exception listing every failure: duplicate keys, then primitive name clashes, then per definition: invalid presence (see Defaults and presence) and invalid `Delimited()` uses.
- **`ConfigurationContract`** is the complete list of definitions, which is also what tooling will enumerate.

### Sources

The core depends on a minimal `IValueSource`, not on `IConfiguration`:

- `GetValue(key)`: `null` means missing
- `GetChildNames(key)`: needed for indexed collections

In-memory sources:

- `ValueSource.FromPairs(pairs)`: copies the pairs into a case-insensitive dictionary. When keys differ only in case, the last one wins.
- `ValueSource.FromDictionary(dictionary, keyComparer)`: uses the dictionary as-is, without copying. `keyComparer` must be the dictionary's own comparer, so that child names are matched the same way as values are looked up.

`Leander.Configuration.Microsoft` will implement `IValueSource` over `IConfiguration`.

### Snapshot

**A source must satisfy the contract to produce a configuration.** Contract and source are aligned when every definition has a value or a default, and that value parses, normalizes and passes validation.

- `contract.Read(source)` reads *every* definition and returns a `ConfigurationSnapshot`, or throws one `InvalidConfigurationException` listing every error.
- `contract.TryRead(source, out snapshot, out diagnostics)` is the non-throwing variant. It gives no snapshot when there is an error.
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

Each diagnostic has a severity (`Error`, `Warning`, `Trace`), a key, a message and the definition it concerns. Only errors prevent a snapshot. Warnings and traces go to a logger or can be inspected in the debugger.

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
- **It carries over** to `.Indexed()`, `.Delimited()` and `.Optional()`, like the description.
- **Diagnostics never contain a sensitive value.** `'abc' is not a valid Int32` becomes `value is not a valid Int32`. That covers parse errors, delimited list errors, and exception messages from normalizers and validators, which may contain the value too. Validator failure messages are fixed descriptions, so they stay.
- **Primitives know nothing about keys**, so they can't decide on redaction. `Primitive<T>` has internal `TryParse`/`TryAccept` overloads with a `redact` flag, which Leander.Configuration passes. With it, the value and exception messages are left out. The public overloads still echo the input.
- **The contract definition decides.** Sensitivity is taken from the definition in the contract, not from the element being read, because `.Indexed().Sensitive()` leaves the element definition as it was.
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
                                               └──► example configuration (later)
```

- **The descriptor lives in `Leander.Configuration`.** It needs the contract's internals (readers, resolved primitives, default values), and building it inside the core keeps those internals internal. The descriptor itself is public, so anyone can write a renderer.
- **Renderers live in `Leander.Configuration.Tooling`.** The core stays about reading configuration, and output formats can change without touching it.
- **Text only.** The descriptor holds strings, not `Type`s or delegates: type names, formatted defaults, rule descriptions. A descriptor built from a running contract and one read back from a contract file are the same kind of object, so they can be compared.
- **"Descriptor", not "Description".** `Description` is already the string property on definitions and primitives, and `Describe(...)` is the builder method that sets it.
- **Library calls first.** The application or a test gets the descriptor from the contract and writes the files. A command-line tool that finds the contract in an assembly needs discovery conventions and comes later.

```csharp
var descriptor = contract.CreateDescriptor();                       // Leander.Configuration.Descriptors
File.WriteAllText("configuration.md", MarkdownDocumentation.Write(descriptor, "Server configuration"));
File.WriteAllText("configuration.contract.json", ContractFile.Write(descriptor));
ContractDescriptor committed = ContractFile.Read(File.ReadAllText("configuration.contract.json"));
```

### The descriptor

```
ContractDescriptor
  Definitions        every definition in the contract, in registration order
  Primitives         the named primitives that definitions use, and their bases

DefinitionDescriptor
  Key                "Database:ConnectionStrings"
  Description
  IsSensitive
  Value              ValueDescriptor

ValueDescriptor
  Type               display name without Nullable<>, e.g. "Int32", "IReadOnlyList<String>"
  Presence           Required | Default | Optional
  Default            formatted with the primitive's converter; null when there is none or the definition is sensitive
  Form               Scalar | Indexed | Delimited
  Primitive          Scalar only: a PrimitiveReference (type, name) to a primitive in ContractDescriptor.Primitives
  Delimiter          Delimited only
  Element            Indexed and Delimited only: the ValueDescriptor of each element
  Normalizers        list-level rule descriptions
  Validators         list-level rule descriptions

PrimitiveDescriptor
  Type, Name
  Base               a PrimitiveReference for a derived primitive; null otherwise
  Description
  Normalizers        descriptions of its own rules; the base's rules are on the base
  Validators
  Values             the names of an enum type; null for other types
```

The descriptor types are records, so a renderer or a comparison can use `with` and value equality (except for the lists).

- **Values are recursive.** A list's element is a value with its own presence, default and form, which covers element defaults, `.Indexed().Indexed()`, and optional elements once they exist.
- **Defaults are formatted by the converter**, so a `TimeSpan` default reads `00:00:30`, the same text that would be written in the source. A list default formats each element.
- **Primitives are referenced.** A definition refers to its primitive by type and name, and the primitive is described once. Names are unique per type within a contract (see Contract), so the reference is unambiguous.
- **Primitives are listed in order of first use**, each followed by its bases, and only those some definition uses, directly or as a base. That includes ready-made primitives such as `Int32`, so the contract file is complete.

### Documentation

`MarkdownDocumentation.Write(descriptor, title)` renders Markdown, because it renders on GitHub and diffs well when committed. Definitions are grouped by their first key segment, in order of first appearance. Each group has a summary table, followed by a section per key. Named primitives are described once, at the end, and linked from the keys that use them:

```markdown
## Server

| Key | Type | Presence | Default |
|-----|------|----------|---------|
| [`Server:Port`](#serverport) | [Int32 (Port)](#int32-port) | default | `8080` |
| [`Server:AllowedOrigins`](#serverallowedorigins) | list of Uri | required |  |

### `Server:AllowedOrigins`

Origins allowed to call the server.

- **Type:** list of Uri
- **Presence:** required
- **Form:** indexed: `Server:AllowedOrigins:0`, `Server:AllowedOrigins:1`, …
- **Validated:** must not be empty
- **Element:** Uri

## Primitives

### Int32 (Port)

A TCP port.

- **Validated:** must be between 1 and 65535
```

- **Headings and type names use the display name**, `Int32 (Port)`, like diagnostics do.
- **Elements are nested bullets** under **Element**, with their own presence, form and rules.
- **Primitives with nothing to say** (no description, rules, base or values), such as `Primitive.String`, are not listed or linked. The key shows only the type.
- **Derived primitives** say so in their section: "**Derived from:** [String (Email)](…)", followed by their own rules. The base's rules are in the base's section. A base with nothing to say, as in `new("Port", Primitive.Int32)`, is named without a link: "**Derived from:** Int32".
- **Sensitive definitions** get a **Sensitive** line, and a default shows as *hidden*.
- Lines end in `\n` on every platform, so the committed file doesn't change with the machine that wrote it.

### Contract file

The contract file is the descriptor as JSON, with a format version: `ContractFile.Write(descriptor)` and `ContractFile.Read(json)`. Properties and enum values are camelCase, and nulls are left out. `Read` throws `FormatException` for another format version, and `JsonException` for malformed JSON or missing required properties.

- **It is descriptive.** It is never imported and run. Normalizers and validators are code, and JSON can't hold them. This follows from "code is the authoring format".
- **Programs that share configuration share code**, i.e. a library with the primitives and definitions. Each program builds its own contract from the definitions it uses, which may be a subset.
- **The file is for comparing.** It is committed, so CI can detect drift. Later, two contract files can be checked against each other without loading either program, e.g. "both read `Database:CommandTimeout`, but one says Int32 with default 30 and the other says Int32 (Hex), required".

## Open questions

- **Replacing the converter** in a derived primitive, e.g. Email with a different parser. Not allowed for now.
- **Validators organisation.** One `Validators` class, or one class per type (`StringValidation`, …).
- **Converter descriptions.** Validators and normalizers have a `Description`, but converters don't. Documentation needs one for named converters like "Hex" and custom date formats. `PrimitiveDescriptor` gets a `Converter` property once they do.
- **Type names in contract files** are short (`Int32`, `Verbosity`). Two application types with the same name in different namespaces can't be told apart.
- **Defaults.** Typed vs string defaults.
- **Collections.** Dictionaries.
- **Reload.** `IOptionsMonitor` support. v1 reads once at startup.
- **Tests.** There are none yet, for either project.

## Milestones

1. **Core.** Done: definitions, source abstraction, reader, pipeline, diagnostics, exception with report.
2. **Primitives.** Done: parsing, normalization, validation, explicit primitives with deriving and ready-made primitives, contract builder. Tests pending.
3. **Microsoft adapter.** `IConfiguration` source, DI and `IOptions<T>` registration.
4. **Documentation.** Done: sensitive values, contract descriptor, Markdown documentation, contract file. Later: example configuration, comparing contract files, command-line tool.
5. **Generator.** Options construction code.
