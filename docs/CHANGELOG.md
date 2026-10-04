# Changelog

All notable changes to this repository are documented here.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added

#### Leander.Primitives
- `Leander.Primitives.Parsing`: converters, parsers and formatters, copied from Leander.Parsing. `IFormatter<T>` is now contravariant (`IFormatter<in T>`).
- Conversion errors: `IParser<T>.TryParse(input, out result, out errors)` returns every reason a value can't be read, as `IFormattableText<string>`, so pieces of the input can be hidden for sensitive values; the bool-only `TryParse` is a default interface member. The primitive leads with its own line: `'1024..70000' is not a valid Range<Int32> (PortRange): maximum: must be between 1 and 65535`. List and dictionary converters report every failing item or entry (`item 2: …`, `entry 1: value: …`).
- `Primitive<T>.AsConverter()`: the whole primitive as a converter, with its rules, for a converter that wraps it. Its errors become the outer primitive's reasons.
- `Leander.Primitives.Validation`: `IValidator<T>` with `Description`, `Validate` (every failure, empty when valid) and `IsValid`, and `Validators` (`Create`, `NotEmpty`, `GreaterThan`, `GreaterThanOrEqual`, `LessThan`, `LessThanOrEqual`, `InRange`, `Collections.NotEmpty<T>()`). Validators are invariant, and may report several failures.
- `Leander.Primitives.Normalization`: `INormalizer<T>` with `Description` and `Normalize`, and `Normalizers` (`Create`, `Trim`, `FullPath`, `UpperBound`, `LowerBound`).
- `IFormattableText<T>` and `FormattableText.Create`: rule descriptions and failures, formatted by the primitive with its converter in documentation and error messages, e.g. `must be less than or equal to 0xFF` on a Hex primitive. For sensitive values, every value of `T` in them is hidden.
- `Primitive<T>`: an immutable, always complete kind of value, built with `new(name, converter) { Normalizers = …, Validators = …, Description = … }`. The name is required. Primitives are explicit: there is no registry and no default per type, and sharing one is sharing a field.
- Deriving with `new(name, base) { … }`: takes the base's converter, and its own rules are added to the base's, which always run first. Rules can't be removed. `Base` refers to the base.
- Ready-made primitives for the built-in types, named after their type (`Primitive.String`, `Primitive.Int32`, `Primitive.TimeSpan`, …), the variants `Primitive.Int32Hex`, `Primitive.UInt32Hex` ("Hex") and `Primitive.DateTimeLocal` ("Local"), and `Primitive.Enum<T>()`, named after the enum type.
- `Primitive<T>.TryParse` (parse, normalize, validate) and `Primitive<T>.TryAccept` (normalize, validate), each with an overload that returns every error. Primitives can be used without Leander.Configuration.
- `ListPrimitive<T>`: a `Primitive<IReadOnlyList<T>>` read from one delimited string, with `Element` and `Delimiter` (a constructor argument, `,` by default). Each item goes through the element primitive with all its rules, then the list's own rules run on the list. Item errors name the item, counted from 0 (`item 2: 'x' is not a valid Uri`). `new ListPrimitive<T>(name, baseList)` derives a list, keeping its element and delimiter.
- `IConverter<T>.Description`: what text a converter reads and writes, in one sentence for documentation, e.g. "A 32-bit integer."; `null` by default. Ready-made converters have one, the date and time builders generate one from their formats (`WithDescription` replaces it), and list and dictionary converters compose theirs from their items'.

#### Leander.Configuration
- `ConfigurationDefinition<T>`: key, presence (`Required`/`Default`), description, and its primitive: `ConfigurationDefinition.Define(key, primitive)`.
- `Optional()`: `ConfigurationDefinition<T>` becomes `ConfigurationDefinition<T?>` (`Nullable<T>` for value types, annotated `T?` for reference types), and a missing value is `null` instead of an error. `ConfigurationDefinition.IsOptional` reports it. The contract build rejects a null default on a non-optional definition, and a default combined with `Optional()`.
- `Sensitive()`: diagnostics for the definition never contain its value, e.g. `value is not a valid Int32` instead of `'abc' is not a valid Int32`, and exception messages from normalizers and validators are left out. `ConfigurationDefinition.IsSensitive` reports it, and it carries over to `Optional()`.
- Collections through list primitives: `ConfigurationDefinition.Define(key, list)` reads one delimited entry (`"a,b,c"`), and `ConfigurationDefinition.Indexed(key, list)` reads `Key:0`, `Key:1`, …
- `ConfigurationContractBuilder` / `ConfigurationContract`: registers configuration definitions, and rejects duplicate keys and two different primitives of the same type with the same name. `Build()` lists every failure at once.
- `ConfigurationContract.CreateDescriptor()`: a text-only description of the contract (`Leander.Configuration.Descriptors`): every definition with its type, presence, formatted default, form (scalar or indexed), the items of an indexed default, and rules, and the primitives it uses with their bases, each with its own rules and converter description, and for a list its element and delimiter. Defaults of sensitive definitions are left out.
- `ConfigurationSnapshot`: the validated values of a contract, from `contract.Read(source)` (throws a single `InvalidConfigurationException`) or `contract.TryRead(...)`. A definition without a default is required.
- `ConfigurationDiagnostic` with `DiagnosticSeverity` (`Error`, `Warning`, `Trace`).
- `ReadOptions` for `Read` / `TryRead`: `CheckedSections` reports keys under the named sections that the contract doesn't define, as warnings, and `WarningsAsErrors` makes every warning an error.
- `IValueSource`, `ValueSource.FromPairs` (case-insensitive, last key wins) and `ValueSource.FromDictionary` (uses the dictionary as-is).
- `docs/DESIGN.md`: design overview.

#### Leander.Configuration.MicrosoftExtensions
- `IConfiguration.AsValueSource()`: reads an `IConfiguration` (root or section) as an `IValueSource`, live and without copying.
- `IServiceCollection.AddConfigurationContract(contract[, options])`: registers the contract and snapshot as singletons, without reading anything. The snapshot is read from the container's `IConfiguration` once and not reloaded. `ConfigurationContractOptions` holds the `ReadOptions` and `ValidateOnStart` (on by default), which reads the snapshot when the host starts, so an invalid configuration fails `StartAsync` with `InvalidConfigurationException`.
- `IServiceCollection.AddOptionsFrom<T>(Func<ConfigurationSnapshot, T>)`: exposes `T` as `IOptions<T>`, built once from the snapshot, so options can be immutable records. The snapshot never reloads, so `IOptionsSnapshot<T>` and `IOptionsMonitor<T>` of that `T` throw on resolve, unless the application registers its own.

#### Leander.Configuration.Tooling
- `Documentation.WriteMarkdown(descriptor, title)`: documentation grouped by the first key segment, with a summary table per group and a section per key with everything about it: its type, form, format, values and rules (collected along the primitive's base chain), items, presence and sensitivity.
- `ContractSerializer.WriteJson(descriptor)` / `ContractSerializer.ReadJson(json)`: the descriptor as versioned JSON. The file is descriptive: it can be committed and compared, but not run.
- `ConfigurationGenerator.WriteJson(descriptor)`: an `appsettings.json`-style example with every key, to copy and fill in. Values are strings as the converter formats them, indexed lists are JSON arrays, keys without a default get a placeholder (`"<Port>"`, `"<optional Email>"`), and sensitive values are always `"<secret>"`.
- `ContractDiff.Compare(left, right)`: what differs between two contract descriptors, per key, as `ContractDifference`s: `Added`, `Removed`, or `Changed` with the aspect (type, presence, default, form, sensitive, description, primitive and its rules, bases and elements) and both values. Keys match case-insensitively.

### Notes
- Leander.Configuration no longer references the sibling Leander.Parsing repository. It depends on Leander.Primitives instead.
- The public API of every package has XML documentation comments, shipped with the packages for IntelliSense.