# TODO

## Queued
Decided and set in motion. Pick up here, in this order.

- [ ] Commit the explicit primitives work (registry removed, `Primitive<T>` constructors, deriving, ready-made primitives). Not committed as of 2026-09-28.
- [ ] List primitives (see DESIGN.md, List primitives): `ListPrimitive<T> : Primitive<IReadOnlyList<T>>` with `Element`, `Delimiter` (default `,`) and list rules; `Primitive<T>` unsealed with a non-public constructor for subclasses; items through the element primitive, then list rules; item errors as `item 2: …`, redacted too; deriving with `new ListPrimitive<T>(name, baseList)`.
- [ ] Configuration collections (see DESIGN.md, Collections): `ConfigurationDefinition.Indexed(key, list)`; `Define(key, list)` reads one delimited entry. Remove `.Indexed()`, `.Delimited()`, `DelimitedReader`, `ConfigurationDefinitionExtensions` (list-level `Validate`/`Normalize`), element-default warnings, the `Delimited()` scalar check, and `.Indexed().Indexed()`.
- [ ] Descriptor and Markdown for list primitives: `PrimitiveDescriptor.Element` / `Delimiter`, `ValueForm` down to Scalar | Indexed, `ValueDescriptor` without `Delimiter`, `Element` and list rules; escape `<` and `>` in Markdown text. Then remove the "Status" notes in DESIGN.md.
- [ ] Samples, README and CHANGELOG for list primitives.

## Tests
- [ ] Rewrite the tests removed with the registry: `Primitive<T>` constructors, deriving (base rules first, own rules only in `Validators`), ready-made and enum primitives, display names, list primitives, the contract builder (name clashes, duplicate keys, presence), and reading (`ConfigurationContractTests`).
- [x] `ConfigurationContractBuilder`: duplicate keys, resolving primitives (by definition and by name), invalid `Delimited()` use, every failure reported at once.
- [x] `ConfigurationContract.Read` / `TryRead` and `ConfigurationSnapshot`: scalars, required unless default, `Default(null)` / `Default(default)`, `Indexed()` / `Delimited()` collections, list-level rules, diagnostics, exception message, `Get` outside the contract.
- [x] `Optional()`: value and reference types, missing gives `null`, present values go through the primitive, `IsOptional` / `IsRequired`, and the build error for a default with `Optional()` in either order.

## Design
- [x] Optional values: `.Optional()` on configuration definitions, mapping `T` to `T?` (see DESIGN.md, Defaults and presence), with `IsOptional` and contract build errors for a null default and a default with `.Optional()`.
- [x] Record in DESIGN.md how optional values work: explicit `.Optional()` on the configuration definition, not the primitive. Primitives never see null.
- [ ] Opt-in check when reading that the contract and the source are aligned beyond presence, e.g. keys in the source that the contract doesn't know.
- [ ] Opt-in registration through attributes and reflection, to be replaced by a source generator later. Explicit registration stays the default.
- [ ] Built-in error handling with an opt-in/opt-out for every constructor, factory and builder.
- [ ] Optional items in indexed lists: a gap in the indices becomes `null`. Not designed for list primitives yet (see DESIGN.md, Collections).
- [x] ~~Nested `Delimited()` with different delimiters~~: covered by list primitives with a list primitive as element. The same delimiter twice is the user's bug and isn't checked.
- [ ] Opt-in handling of warnings when reading: ignore them, or treat them as errors. Keep the diagnostics model simple.

## Documentation
- [x] Sensitive values: `.Sensitive()` / `IsSensitive`, carried over by `Indexed()` / `Delimited()` / `Optional()`, and redacted diagnostics (see DESIGN.md, Sensitive values).
- [x] Contract descriptor in `Leander.Configuration` (see DESIGN.md, Documentation and contract files).
- [x] `Leander.Configuration.Tooling` project with the Markdown renderer.
- [x] Contract file: the descriptor as JSON, written and read back.
- [ ] Tests for the descriptor, the Markdown renderer and the contract file round trip.
- [ ] Example configuration (`appsettings.json`) rendered from the descriptor, with placeholders for sensitive values.
- [ ] Compare two contract descriptors, e.g. two programs sharing keys, or the committed file against the current contract.
- [ ] Command-line tool (`Leander.Configuration.Tool`) that finds the contract in an assembly.

## Later
- [x] `IConfiguration` adapter: implement `IValueSource` directly (`GetSection(key).Value`, `GetChildren()`).
- [ ] `Uri` formatting: trailing `/` from `AbsoluteUri`, if it matters.
