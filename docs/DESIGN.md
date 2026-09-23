# Leander.Configuration — Design Overview

Status: draft. Milestone 1 (core) is implemented. This document records decisions made so far and the questions still open.

## Purpose

Applications accumulate configuration keys that nobody fully knows about: what they mean, what type they are, what values are valid, and what happens when they are missing.

Leander.Configuration makes every configuration key an explicit, typed **definition** in C#. From that single declaration we get:

- reading and parsing, under our control rather than the Microsoft binder's
- validation that reports *all* problems at startup, not just the first one
- metadata that tooling can export as documentation, example configuration, or a contract file

## Principles

- **Explicit over conventional.** Keys, converters, validators and normalizers are named in the definition. Nothing is inferred from property names.
- **Code is the authoring format.** Contracts are written in C#, not YAML or JSON. Any machine-readable artifact is a build product, not something humans maintain.
- **Avoid reflection where possible.** It is not forbidden, but it should be the exception and be visible (see Diagnostics).
- **Report everything at once.** Reading collects diagnostics and never stops at the first failure.
- **Stay small.** Microsoft's infrastructure keeps the configuration sources, providers, environment variables, DI and hosting. We replace only the binding step.

## Non-goals

- Replacing configuration sources/providers (JSON, environment variables, command line, secrets).
- A general-purpose validation library for domain objects.
- Our own DI container or options lifecycle.
- A language-neutral schema system (kept possible, not built — see Tooling).

## Relationship to Microsoft.Extensions.Configuration

We **replace `.Bind()`**. The binder decides how strings are converted to objects, and we want that under our control through Leander.Parsing.

| Owned by Microsoft                  | Owned by Leander.Configuration          |
|-------------------------------------|-----------------------------------------|
| Sources and providers               | Key definitions (the contract)          |
| Key/value storage, `IConfiguration` | Parsing (via Leander.Parsing)           |
| DI container, hosting               | Normalization and validation            |
|                                     | Diagnostics and the fail-fast report    |
|                                     | Construction of options objects         |

Options objects can still be exposed as `IOptions<T>` by registering a factory. Consumers will not notice the difference. Because reading and validating happen together, `ValidateOnStart()` is unnecessary.

## Project layout

```
src/
  Leander.Configuration               core: definitions, reader, pipeline, diagnostics
                                      depends on Leander.Parsing only, no Microsoft.Extensions.*
  Leander.Configuration.Microsoft     IConfiguration source adapter, DI / IOptions registration
tests/
  Leander.Configuration.Tests
later:
  Leander.Configuration.Generators    source generator for options construction code
  Leander.Configuration.Tool          export contract / documentation / example configuration
```

### Dependency on Leander.Parsing

For now it is a `ProjectReference` to the sibling repository (`..\Leander.Parsing`). When Leander.Parsing 1.0.0 is published it becomes a `PackageReference`. No code change is expected, because the dependency is already through interfaces (`IParser<T>`, `IFormatter<T>`, `IConverter<T>`).

## Core concepts

### Contracts and definitions

A contract is a static class holding definitions as `static readonly` fields.

```csharp
// Illustrative — API not final.
public static class DatabaseConfiguration
{
    public static readonly ConfigurationDefinition<int> CommandTimeout =
        ConfigurationDefinition.Define("Database:CommandTimeout", Converters.Int32)
            .Default(30)
            .Validate(Validators.GreaterThan(0))
            .Describe("Maximum time in seconds allowed for a database command.");

    public static readonly ConfigurationDefinition<string> LogFilePath =
        ConfigurationDefinition.Define("Logging:FilePath", Converters.String)
            .Required()
            .Normalize(Normalizers.FullPath)
            .Validate("PathExists");
}
```

**Definitions are inert data.** This rule makes static initialization safe:

- Building a definition reads no configuration, does no I/O and has no side effects. When the type initializer runs is therefore irrelevant.
- **Builders never throw.** An exception in a static initializer becomes a `TypeInitializationException` and breaks the type for the rest of the process. Problems with the contract itself, such as a default that fails its own validator or an unknown registry key, are reported as diagnostics by a contract check or by the reader.
- Definitions do not reference each other, which avoids cycles between static initializers.

### Definition hierarchy

There is a single class hierarchy with no parallel interfaces:

- `ConfigurationDefinition` (abstract, non-generic): key, value type, description, required, has-default. Converter, validator and normalizer descriptions are to be added when tooling needs them. Tooling enumerates this type. It also hosts the `Define` entry points. (A type named `Configuration` inside the namespace `Leander.Configuration` would break name lookup for consumers.)
- `ConfigurationDefinition<T> : ConfigurationDefinition`: the typed converter, validators and normalizers.

### Keys

- The key separator is `:`, for compatibility with Microsoft configuration.
- Keys are plain keys. The collection form is chosen with a builder method (see Collections).

### The value pipeline

The pipeline for each definition has fixed stages, regardless of the order builder methods are called in:

```
source lookup ──► presence check ──► parse ──► normalize ──► validate ──► value
     (key)        (Required/Default)  (IConverter<T>)  (T → T)     (T → diagnostics)
```

- **Presence.** `null` from the source means *missing*, which is handled by `Required` or `Default`. Any other value, **including `""`**, is passed to the parser. Whether `""` is a valid `int`, `string` or `Uri` is the parser's decision. A required string therefore accepts `""` unless it also has a not-empty validator.
- **Parse.** This uses `IConverter<T>` from Leander.Parsing. Parsing and formatting are closely linked, and formatting is needed outside tooling too, for example to dump the effective configuration.
- **Normalize.** A per-value `T → T` canonicalization, such as making a path absolute. It is deliberately not called `PostConfigure`, which in Microsoft's options system means mutating the whole options object. The parser handles *syntax* (string → T). The normalizer handles *canonicalization* that is independent of how the value was written.
- **Validate.** Zero or more validators run. All of them run, and every failure becomes a diagnostic.

### Converters

- A definition names its converter either as an instance (`Converters.Int32Hex`) or by key (`"Hex"`), resolved from a `ConverterRegistry`.
- Converter keys and descriptions are part of the contract. They appear in exported docs and in error messages.

### Validators

- A validator (`IValidator<T>`, name not final) has a **description** and validates a `T`, producing a failure message or nothing.
- A definition holds any number of validators.
- Validators are referenced either as instances (`Validators.Email`) or **by key** (`"Email"`) through a validator registry, mirroring the converter registry. Keyed resolution allows:
  - **Definition is separate from resolution.** Definitions stay static and inert, and the registry is supplied late, by the reader and possibly from DI.
  - **Environmental validators.** Validators whose outcome depends on the machine (`PathExists`) or that need services can be resolved from DI without touching the static contract.
  - The trade-off is that a misspelled key is caught at read time, as a diagnostic, not at compile time.
- Custom validators are ordinary implementations, registered under a key when needed.

### Normalizers

Normalizers work like validators: they are described, referenced as instances or by key, and registrable.

### Collections

A collection is written as a definition of its **element**, followed by an explicit method that maps it to a list definition:

```csharp
ConfigurationDefinition.Define<string>("Database:ConnectionStrings")
    .Validate("CheckConnection")                 // runs on each element
    .Indexed()                                   // ConfigurationDefinition<string> → ConfigurationDefinition<IReadOnlyList<string>>
    .Validate(Validators.Collections.NotEmpty);  // runs on the list
```

| Method         | Source shape                                                            |
|----------------|-------------------------------------------------------------------------|
| `.Indexed()`   | One entry per element: `Database:ConnectionStrings:0`, `:1`, …          |
| `.Delimited()` | One entry holding a delimited list, e.g. `"a,b,c"` (Leander.Parsing list converter) |

- Everything configured **before** the mapping (converter, normalizers, validators) applies to each element. Everything configured **after** applies to the list.
- The key stays a plain key, with no special syntax.
- `.Indexed()` composes: `.Indexed().Indexed()` reads `Key:0:0`, `Key:0:1`, …
- `.Delimited()` requires a scalar element, i.e. not an already indexed definition.
- The indexed form requires the source to enumerate child keys (see Sources).
- Index names must be integers. Elements are ordered numerically, and gaps (`:0`, `:2`) produce a warning.
- A value in the other form (e.g. `Key:0` exists but the definition is delimited) produces a warning.
- Dictionaries (`Key:Name`) are not designed yet.

### Defaults

Current behaviour: `.Default(value)` is typed. `Required()` and `Default()` are mutually exclusive, and the last one called wins. Defaults go through normalize and validate like any other value. A missing value with neither Required nor Default gives `default(T)` and a trace diagnostic.

Still open: typed defaults (`.Default(30)`, formatted for export via `IFormatter<T>`) vs string defaults (`.Default("30")`, sent through the parser), and how required/default/optional is expressed in the type (`T` vs `T?`).

### Sources

The core depends on a minimal source abstraction, not on `IConfiguration`:

- get a value by key (`null` = missing)
- enumerate child keys of a key (needed for indexed collections)

`Leander.Configuration.Microsoft` implements it over `IConfiguration`.

### Reader

The reader takes a source plus registries, and:

- reads definitions through the pipeline
- collects diagnostics instead of throwing
- exposes `ThrowIfInvalid()` (name not final), which throws a single exception listing every error

```
Configuration is invalid:

  Database:CommandTimeout    'abc' is not a valid integer
  Database:ConnectionString  value is required
  Logging:FilePath           path does not exist
```

### Diagnostics

Every problem is a diagnostic with:

- **severity**: `Error`, `Warning`, `Trace`
- the key and definition it concerns
- a message

Examples: missing required value (error), parse failure (error), validator failure (error), unknown converter/validator key (error), conflicting collection forms (error or warning), a reflection-based fallback being used (trace). Only errors make `ThrowIfInvalid()` throw. Warnings and traces go to a logger or can be inspected in the debugger.

### Parse error messages

`IParser<T>.TryParse` stays `bool`-only: fast, allocation-light, reusable, and without a result type. Messages are built from the **converter description**, e.g. "'abc' is not a valid integer (hex)". If richer errors are needed later, an opt-in interface (e.g. `IDiagnosticParser<T> : IParser<T>`) can be detected with `is`, and plain parsers stay plain.

### Sensitive values

Diagnostics and value dumps may echo raw values (`'Server=…;Password=…' is not valid`). A definition must eventually be able to mark itself sensitive so values are redacted. Deferred.

## Options objects

Options classes are written by the application. Construction is explicit:

```csharp
new DatabaseOptions
{
    CommandTimeout = reader.Get(DatabaseConfiguration.CommandTimeout),
    ConnectionString = reader.Get(DatabaseConfiguration.ConnectionString),
};
```

This is handwritten at first. A source generator may later emit exactly this code. It generates explicit construction, never convention-based mapping, and never invents application types.

## Tooling (later)

Because the non-generic `ConfigurationDefinition` exposes all metadata, a tool can discover contracts and export:

- a contract artifact (build product, tracked in Git, CI can detect drift)
- documentation
- example configuration files

Discovery may use reflection inside the tool, since it is not a runtime path. The contract model must stay independent enough of the C# builder that such export is possible. The export tooling itself is not built yet.

## Follow-ups in Leander.Parsing

- `ConverterRegistry.GetConverter<T>` throws an unhelpful `KeyNotFoundException` when no converter or fallback matches.
- The enum fallback uses reflection (`MakeGenericType`). This is acceptable, but it should be reported as a trace diagnostic, and explicit `Converters.Enum<T>()` should be preferred in definitions.
- Converters need a human-readable description for error messages and exported docs.

## Open questions

- Defaults: typed vs string, and the optional/required type shape.
- Collections: dictionaries.
- Static definition references (`Validators.Email`) vs keyed ones (`"Email"`) — which is primary.
- Sensitive values: API and redaction rules.
- Reload / `IOptionsMonitor` support. v1 reads once at startup.

## Milestones

1. **Core.** Definitions, source abstraction, reader, pipeline (parse / normalize / validate), diagnostics, exception with report, registries for validators and normalizers. Tests. No Microsoft dependency.
2. **Microsoft adapter.** `IConfiguration` source, DI and `IOptions<T>` registration.
3. **Tooling.** Contract export, docs, example configuration.
4. **Generator.** Options construction code.
