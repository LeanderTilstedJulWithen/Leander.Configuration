# Leander.Configuration

.NET libraries for treating configuration as an explicit, typed contract.

- **Leander.Primitives** describes kinds of values: how they are parsed, normalized and validated.
- **Leander.Configuration** describes configuration keys and reads them into a validated snapshot.
- **Leander.Configuration.MicrosoftExtensions** reads an `IConfiguration` as a source and exposes options as `IOptions<T>`.
- **Leander.Configuration.Tooling** renders a contract as Markdown documentation and as a JSON contract file.

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
public static readonly Primitive<int> Port =
    Primitive.Create("Port", Converters.Int32)
        .Validate(Validators.InRange(1, 65535))
        .Describe("A TCP port.");

Port.TryParse("99999", out var port, out var errors);
// false, errors: ["must be between 1 and 65535"]
```

A primitive is always explicit and complete: sharing one is sharing a field. Ready-made primitives cover the common types (`Primitive.String`, `Primitive.Int32`, `Primitive.TimeSpan`, `Primitive.Enum<T>()`, …), and `Primitive.DeriveFrom("AdminPort", Port)` adds rules under a new name.

Primitives has no dependency on Leander.Configuration. Anything that turns text into values can use it: configuration, command-line arguments, query strings or files.

## Leander.Configuration

A configuration definition names a key, its primitive, whether it's required, has a default or is optional, and a description:

```csharp
public static readonly ConfigurationDefinition<int> Port =
    ConfigurationDefinition.Define("Server:Port", SamplePrimitives.Port)
        .Default(8080)
        .Describe("The port to listen on.");

public static readonly ConfigurationDefinition<IReadOnlyList<Uri>> AllowedOrigins =
    ConfigurationDefinition.Define("Server:AllowedOrigins", Primitive.Uri)
        .Indexed()
        .Validate(Validators.Collections.NotEmpty)
        .Describe("Origins allowed to call the server.");

public static readonly ConfigurationDefinition<string?> BackupEmail =
    ConfigurationDefinition.Define("Admin:BackupEmail", SamplePrimitives.Email)
        .Optional()
        .Describe("Where operational alerts are also sent, if set.");
```

A definition without a default is required. `Optional()` turns `T` into `T?`: a missing value is `null` instead of an error, and a present value still goes through the primitive's rules.

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

With a host, the configuration is read before the host is built, and options are constructed by your own code:

```csharp
builder.Services
    .AddConfigurationContract(contract, builder.Configuration) // throws InvalidConfigurationException listing every problem
    .AddOptionsFrom(ServerOptions.From);                       // IOptions<ServerOptions>
```

A secret is marked with `Sensitive()`: its value never appears in diagnostics (`value is not a valid Int32`), and documentation never shows its default.

## Documentation and contract files

The contract describes itself. Documentation and a contract file are rendered from the same descriptor:

```csharp
var descriptor = contract.CreateDescriptor();
File.WriteAllText("configuration.md", MarkdownDocumentation.Write(descriptor, "Server configuration"));
File.WriteAllText("configuration.contract.json", ContractFile.Write(descriptor));
```

The documentation lists every key with its type, presence, default, form and rules, grouped by the first key segment. Named primitives are described once and linked from the keys that use them.

The contract file is JSON and descriptive: validators are code, so it can't be run. Commit it, and a change to the configuration contract shows up in review. Programs that share configuration share the C# definitions, not the file.

See [samples/Leander.Configuration.Sample](samples/Leander.Configuration.Sample) for a complete example, and [samples/Leander.Configuration.MicrosoftExtensions.Sample](samples/Leander.Configuration.MicrosoftExtensions.Sample) for a host with `IOptions<T>`.
