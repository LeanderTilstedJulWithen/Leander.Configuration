# Changelog

All notable changes to this repository are documented here.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added

#### Leander.Primitives
- `Leander.Primitives.Parsing`: converters, parsers and formatters, copied from Leander.Parsing with only the namespace changed. `IFormatter<T>` is now contravariant (`IFormatter<in T>`).
- `Leander.Primitives.Validation`: `IValidator<in T>` with `Description` and `Validate`, and `Validators` (`Create`, `NotEmpty`, `GreaterThan`, `GreaterThanOrEqual`, `LessThan`, `LessThanOrEqual`, `InRange`, `Collections.NotEmpty`).
- `Leander.Primitives.Normalization`: `INormalizer<T>` with `Description` and `Normalize`, and `Normalizers` (`Create`, `Trim`, `FullPath`).
- `Primitive<T>`: an immutable, always complete kind of value, built with `new(name, converter) { Normalizers = …, Validators = …, Description = … }`. The name is required. Primitives are explicit: there is no registry and no default per type, and sharing one is sharing a field.
- Deriving with `new(name, base) { … }`: takes the base's converter, and its own rules are added to the base's, which always run first. Rules can't be removed. `Base` refers to the base.
- Ready-made primitives for the built-in types, named after their type (`Primitive.String`, `Primitive.Int32`, `Primitive.TimeSpan`, …), the variants `Primitive.Int32Hex`, `Primitive.UInt32Hex` ("Hex") and `Primitive.DateTimeLocal` ("Local"), and `Primitive.Enum<T>()`, named after the enum type.
- `Primitive<T>.TryParse` (parse, normalize, validate) and `Primitive<T>.TryAccept` (normalize, validate), each with an overload that returns every error. Primitives can be used without Leander.Configuration.

#### Leander.Configuration
- `ConfigurationDefinition<T>`: key, presence (`Required`/`Default`), description, and its primitive: `ConfigurationDefinition.Define(key, primitive)`.
- `Optional()`: `ConfigurationDefinition<T>` becomes `ConfigurationDefinition<T?>` (`Nullable<T>` for value types, annotated `T?` for reference types), and a missing value is `null` instead of an error. `ConfigurationDefinition.IsOptional` reports it. The contract build rejects a null default on a non-optional definition, and a default combined with `Optional()`.
- `Sensitive()`: diagnostics for the definition never contain its value, e.g. `value is not a valid Int32` instead of `'abc' is not a valid Int32`, and exception messages from normalizers and validators are left out. `ConfigurationDefinition.IsSensitive` reports it, and it carries over to `Indexed()`, `Delimited()` and `Optional()`.
- Collections via `Indexed()` (`Key:0`, `Key:1`, …) and `Delimited()` (`"a,b,c"`), with list-level `Validate`/`Normalize` extension methods.
- `ConfigurationContractBuilder` / `ConfigurationContract`: registers configuration definitions, and rejects duplicate keys and two different primitives of the same type with the same name. `Build()` lists every failure at once.
- `ConfigurationContract.CreateDescriptor()`: a text-only description of the contract (`Leander.Configuration.Descriptors`): every definition with its type, presence, formatted default, form, elements and rules, and the primitives it uses with their bases, each with its own rules. Defaults of sensitive definitions are left out.
- `ConfigurationSnapshot`: the validated values of a contract, from `contract.Read(source)` (throws a single `InvalidConfigurationException`) or `contract.TryRead(...)`. A definition without a default is required.
- `ConfigurationDiagnostic` with `DiagnosticSeverity` (`Error`, `Warning`, `Trace`).
- `IValueSource`, `ValueSource.FromPairs` (case-insensitive, last key wins) and `ValueSource.FromDictionary` (uses the dictionary as-is).
- `docs/DESIGN.md`: design overview.

#### Leander.Configuration.MicrosoftExtensions
- `IConfiguration.AsValueSource()`: reads an `IConfiguration` (root or section) as an `IValueSource`, live and without copying.
- `IServiceCollection.AddConfigurationContract(contract, configuration)`: reads the configuration immediately (throwing `InvalidConfigurationException` on failure) and registers the contract and snapshot as singletons. The snapshot is read once and not reloaded.
- `IServiceCollection.AddOptionsFrom<T>(Func<ConfigurationSnapshot, T>)`: exposes `T` as `IOptions<T>`, `IOptionsSnapshot<T>` and `IOptionsMonitor<T>` through a factory, so options can be immutable records.

#### Leander.Configuration.Tooling
- `MarkdownDocumentation.Write(descriptor, title)`: documentation grouped by the first key segment, with a summary table per group, a section per key, and the primitives described once and linked, with the primitive they derive from. Primitives with nothing to say, such as `String`, are left out.
- `ContractFile.Write(descriptor)` / `ContractFile.Read(json)`: the descriptor as versioned JSON. The file is descriptive: it can be committed and compared, but not run.

### Notes
- Leander.Configuration no longer references the sibling Leander.Parsing repository. It depends on Leander.Primitives instead.