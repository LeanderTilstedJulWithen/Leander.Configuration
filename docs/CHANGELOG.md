# Changelog

All notable changes to this repository are documented here.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added

#### Leander.Primitives
- `Leander.Primitives.Parsing`: converters, parsers and formatters, copied from Leander.Parsing with only the namespace changed. `IFormatter<T>` is now contravariant (`IFormatter<in T>`).
- `Leander.Primitives.Validation`: `IValidator<in T>` with `Description` and `Validate`, and `Validators` (`Create`, `NotEmpty`, `GreaterThan`, `GreaterThanOrEqual`, `LessThan`, `LessThanOrEqual`, `InRange`, `Collections.NotEmpty`).
- `Leander.Primitives.Normalization`: `INormalizer<T>` with `Description` and `Normalize`, and `Normalizers` (`Create`, `Trim`, `FullPath`).
- `PrimitiveDefinition<T>`: an immutable, fluent definition of a kind of value (name, description, converter, normalizers, validators). A definition without a name is the default for its type.
- `Primitive<T>`: a resolved definition. The converter falls back to the type's default, and normalizers and validators are appended to the default's.
- `Primitive<T>.TryParse` (parse, normalize, validate) and `Primitive<T>.TryAccept` (normalize, validate), each with an overload that returns every error. Primitives can be used without Leander.Configuration.
- `PrimitiveRegistry` / `PrimitiveRegistryBuilder`: primitives keyed by `(type, name)`, with `(type, null)` as the default. `RegisterDefaults()` covers the built-in types, `"Hex"` and `"Local"`. `Build()` reports every unresolvable definition at once.
- `IPrimitiveFallback`, with a built-in enum fallback.

#### Leander.Configuration
- `ConfigurationDefinition<T>`: key, presence (`Required`/`Default`), description, and a primitive given as a definition or a registered name.
- Collections via `Indexed()` (`Key:0`, `Key:1`, …) and `Delimited()` (`"a,b,c"`), with list-level `Validate`/`Normalize` extension methods.
- `ConfigurationContractBuilder` / `ConfigurationContract`: registers primitives and configuration definitions, resolves every definition's primitive, and rejects duplicate keys. `Build()` lists every failure at once, including registered primitives that cannot be resolved.
- `ConfigurationSnapshot`: the validated values of a contract, from `contract.Read(source)` (throws a single `InvalidConfigurationException`) or `contract.TryRead(...)`. A definition without a default is required.
- `ConfigurationDiagnostic` with `DiagnosticSeverity` (`Error`, `Warning`, `Trace`).
- `IValueSource`, `ValueSource.FromPairs` (case-insensitive, last key wins) and `ValueSource.FromDictionary` (uses the dictionary as-is).
- `docs/DESIGN.md`: design overview.

#### Leander.Configuration.MicrosoftExtensions
- `IConfiguration.AsValueSource()`: reads an `IConfiguration` (root or section) as an `IValueSource`, live and without copying.
- `IServiceCollection.AddConfigurationContract(contract, configuration)`: reads the configuration immediately (throwing `InvalidConfigurationException` on failure) and registers the contract and snapshot as singletons. The snapshot is read once and not reloaded.
- `IServiceCollection.AddOptionsFrom<T>(Func<ConfigurationSnapshot, T>)`: exposes `T` as `IOptions<T>`, `IOptionsSnapshot<T>` and `IOptionsMonitor<T>` through a factory, so options can be immutable records.

### Notes
- Leander.Configuration no longer references the sibling Leander.Parsing repository. It depends on Leander.Primitives instead.