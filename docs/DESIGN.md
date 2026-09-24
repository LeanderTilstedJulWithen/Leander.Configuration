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
- **Report everything at once.** Reading collects diagnostics and never stops at the first failure. Building a contract or registry lists every problem in one exception.
- **Definitions describe, builders resolve.** Definitions are inert rules. Resolution happens in explicit build steps.
- **Stay small.** Microsoft's infrastructure keeps the configuration sources, providers, environment variables, DI and hosting. We replace only the binding step.

## Non-goals

- Replacing configuration sources/providers (JSON, environment variables, command line, secrets).
- Our own DI container or options lifecycle.
- A language-neutral schema system (kept possible, not built — see Tooling).

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
  Leander.Configuration               where a value lives: keys, presence, collections, contract, reader, diagnostics
                                      depends on Leander.Primitives, no Microsoft.Extensions.*
later:
  Leander.Configuration.Microsoft     IConfiguration source adapter, DI / IOptions registration
  Leander.Configuration.Generators    source generator for options construction code
  Leander.Configuration.Tool          export contract / documentation / example configuration
```

- **One package per project, with namespaces as separators.** Leander.Primitives has `Leander.Primitives`, `.Parsing`, `.Normalization` and `.Validation`. It can be extracted to its own repository later if needed.
- **Leander.Parsing was copied in** as `Leander.Primitives.Parsing`, replacing the cross-repository project reference.
- **Leander.Primitives grants `InternalsVisibleTo` to Leander.Configuration**, so the contract builder can resolve unregistered primitive definitions.

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

### Primitive definitions

A **primitive** is a named, reusable kind of value with its rules, similar to a SQL `CREATE DOMAIN`:

```csharp
public static readonly PrimitiveDefinition<string> Email =
    PrimitiveDefinition.Define<string>("Email")
        .Normalize(Normalizers.Trim)
        .Validate(Validators.Create<string>("must contain @", v => v.Contains('@')))
        .Describe("An e-mail address.");
```

- `PrimitiveDefinition<T>` is immutable and fluent, and its builder methods never throw. It only *describes* rules.
- Only the type is always known. The name can be `null`: a definition without a name is the **default** for its type. The converter can be `null`, meaning "use the default converter for T". The normalizer and validator lists are never `null`, only empty.
- **Deriving.** `Email.Validate(...)` returns a new definition with an extra validator.

### Resolution

`Primitive<T>` is a resolved definition. Its converter is guaranteed, and its rules are combined with the default for `T`:

```
Converter   = definition.Converter ?? default.Converter           // replace: there is only one
Normalizers = default.Normalizers + definition.Normalizers         // append
Validators  = default.Validators  + definition.Validators          // append
Description = definition.Description                               // not inherited
```

"Append" is provisional. Practice will show whether some cases need "replace", perhaps decided by the builder.

### Registry

- `PrimitiveRegistryBuilder.Register(definition)` stores definitions keyed by `(type, name)`. `(type, null)` is the default for that type.
- `RegisterDefaults()` registers defaults for the built-in types (`string`, `bool`, the integer and floating-point types, `decimal`, `Guid`, `Uri`, `TimeSpan`, `DateTime` (UTC), `DateTimeOffset`). It also registers the named variants `"Hex"` (`int`, `uint`) and `"Local"` (`DateTime`), plus the enum fallback.
- `IPrimitiveFallback.Define<T>()` supplies a default definition for types without a registered one. The enum fallback is the only place that uses reflection. Fallback results are cached by the registry.
- `Build()` resolves the defaults first, then the named definitions against them (including fallbacks). It throws a single exception listing every definition without a converter.
- `PrimitiveRegistry` stores resolved `Primitive<T>`s. It has `Get<T>(name = null)` and `TryGet<T>(name, out …)`, plus an internal `TryResolve(definition)` for definitions that are used without being registered.

## Leander.Configuration

### Configuration definitions

A configuration definition is **key + presence + primitive**:

```csharp
public static class DatabaseConfiguration
{
    public static readonly ConfigurationDefinition<int> CommandTimeout =
        ConfigurationDefinition.Define<int>("Database:CommandTimeout")       // default primitive for int
            .Default(30)
            .Describe("Maximum time in seconds allowed for a database command.");

    public static readonly ConfigurationDefinition<string> AdminEmail =
        ConfigurationDefinition.Define("Admin:Email", Primitives.Email)      // a primitive definition
            .Required();

    public static readonly ConfigurationDefinition<int> Flags =
        ConfigurationDefinition.Define<int>("Flags", "Hex");               // a registered named primitive
}
```

- **Value rules live on the primitive.** `ConfigurationDefinition` has no per-value `Validate`/`Normalize`. For a one-off rule, derive: `Define("Admin:Email", Primitives.Email.Validate(...))`.
- **Referencing a primitive by name** (`"Hex"`) is a string, but it's used only for registered primitives, and an unknown name fails when the contract is built.
- The entry point is `ConfigurationDefinition.Define`, not `Configuration.Define`. A type named `Configuration` inside the namespace `Leander.Configuration` would break name lookup for consumers.

**Definitions are inert data.** This rule makes static initialization safe:

- Building a definition reads no configuration, does no I/O and has no side effects.
- **Builders never throw.** An exception in a static initializer becomes a `TypeInitializationException` and breaks the type for the rest of the process. Problems are reported when the contract is built or the definition is read.
- Configuration definitions do not reference each other.

### Definition hierarchy

- `ConfigurationDefinition` (abstract, non-generic): key, value type, description, required, has-default. Tooling enumerates this type. It also hosts the `Define` entry points.
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
ConfigurationDefinition.Define("Database:ConnectionStrings", Primitives.ConnectionString)  // element rules on the primitive
    .Indexed()                                    // ConfigurationDefinition<string> → ConfigurationDefinition<IReadOnlyList<string>>
    .Validate(Validators.Collections.NotEmpty);   // list rule
```

| Method         | Source shape                                                   |
|----------------|----------------------------------------------------------------|
| `.Indexed()`   | One entry per element: `Key:0`, `Key:1`, …                     |
| `.Delimited()` | One entry holding a delimited list, e.g. `"a,b,c"`             |

- **List-level `Validate`/`Normalize`** are extension methods on `ConfigurationDefinition<IReadOnlyList<T>>`, because a list has no primitive of its own.
- **Presence carries over.** `Required()` before the mapping carries over to the list, as does the description.
- **`.Indexed()` composes**: `.Indexed().Indexed()` reads `Key:0:0`, `Key:0:1`, …
- **`.Delimited()` requires a scalar element.** Otherwise the contract fails to build. Item diagnostics use `Key[i]`.
- **Index rules.** Index names must be integers and are ordered numerically. A non-integer or duplicate index is an error, and gaps produce a warning.
- **Mismatched form.** A value in the other form (e.g. `Key:0` exists but the definition is delimited or scalar) produces a warning.
- **Dictionaries** (`Key:Name`) are not designed yet.

### Defaults and presence

`.Default(value)` is typed. `Required()` and `Default()` are mutually exclusive, and the last one called wins. A missing value with neither gives `default(T)` and a trace diagnostic. That means `null` for a non-nullable `string`, which is the weakest spot of the current design.

### Contract

`ConfigurationContractBuilder` collects primitive definitions and configuration definitions in one place:

```csharp
var contract = new ConfigurationContractBuilder()
    .RegisterDefaultPrimitives()
    .Register(Primitives.Email)
    .Register(DatabaseConfiguration.CommandTimeout)
    .Register(DatabaseConfiguration.AdminEmail)
    .Build();
```

- **`Build()`** builds the primitive registry first. It then resolves the primitive of every configuration definition, including the element definitions of lists.
- **Resolution is keyed by definition instance, not by name.** Derived primitives (`Email.Validate(...)`) keep the name "Email", but they never enter the registry's name table, so they never collide with the real Email.
- **Failures.** Build throws one exception listing every unresolvable primitive, duplicate key, or invalid `Delimited()` use.
- **`ConfigurationContract`** is the complete list of definitions, which is also what tooling will enumerate. `contract.Validate(source)` reads *every* definition and returns all diagnostics.

### Sources

The core depends on a minimal `IValueSource`, not on `IConfiguration`:

- `GetValue(key)`: `null` means missing
- `GetChildNames(key)`: needed for indexed collections

`ValueSource.FromDictionary(...)` provides a case-insensitive in-memory source. `Leander.Configuration.Microsoft` will implement `IValueSource` over `IConfiguration`.

### Reader

`new ConfigurationReader(contract, source)`:

- `Get`/`TryGet` read a definition through the pipeline. A definition that is not part of the contract is an error.
- Diagnostics are collected instead of thrown. Values of failed definitions are `default(T)`.
- `ThrowIfInvalid()` throws one `InvalidConfigurationException` listing every error:

```
Configuration is invalid:

  Database:CommandTimeout    'abc' is not a valid Int32
  Admin:Email                must contain @
  Flags                      'xyz' is not a valid Int32 (Hex)
```

### Diagnostics

Each diagnostic has a severity (`Error`, `Warning`, `Trace`), a key, a message and the definition it concerns. Only errors make `ThrowIfInvalid()` throw. Warnings and traces go to a logger or can be inspected in the debugger.

### Parse error messages

`IParser<T>.TryParse` stays `bool`-only: fast, allocation-light, reusable. Messages are built from the type and the primitive name, e.g. "'abc' is not a valid Int32 (Hex)". If richer errors are needed later, an opt-in interface (e.g. `IDiagnosticParser<T> : IParser<T>`) can be detected with `is`.

### Sensitive values

Diagnostics may echo raw values (`'Server=…;Password=…' is not valid`). A definition must eventually be able to mark itself sensitive so values are redacted. Deferred.

## Options objects

Options classes are written by the application. Construction is explicit:

```csharp
new DatabaseOptions
{
    CommandTimeout = reader.Get(DatabaseConfiguration.CommandTimeout),
    AdminEmail = reader.Get(DatabaseConfiguration.AdminEmail),
};
```

This is handwritten at first. A source generator may later emit exactly this code. It generates explicit construction, never convention-based mapping, and never invents application types.

## Tooling (later)

The contract lists every definition, and both definition hierarchies expose their metadata, so a tool can export:

- a contract artifact (build product, tracked in Git, CI can detect drift)
- documentation
- example configuration files

## Open questions

- **Append vs replace** when a definition is combined with its type's default (currently append).
- **Honest documentation for derived primitives.** `Email.Validate(...)` still looks like "Email". Perhaps "derived from Email". Revisit when documentation becomes a concern.
- **Validators organisation.** One `Validators` class, or one class per type (`StringValidation`, …).
- **Converter descriptions.** Validators and normalizers have a `Description`, but converters don't.
- **Enum fallback visibility.** It uses reflection, and should perhaps be reported as a trace diagnostic.
- **Defaults and optionality.** Typed vs string defaults, and how required/default/optional shows up in the type (`T` vs `T?`).
- **Collections.** Dictionaries.
- **Sensitive values.** API and redaction rules.
- **Reload.** `IOptionsMonitor` support. v1 reads once at startup.
- **Tests.** There are none yet, for either project.

## Milestones

1. **Core.** Done: definitions, source abstraction, reader, pipeline, diagnostics, exception with report.
2. **Primitives.** Done: parsing, normalization, validation, primitive definitions, registry with defaults and fallbacks, contract builder. Tests pending.
3. **Microsoft adapter.** `IConfiguration` source, DI and `IOptions<T>` registration.
4. **Tooling.** Contract export, docs, example configuration.
5. **Generator.** Options construction code.
