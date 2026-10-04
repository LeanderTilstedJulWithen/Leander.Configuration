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
- [x] Primitives are an implementation detail: everything folded into the key (see DESIGN.md, Documentation). No Primitives section, no links. Type is the display name, and the primitive's description isn't shown. Format, values and rules are collected along the base chain, and a list's items get a nested block.
- [x] Converter descriptions in the documentation: `PrimitiveDescriptor.Converter` (null for a derived primitive), a `converter` field in the contract JSON, the Markdown **Format** line (from the base for a derived primitive; counts as something to say), and a `ContractDiff` aspect.

### Samples
- [x] Leander.Primitives sample (standalone).
- [x] Leander.Configuration sample without Tooling: the documentation output moves to the comprehensive sample. The Leander.Configuration.MicrosoftExtensions sample already doesn't use Tooling.
- [x] ~~Comprehensive sample covering everything~~: a Tooling sample instead, whose contract uses everything the documentation can show. It compares the committed contract file with the current contract, then writes the contract JSON, Markdown and example appsettings.json to its own `output` folder (committed).

### README
- [x] Sections named after projects without the prefix (Primitives, Configuration, …); Microsoft.Extensions in its own section.
- [x] "Why" → "Motivation", with a bit more on features; tighten clunky passages ("sharing one is sharing a field").
- [x] Samples section linking the samples and their committed output (the Tooling sample's `output` folder, not `samples/output`); a small Planned section (e.g. `IOptionsMonitor`), separate from TODO.md and DESIGN.md.

### Clean-up
- [x] Repo-wide code clean-up for member order (from then on, per class when touched): consts; static fields; readonly fields; fields; constructors (public, internal, private); public static members; public properties; internal properties; protected and private protected properties; private properties; public methods; internal methods; protected and private protected methods; private methods; nested types. Internal and private static methods sit with the instance methods of the same access; abstract and override members count by their access. Doc comments and comments on a group move with their member. Primary constructors where possible. Alphabetical within a group, over logical pairs. In tests, `[Fact]` and `[Theory]` methods keep their order, which reads from the simple case to the edge cases.

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

## 1.0.0
What stands between the current code and a release. The code is feature-complete; these freeze the API and ship it.

### Decide first (breaking after 1.0.0)
- [x] Conversion errors from converters: the converter's `TryParse` outputs its errors as `IFormattableText<string>`, the primitive leads with its own line, the bool-only `TryParse` stays as a default interface member, and `Primitive<T>.AsConverter()` for wrapping (see DESIGN.md, Conversion errors).
- [x] Implement conversion errors: `IParser<T>`, the ready-made converters, builders and composite converters, `PrimitiveConverter<T>` behind `AsConverter()`, the primitive's lead line and redaction, and the `RangePrimitive` sample. `ListPrimitive` keeps its `TryConvert` hook, so list errors don't repeat the list before every item.
- [x] Type names in contract files: the shortest name without collisions in the contract, `Billing.Status` and `Shipping.Status`; `ContractDiff` matches type names by suffix (see DESIGN.md, Type names).
- [x] Implement type names: naming in the descriptor (value types, primitive types, generic arguments, nested types), the build error for types with the same full name, the Markdown type, and suffix matching in `ContractDiff`, primitive references included.
- [x] Validators organisation: one entry point per kind of rule, rules for one kind of value in nested classes (see DESIGN.md, Leander.Primitives).
- [x] Implement the organisation: `Validators.NotEmpty` → `Validators.Strings.NotEmpty`, `Normalizers.Trim` / `FullPath` → `Normalizers.Strings`; update samples, README, DESIGN.md examples and CHANGELOG.
- [x] Replacing the converter in a derived primitive: not allowed, because a converter carries implicit rules (see DESIGN.md, Deriving).
- [x] DESIGN.md, Milestones: milestone 5 (Generator) moved after the release.

### Ship
- [x] Package metadata in a shared `src/Directory.Build.props`: version, authors, license, repository URL, tags, and the README as package readme; a description per project.
- [x] LICENSE file: MIT.
- [ ] CI: build and test on push. Optionally regenerate the Tooling sample's output and fail when it changes.
- [ ] CHANGELOG: `[Unreleased]` becomes `[1.0.0]`, possibly reset to a short first-release summary.

## 1.1.0
Planned after 1.0.0, not before.

- [ ] Opt-in registration through attributes and reflection, to be replaced by a source generator later. Explicit registration stays the default. Don't assume definitions are public static members: applications may declare them differently.
- [ ] Reloading: `IOptionsSnapshot<T>` and `IOptionsMonitor<T>` from a snapshot read again on `IConfiguration` reload. Design first (see DESIGN.md, Open questions, Reload). Related: reading a section of the contract (see IDEAS.md).

## Later
- [x] `IConfiguration` adapter: implement `IValueSource` directly (`GetSection(key).Value`, `GetChildren()`).
- [x] `Uri` formatting: `OriginalString` instead of `AbsoluteUri`, so `https://example.com` formats without a trailing `/`.
