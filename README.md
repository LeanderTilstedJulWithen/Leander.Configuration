# Leander.Configuration

.NET libraries for treating configuration as an explicit, typed contract.

- **Leander.Primitives** describes kinds of values: how they are parsed, normalized and validated.
- **Leander.Configuration** describes configuration keys and reads them into a validated snapshot.
- **Leander.Configuration.MicrosoftExtensions** reads an `IConfiguration` as a source and exposes options as `IOptions<T>`.
- **Leander.Configuration.Tooling** renders a contract as Markdown documentation, a JSON contract file and an example configuration, and compares contracts.

## Motivation

Applications collect configuration keys that nobody fully knows about: what they mean, what type they are, which values are valid, and what happens when they are missing. The Microsoft binder converts strings to objects by convention, and a bad value often only shows up when it is first used.

Here every key is declared in C#, and the declarations are the single source of truth:

- **Every problem at once.** Reading a source checks all keys and reports every problem in one go, at startup.
- **Rules belong to kinds of values.** A port is an `int` between 1 and 65535, wherever it is used. Declare it once as a primitive and reuse it.
- **Presence is explicit.** A key is required, has a default, or is optional, and the type says which: `int` or `int?`.
- **Secrets stay secret.** A sensitive key's value never appears in diagnostics or documentation.
- **Documentation from the code.** The contract renders as Markdown, as a JSON contract file that can be committed and compared, and as an example `appsettings.json`.
- **No new infrastructure.** Sources, providers, dependency injection and hosting stay with Microsoft.Extensions. Only the binding step is replaced.

```
Configuration is invalid:

  Server:Port              must be between 1 and 65535
  Server:RequestTimeout    '30s' is not a valid TimeSpan
  Server:MaxConnections    must be greater than 0
  Server:AllowedOrigins:1  'not a uri' is not a valid Uri
  Server:Features          value is required
  Admin:Email              must contain @
  Admin:BackupEmail        must contain @
  Logging:Verbosity        'Chatty' is not a valid Verbosity
```

## Primitives

> A primitive is a named rule for a value that crosses a text boundary: how it's parsed and formatted, what its canonical form is, and what makes it valid.

An `int` is not a port, and a `string` is not an e-mail address. A primitive gives the same underlying type its own rules, without a wrapper type:

```csharp
public static readonly Primitive<int> Port = new("Port", Converters.Int32)
{
    Validators = [Validators.InRange(1, 65535)],
    Description = "A TCP port.",
};

Port.TryParse("99999", out var port, out var errors);
// false, errors: ["must be between 1 and 65535"]
```

Every primitive has a name and a converter, and nothing is looked up by type: the code that uses a primitive refers to it directly. Ready-made primitives cover the common types (`Primitive.String`, `Primitive.Int32`, `Primitive.TimeSpan`, `Primitive.Enum<T>()`, …). A derived primitive adds rules to its base under a new name, and can never remove them:

```csharp
public static readonly Primitive<int> UnprivilegedPort = new("UnprivilegedPort", Port)
{
    Validators = [Validators.GreaterThanOrEqual(1024)], // runs after Port's rules
};
```

A list is a primitive too. Each item goes through the element primitive with all its rules, then the list's own rules run on the list:

```csharp
public static readonly ListPrimitive<Uri> Origins = new("Origins", Primitive.Uri) // delimiter ',' by default
{
    Validators = [Validators.Collections.NotEmpty<Uri>()],
};

Origins.TryParse("https://a.example, not a uri", out var origins, out var errors);
// false, errors: ["item 1: 'not a uri' is not a valid Uri"]
```

The converter formats values back as well, so a rule's text uses the same form the value is written in: a Hex primitive says `must be less than or equal to 0xFF`, not `255`.

Primitives has no dependency on Leander.Configuration. Anything that turns text into values can use it: configuration, command-line arguments, query strings or files.

## Configuration

A configuration definition names a key, its primitive, whether it's required, has a default or is optional, and a description:

```csharp
public static readonly ConfigurationDefinition<int> Port =
    ConfigurationDefinition.Define("Server:Port", SamplePrimitives.Port)
        .Default(8080)
        .Describe("The port to listen on.");

public static readonly ConfigurationDefinition<IReadOnlyList<Uri>> AllowedOrigins =
    ConfigurationDefinition.Indexed("Server:AllowedOrigins", SamplePrimitives.Origins)
        .Describe("Origins allowed to call the server.");

public static readonly ConfigurationDefinition<string?> BackupEmail =
    ConfigurationDefinition.Define("Admin:BackupEmail", SamplePrimitives.Email)
        .Optional()
        .Describe("Where operational alerts are also sent, if set.");
```

A definition without a default is required. `Optional()` turns `T` into `T?`: a missing value is `null` instead of an error, and a present value still goes through the primitive's rules. `Sensitive()` keeps the value out of diagnostics (`value is not a valid Int32`) and documentation.

With a list primitive, `Define(key, list)` reads one delimited entry (`"a,b,c"`), and `Indexed(key, list)` reads one entry per item (`Key:0`, `Key:1`, …), which is what a JSON array becomes.

Definitions are registered in a contract, built once at startup. `Build()` fails if a key is defined twice, two different primitives of the same type share a name, or a default doesn't fit (`null` on a non-optional key, or any default on an optional one). Reading a source gives a snapshot that is already validated:

```csharp
var contract = new ConfigurationContractBuilder()
    .Register(ServerConfiguration.Port)
    .Register(ServerConfiguration.AllowedOrigins)
    .Register(ServerConfiguration.BackupEmail)
    .Build();

var snapshot = contract.Read(source); // throws InvalidConfigurationException listing every problem
int port = snapshot.Get(ServerConfiguration.Port);
string? backupEmail = snapshot.Get(ServerConfiguration.BackupEmail);
```

A source is anything implementing `IValueSource`. `ValueSource.FromPairs` and `ValueSource.FromDictionary` cover plain key/value data. `TryRead` returns the diagnostics instead of throwing.

A misspelled key isn't wrong by itself: `Server:Prot` is ignored and `Server:Port` falls back to its default. `ReadOptions` checks the sections the application owns for keys the contract doesn't define, and can treat warnings as errors:

```csharp
var options = new ReadOptions { CheckedSections = ["Server", "Admin"], WarningsAsErrors = true };
contract.Read(source, options); // Server:Prot  is not in the configuration contract
```

## Microsoft.Extensions

`AsValueSource()` reads any `IConfiguration` live, the host's or a section of it:

```csharp
var snapshot = contract.Read(configuration.AsValueSource());
```

With a host, `AddConfigurationContract` registers the contract and its snapshot. The snapshot is read from the host's `IConfiguration` when the host starts, so an invalid configuration fails `StartAsync` before anything runs. Options are constructed by your own code, from the snapshot:

```csharp
builder.Services
    .AddConfigurationContract(contract, new() { ReadOptions = options })    // StartAsync throws InvalidConfigurationException
    .AddSingleton(provider => ServerOptions.FromSnapshot(provider.GetRequiredService<ConfigurationSnapshot>())) // ServerOptions itself
    .AddOptionsFrom(CorsOptions.FromSnapshot);                               // IOptions<CorsOptions>
```

The snapshot is read once and never reloads, so resolving `IOptionsSnapshot<T>` or `IOptionsMonitor<T>` of an options type from `AddOptionsFrom` throws instead of quietly returning values that never change. The singleton is our default, not a rule: an application can register a scoped snapshot that reads `IConfiguration` again in every scope.

## Tooling

The contract describes itself. Documentation, a contract file and an example configuration are rendered from the same descriptor:

```csharp
var descriptor = contract.CreateDescriptor();
File.WriteAllText("configuration.md", Documentation.WriteMarkdown(descriptor, "Server configuration"));
File.WriteAllText("configuration.contract.json", ContractSerializer.WriteJson(descriptor));
File.WriteAllText("appsettings.example.json", ConfigurationGenerator.WriteJson(descriptor));
```

The documentation lists every key with its type, presence, default, form and rules, grouped by the first key segment. Each key is documented in full, including the format and rules of the primitive it uses.

The contract file is JSON and descriptive: validators are code, so it can't be run. Commit it, and a change to the configuration contract shows up in review. `ContractDiff.Compare(committed, current)` says what changed, per key, e.g. `Server:Port: validators of Int32 (Port): [must be between 1 and 65535] → [must be between 1024 and 65535]`. It also compares two programs that share configuration. Programs that share configuration share the C# definitions, not the file.

The example configuration is an `appsettings.json` with every key, to copy and fill in. Defaults are written as they are, and keys without one get a placeholder such as `"<Port>"` or `"<optional Email>"`. Sensitive values are always `"<secret>"`.

## Samples

- [Leander.Primitives.Sample](https://github.com/LeanderTilstedJulWithen/Leander.Configuration/tree/main/samples/Leander.Primitives.Sample): primitives on their own. Ready-made, custom, derived and list primitives, parsing and formatting, and a primitive wrapping another.
- [Leander.Configuration.Sample](https://github.com/LeanderTilstedJulWithen/Leander.Configuration/tree/main/samples/Leander.Configuration.Sample): a contract read from key/value pairs, with every problem reported and a strict read that catches a misspelled key.
- [Leander.Configuration.MicrosoftExtensions.Sample](https://github.com/LeanderTilstedJulWithen/Leander.Configuration/tree/main/samples/Leander.Configuration.MicrosoftExtensions.Sample): reading an `IConfiguration`, a scoped snapshot, and a host with options with and without `IOptions<T>`.
- [Leander.Configuration.Tooling.Sample](https://github.com/LeanderTilstedJulWithen/Leander.Configuration/tree/main/samples/Leander.Configuration.Tooling.Sample): documentation, contract file and example configuration, and a comparison with the committed contract file. Its output is committed, so it can be read without running anything: [configuration.md](https://github.com/LeanderTilstedJulWithen/Leander.Configuration/blob/main/samples/Leander.Configuration.Tooling.Sample/output/configuration.md), [configuration.contract.json](https://github.com/LeanderTilstedJulWithen/Leander.Configuration/blob/main/samples/Leander.Configuration.Tooling.Sample/output/configuration.contract.json) and [appsettings.example.json](https://github.com/LeanderTilstedJulWithen/Leander.Configuration/blob/main/samples/Leander.Configuration.Tooling.Sample/output/appsettings.example.json).

## Planned

After 1.0.0:

- **Reloading:** `IOptionsSnapshot<T>` and `IOptionsMonitor<T>` from a snapshot read again when `IConfiguration` reloads.
- **Registration without listing every definition:** opt-in, through attributes first and a source generator later. Explicit registration stays the default.

## License

[MIT](https://github.com/LeanderTilstedJulWithen/Leander.Configuration/blob/main/LICENSE)
