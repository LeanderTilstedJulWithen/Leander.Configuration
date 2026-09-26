# TODO

## Tests
- [x] `ConfigurationContractBuilder`: duplicate keys, resolving primitives (by definition and by name), invalid `Delimited()` use, every failure reported at once.
- [x] `ConfigurationContract.Read` / `TryRead` and `ConfigurationSnapshot`: scalars, required unless default, `Default(null)` / `Default(default)`, `Indexed()` / `Delimited()` collections, list-level rules, diagnostics, exception message, `Get` outside the contract.

## Design
- [ ] Optional values: `.Optional()` on configuration definitions, mapping `T` to `T?` (see DESIGN.md, Defaults and presence). Done: value types, `IsOptional`, and the contract build errors for a null default and a default with `.Optional()`. Left: reference types.
- [x] Record in DESIGN.md how optional values work: explicit `.Optional()` on the configuration definition, not the primitive. Primitives never see null.
- [ ] Opt-in check when reading that the contract and the source are aligned beyond presence, e.g. keys in the source that the contract doesn't know.
- [ ] Opt-in registration through attributes and reflection, to be replaced by a source generator later. Explicit registration stays the default.
- [ ] Built-in error handling with an opt-in/opt-out for every constructor, factory and builder.
- [ ] Polish presence for collections, e.g. what `.Default(x).Indexed()` means.
- [ ] Allow nested `Delimited()` with different delimiters, e.g. `"1,2,3;2,3;1"`. Reject only the same delimiter twice, and `Indexed()` inside `Delimited()`.
- [ ] Opt-in handling of warnings when reading: ignore them, or treat them as errors. Keep the diagnostics model simple.

## Later
- [x] `IConfiguration` adapter: implement `IValueSource` directly (`GetSection(key).Value`, `GetChildren()`).
- [ ] `Uri` formatting: trailing `/` from `AbsoluteUri`, if it matters.
