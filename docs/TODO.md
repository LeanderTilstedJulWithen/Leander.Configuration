# TODO

## Tests
- [x] `ConfigurationContractBuilder`: duplicate keys, resolving primitives (by definition and by name), invalid `Delimited()` use, every failure reported at once.
- [x] `ConfigurationContract.Read` / `TryRead` and `ConfigurationSnapshot`: scalars, required unless default, `Default(null)` / `Default(default)`, `Indexed()` / `Delimited()` collections, list-level rules, diagnostics, exception message, `Get` outside the contract.
- [x] `Optional()`: value and reference types, missing gives `null`, present values go through the primitive, `IsOptional` / `IsRequired`, and the build error for a default with `Optional()` in either order.

## Design
- [x] Optional values: `.Optional()` on configuration definitions, mapping `T` to `T?` (see DESIGN.md, Defaults and presence), with `IsOptional` and contract build errors for a null default and a default with `.Optional()`.
- [x] Record in DESIGN.md how optional values work: explicit `.Optional()` on the configuration definition, not the primitive. Primitives never see null.
- [ ] Opt-in check when reading that the contract and the source are aligned beyond presence, e.g. keys in the source that the contract doesn't know.
- [ ] Opt-in registration through attributes and reflection, to be replaced by a source generator later. Explicit registration stays the default.
- [ ] Built-in error handling with an opt-in/opt-out for every constructor, factory and builder.
- [ ] Presence for collections (see DESIGN.md, Collections). Done: warning for an element default. Left: gaps become `null` for `.Optional().Indexed()`, and a clear build error for `.Optional().Delimited()`.
- [ ] Allow nested `Delimited()` with different delimiters, e.g. `"1,2,3;2,3;1"`. Reject only the same delimiter twice, and `Indexed()` inside `Delimited()`.
- [ ] Opt-in handling of warnings when reading: ignore them, or treat them as errors. Keep the diagnostics model simple.

## Documentation
- [ ] Sensitive values: `.Sensitive()` / `IsSensitive`, carried over by `Indexed()` / `Delimited()` / `Optional()`, and redacted diagnostics (see DESIGN.md, Sensitive values).
- [ ] Contract descriptor in `Leander.Configuration` (see DESIGN.md, Documentation and contract files).
- [ ] `Leander.Configuration.Tooling` project with the Markdown renderer.
- [ ] Contract file: the descriptor as JSON, written and read back.

## Later
- [x] `IConfiguration` adapter: implement `IValueSource` directly (`GetSection(key).Value`, `GetChildren()`).
- [ ] `Uri` formatting: trailing `/` from `AbsoluteUri`, if it matters.
