# TODO

## Tests
- [x] `ConfigurationContractBuilder`: duplicate keys, resolving primitives (by definition and by name), invalid `Delimited()` use, every failure reported at once.
- [x] `ConfigurationContract.Read` / `TryRead` and `ConfigurationSnapshot`: scalars, required unless default, `Default(null)` / `Default(default)`, `Indexed()` / `Delimited()` collections, list-level rules, diagnostics, exception message, `Get` outside the contract.

## Design
- [ ] Optional values: nullable types (`int?`, `string?`). `Nullable<T>` needs primitive support, and `string?` cannot be told apart from `string` at runtime. Also close the gap where a null default passes on a primitive without rules that reject it.
- [x] Record in DESIGN.md: the primitive's type decides whether null is allowed. A null default on a non-nullable primitive fails, and there's no special handling of null in the pipeline.
- [ ] Opt-in check when reading that the contract and the source are aligned beyond presence, e.g. keys in the source that the contract doesn't know.
- [ ] Opt-in registration through attributes and reflection, to be replaced by a source generator later. Explicit registration stays the default.
- [ ] Built-in error handling with an opt-in/opt-out for every constructor, factory and builder.
- [ ] Polish presence for collections, e.g. what `.Default(x).Indexed()` means.
- [ ] Allow nested `Delimited()` with different delimiters, e.g. `"1,2,3;2,3;1"`. Reject only the same delimiter twice, and `Indexed()` inside `Delimited()`.
- [ ] Opt-in handling of warnings when reading: ignore them, or treat them as errors. Keep the diagnostics model simple.

## Later
- [x] `IConfiguration` adapter: implement `IValueSource` directly (`GetSection(key).Value`, `GetChildren()`).
- [ ] `Uri` formatting: trailing `/` from `AbsoluteUri`, if it matters.
