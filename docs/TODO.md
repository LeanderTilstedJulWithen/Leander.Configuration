# TODO

## Queued
Decided and set in motion. Pick up here, in this order.

- [x] Commit the explicit primitives work (registry removed, `Primitive<T>` constructors, deriving, ready-made primitives).
- [x] List primitives (see DESIGN.md, List primitives): `ListPrimitive<T> : Primitive<IReadOnlyList<T>>` with `Element`, `Delimiter` (constructor argument, default `,`) and list rules; `Primitive<T>` unsealed with `private protected` hooks; items through the element primitive, then list rules; item errors as `item 2: …`, redacted too; deriving with `new ListPrimitive<T>(name, baseList)`.
- [x] Configuration collections (see DESIGN.md, Collections): `ConfigurationDefinition.Indexed(key, list)`; `Define(key, list)` reads one delimited entry. Remove `.Indexed()`, `.Delimited()`, `DelimitedReader`, `ConfigurationDefinitionExtensions` (list-level `Validate`/`Normalize`), element-default warnings, the `Delimited()` scalar check, and `.Indexed().Indexed()`.
- [x] Descriptor and Markdown for list primitives: `PrimitiveDescriptor.Element` / `Delimiter`, `ValueForm` down to Scalar | Indexed, `ValueDescriptor` without `Delimiter`, `Element` and list rules; escape `<` and `>` in Markdown text. Then remove the "Status" notes in DESIGN.md.
- [x] Samples, README and CHANGELOG for list primitives.
- [x] Tests for Leander.Configuration.MicrosoftExtensions: `AsValueSource` (values, child names, sections), `AddConfigurationContract` (with and without `ReadOptions`, the exception on invalid configuration), and `AddOptionsFrom` (`IOptions<T>`, `IOptionsSnapshot<T>`, `IOptionsMonitor<T>`).
- [x] XML documentation comments (`///`) for the public API instead of `//` comments, so users get them in IntelliSense. `GenerateDocumentationFile` in every src project, so a missing comment is a CS1591 warning.
- [x] Run both samples and check their output. Fix documentation drift, e.g. DESIGN.md (Sources) still says "`Leander.Configuration.Microsoft` will implement `IValueSource`", which is done as Leander.Configuration.MicrosoftExtensions.

## Review (2026-10-02)
From REVIEW.md. In this order.

### Tooling
- [x] Contract JSON owned by Tooling: an internal DTO is the only place that defines the format, with plain serializer options. Flat shape: `key`, `description`, `type`, `primitive` (name only; nullability shows in `presence`), `presence`, `default`, `form` (always), `sensitive` (only when true). `FormatVersion` stays 1 while pre-release.
- [x] Indexed defaults in `default` as a JSON array instead of a separate `defaultItems`.
- [x] Public API by output, formats internal: `Documentation.WriteMarkdown`, `ConfigurationGenerator` (from `ExampleConfiguration`), a new name for `ContractFile` (it doesn't touch files) with `WriteJson` / `ReadJson`, and `ContractComparison` → `ContractDiff`.

### Leander.Configuration.MicrosoftExtensions
- [x] Nothing throws at registration: the snapshot is read from `IConfiguration` resolved from the container, and an explicitly registered startup validator fails the host at start. An options parameter for setup, no builder. Singleton is our default, not a rule. Record in DESIGN.md.

### Markdown documentation
- [x] Rules shown on the key that uses them, including element and list rules.
- [x] Folding: a primitive used by one entity is folded into it; one used by several gets its own entry, with "Used by" links to keys and derived primitives.
- [x] Converters describe how they parse, in a simple sentence (see DESIGN.md, Converter descriptions): `IConverter<T>.Description`, ready-made and builder descriptions, composition.
- [x] ~~Rules shown on the key, and folding~~: replaced. Rules live on primitives: a key shows its type (linked), presence, form and sensitivity, and every primitive with something to say has one section with its own parts, sorted by type (see DESIGN.md, Documentation).
- [x] ~~Markdown output is still a bit flawed: every list and every bound needs its own named primitive.~~ Resolved by treating primitives as an implementation detail (below). Parameterized primitives stay out of scope (see IDEAS.md, Parameterized primitives).
- [ ] Primitives are an implementation detail: everything folded into the key (see DESIGN.md, Documentation). No Primitives section, no links. Type is the display name, and the primitive's description isn't shown. Format, values and rules are collected along the base chain, and a list's items get a nested block.
- [x] Converter descriptions in the documentation: `PrimitiveDescriptor.Converter` (null for a derived primitive), a `converter` field in the contract JSON, the Markdown **Format** line (from the base for a derived primitive; counts as something to say), and a `ContractDiff` aspect.

### Samples
- [ ] Leander.Primitives sample (standalone).
- [ ] Leander.Configuration.MicrosoftExtensions sample without Tooling.
- [ ] Comprehensive sample covering everything, writing the contract JSON, Markdown and appsettings.json to `samples/output` (committed).

### README
- [ ] Sections named after projects without the prefix (Primitives, Configuration, …); Microsoft.Extensions in its own section.
- [ ] "Why" → "Motivation", with a bit more on features; tighten clunky passages ("sharing one is sharing a field").
- [ ] Samples section linking the samples and `samples/output`; a small Planned section (e.g. `IOptionsMonitor`), separate from TODO.md and DESIGN.md.

### Clean-up
- [ ] Repo-wide code clean-up for member order (until then, per class when touched): consts; static fields; readonly fields; fields; constructors (public, internal, private); public static members; public properties; internal properties; public methods; internal methods; private methods; nested classes. Primary constructors where possible. Alphabetical within a group, over logical pairs.

## Tests
- [x] Rewrite the tests removed with the registry: `Primitive<T>` constructors, deriving (base rules first, own rules only in `Validators`), ready-made and enum primitives, display names, list primitives, the contract builder (name clashes, duplicate keys, presence), and reading (`ConfigurationContractTests`).
- [x] `ConfigurationContractBuilder`: duplicate keys, resolving primitives (by definition and by name), invalid `Delimited()` use, every failure reported at once.
- [x] `ConfigurationContract.Read` / `TryRead` and `ConfigurationSnapshot`: scalars, required unless default, `Default(null)` / `Default(default)`, `Indexed()` / `Delimited()` collections, list-level rules, diagnostics, exception message, `Get` outside the contract.
- [x] `Optional()`: value and reference types, missing gives `null`, present values go through the primitive, `IsOptional` / `IsRequired`, and the build error for a default with `Optional()` in either order.

## Design
- [x] Optional values: `.Optional()` on configuration definitions, mapping `T` to `T?` (see DESIGN.md, Defaults and presence), with `IsOptional` and contract build errors for a null default and a default with `.Optional()`.
- [x] Record in DESIGN.md how optional values work: explicit `.Optional()` on the configuration definition, not the primitive. Primitives never see null.
- [x] ~~Nested `Delimited()` with different delimiters~~: covered by list primitives with a list primitive as element. The same delimiter twice is the user's bug and isn't checked.
- [x] Opt-in check when reading that the contract and the source are aligned beyond presence, e.g. keys in the source that the contract doesn't know: `ReadOptions.CheckedSections` (see DESIGN.md, Read options).
- [x] Opt-in handling of warnings when reading: `ReadOptions.WarningsAsErrors`. Ignoring needs no option: warnings never block.

## Documentation
- [x] Sensitive values: `.Sensitive()` / `IsSensitive`, carried over by `Indexed()` / `Delimited()` / `Optional()`, and redacted diagnostics (see DESIGN.md, Sensitive values).
- [x] Contract descriptor in `Leander.Configuration` (see DESIGN.md, Documentation and contract files).
- [x] `Leander.Configuration.Tooling` project with the Markdown renderer.
- [x] Contract file: the descriptor as JSON, written and read back.
- [x] Tests for the descriptor, the Markdown renderer and the contract file round trip.
- [x] `ContractFile.Read` failed on a definition or primitive without a description, and checked the format version only after reading the whole file. Found by `ContractFileTests`.
- [x] Example configuration (`appsettings.json`) rendered from the descriptor, with placeholders for sensitive values.
- [x] Compare two contract descriptors, e.g. two programs sharing keys, or the committed file against the current contract.

## 1.1.0
Planned after 1.0.0, not before.

- [ ] Opt-in registration through attributes and reflection, to be replaced by a source generator later. Explicit registration stays the default. Don't assume definitions are public static members: applications may declare them differently.
- [ ] Reloading: `IOptionsSnapshot<T>` and `IOptionsMonitor<T>` from a snapshot read again on `IConfiguration` reload. Design first (see DESIGN.md, Open questions, Reload). Related: reading a section of the contract (see IDEAS.md).

## Later
- [x] `IConfiguration` adapter: implement `IValueSource` directly (`GetSection(key).Value`, `GetChildren()`).
- [x] `Uri` formatting: `OriginalString` instead of `AbsoluteUri`, so `https://example.com` formats without a trailing `/`.
