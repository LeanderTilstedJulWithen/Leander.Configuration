# Leander.Configuration

.NET libraries for treating configuration as an explicit, typed contract.

- **Leander.Primitives** describes kinds of values: how they are parsed, normalized and validated.
- **Leander.Configuration** describes configuration keys and reads them into a validated snapshot.
- **Leander.Configuration.MicrosoftExtensions** reads an `IConfiguration` as a source and exposes options as `IOptions<T>`.
- **Leander.Configuration.Tooling** renders a contract as Markdown documentation, a JSON contract file and an example configuration, and compares contracts.

## Why

Applications collect configuration keys that nobody fully knows about: what they mean, what type they are, which values are valid, and what happens when they are missing. The Microsoft binder converts strings to objects by convention, and a bad value often only shows up when it is first used.

Here every key is declared in C#. Reading a source checks all keys at once and reports every problem in one go:

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

## Leander.Primitives

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

A primitive is always explicit and complete, with a name and a converter: sharing one is sharing a field. Ready-made primitives cover the common types (`Primitive.String`, `Primitive.Int32`, `Primitive.TimeSpan`, `Primitive.Enum<T>()`, …). `new("AdminPort", Port) { Validators = [...] }` derives a primitive: it adds rules to Port's under a new name, and can never remove them.

A list is a primitive too. Each item goes through the element primitive with all its rules, then the list's own rules run on the list:

```csharp
public static readonly ListPrimitive<Uri> Origins = new("Origins", Primitive.Uri) // delimiter ',' by default
{
    Validators = [Validators.Collections.NotEmpty<Uri>()],
};

Origins.TryParse("https://a.example, not a uri", out var origins, out var errors);
// false, errors: ["item 1: 'not a uri' is not a valid Uri"]
```

Primitives has no dependency on Leander.Configuration. Anything that turns text into values can use it: configuration, command-line arguments, query strings or files.

## Leander.Configuration

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

A definition without a default is required. `Optional()` turns `T` into `T?`: a missing value is `null` instead of an error, and a present value still goes through the primitive's rules.

With a list primitive, `Define(key, list)` reads one delimited entry (`"a,b,c"`), and `Indexed(key, list)` reads one entry per item (`Key:0`, `Key:1`, …), as a JSON array becomes.

Definitions are registered in a contract, built once at startup. `Build()` fails if a key is defined twice, two different primitives of the same type share a name, or a default doesn't fit (`null` on a non-optional key, or any default on an optional one). Reading a source gives a snapshot that is already validated:

```csharp
var contract = new ConfigurationContractBuilder()
    .Register(ServerConfiguration.Port)
    .Register(ServerConfiguration.AllowedOrigins)
    .Register(ServerConfiguration.BackupEmail)
    .Build();

var snapshot = contract.Read(configuration.AsValueSource()); // throws InvalidConfigurationException listing every problem
int port = snapshot.Get(ServerConfiguration.Port);
string? backupEmail = snapshot.Get(ServerConfiguration.BackupEmail);
```

Leander.Configuration replaces only the binding step. Sources, providers, dependency injection and hosting stay with Microsoft.Extensions. A source is anything implementing `IValueSource`. Leander.Configuration.MicrosoftExtensions reads an `IConfiguration` with `AsValueSource()`, and `ValueSource.FromPairs` and `ValueSource.FromDictionary` cover plain key/value data.

With a host, the configuration is read from the host's `IConfiguration` when the host starts, and options are constructed by your own code:

```csharp
builder.Services
    .AddConfigurationContract(contract)  // StartAsync throws InvalidConfigurationException listing every problem
    .AddOptionsFrom(ServerOptions.From); // IOptions<ServerOptions>
```

A misspelled key isn't wrong by itself: `Server:Prot` is ignored and `Server:Port` falls back to its default. `ReadOptions` checks the sections the application owns for keys the contract doesn't define, and can treat warnings as errors:

```csharp
var options = new ReadOptions { CheckedSections = ["Server", "Admin"], WarningsAsErrors = true };
contract.Read(source, options);                                         // Server:Prot  is not in the configuration contract
builder.Services.AddConfigurationContract(contract, new() { ReadOptions = options });
```

A secret is marked with `Sensitive()`: its value never appears in diagnostics (`value is not a valid Int32`), and documentation never shows its default.

## Documentation and contract files

The contract describes itself. Documentation, a contract file and an example configuration are rendered from the same descriptor:

```csharp
var descriptor = contract.CreateDescriptor();
File.WriteAllText("configuration.md", Documentation.WriteMarkdown(descriptor, "Server configuration"));
File.WriteAllText("configuration.contract.json", ContractSerializer.WriteJson(descriptor));
File.WriteAllText("appsettings.example.json", ConfigurationGenerator.WriteJson(descriptor));
```

The documentation lists every key with its type, presence, default, form and rules, grouped by the first key segment. Each key is documented in full, including the rules of the primitive it uses.

The contract file is JSON and descriptive: validators are code, so it can't be run. Commit it, and a change to the configuration contract shows up in review. `ContractDiff.Compare(committed, current)` says what changed, per key, e.g. `Server:Port: validators of Int32 (Port): [must be between 1 and 65535] → [must be between 1024 and 65535]`. It also compares two programs that share configuration. Programs that share configuration share the C# definitions, not the file.

The example configuration is an `appsettings.json` with every key, to copy and fill in. Defaults are written as they are, and keys without one get a placeholder such as `"<Port>"` or `"<optional Email>"`. Sensitive values are always `"<secret>"`.

See [samples/Leander.Primitives.Sample](samples/Leander.Primitives.Sample) for primitives on their own, [samples/Leander.Configuration.Sample](samples/Leander.Configuration.Sample) for reading a contract, and [samples/Leander.Configuration.MicrosoftExtensions.Sample](samples/Leander.Configuration.MicrosoftExtensions.Sample) for a host with `IOptions<T>`.
