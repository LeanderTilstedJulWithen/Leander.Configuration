# Leander.Configuration

.NET libraries for treating configuration as an explicit, typed contract.

- **Leander.Primitives** describes kinds of values: how they are parsed, normalized and validated.
- **Leander.Configuration** describes configuration keys and reads them into a validated snapshot.
- **Leander.Configuration.MicrosoftExtensions** reads an `IConfiguration` as a source and exposes options as `IOptions<T>`.

## Why

Applications collect configuration keys that nobody fully knows about: what they mean, what type they are, which values are valid, and what happens when they are missing. The Microsoft binder converts strings to objects by convention, and a bad value often only shows up when it is first used.

Here every key is declared in C#. Reading a source checks all keys at once and reports every problem in one go:

```
Configuration is invalid:

  Server:Port              must be between 1 and 65535
  Server:RequestTimeout    '30s' is not a valid TimeSpan
  Server:AllowedOrigins:1  'not a uri' is not a valid Uri
  Server:Features          value is required
  Admin:Email              must contain @
  Logging:Verbosity        'Chatty' is not a valid Verbosity
```

## Leander.Primitives

> A primitive is a named rule for a value that crosses a text boundary: how it's parsed and formatted, what its canonical form is, and what makes it valid.

An `int` is not a port, and a `string` is not an e-mail address. A primitive gives the same underlying type its own rules, without a wrapper type:

```csharp
public static readonly PrimitiveDefinition<int> Port =
    PrimitiveDefinition.Define<int>("Port")
        .Validate(Validators.InRange(1, 65535))
        .Describe("A TCP port.");

var primitives = new PrimitiveRegistryBuilder()
    .RegisterDefaults()
    .Register(Port)
    .Build();

primitives.Get<int>("Port").TryParse("99999", out var port, out var errors);
// false, errors: ["must be between 1 and 65535"]
```

Primitives has no dependency on Leander.Configuration. Anything that turns text into values can use it: configuration, command-line arguments, query strings or files.

## Leander.Configuration

A configuration definition names a key, its primitive, whether it has a default, and a description:

```csharp
public static readonly ConfigurationDefinition<int> Port =
    ConfigurationDefinition.Define("Server:Port", SamplePrimitives.Port)
        .Default(8080)
        .Describe("The port to listen on.");

public static readonly ConfigurationDefinition<IReadOnlyList<Uri>> AllowedOrigins =
    ConfigurationDefinition.Define<Uri>("Server:AllowedOrigins")
        .Indexed()
        .Validate(Validators.Collections.NotEmpty)
        .Describe("Origins allowed to call the server.");
```

Definitions are registered in a contract, built once at startup. `Build()` fails if a primitive cannot be resolved or a key is defined twice. Reading a source gives a snapshot that is already validated:

```csharp
var contract = new ConfigurationContractBuilder()
    .RegisterDefaultPrimitives()
    .Register(ServerConfiguration.Port)
    .Register(ServerConfiguration.AllowedOrigins)
    .Build();

var snapshot = contract.Read(configuration.AsValueSource()); // throws InvalidConfigurationException listing every problem
int port = snapshot.Get(ServerConfiguration.Port);
```

Leander.Configuration replaces only the binding step. Sources, providers, dependency injection and hosting stay with Microsoft.Extensions. A source is anything implementing `IValueSource`. Leander.Configuration.MicrosoftExtensions reads an `IConfiguration` with `AsValueSource()`, and `ValueSource.FromPairs` and `ValueSource.FromDictionary` cover plain key/value data.

With a host, the configuration is read before the host is built, and options are constructed by your own code:

```csharp
builder.Services
    .AddConfigurationContract(contract, builder.Configuration) // throws InvalidConfigurationException listing every problem
    .AddOptionsFrom(ServerOptions.From);                       // IOptions<ServerOptions>
```

See [samples/Leander.Configuration.Sample](samples/Leander.Configuration.Sample) for a complete example, and [samples/Leander.Configuration.MicrosoftExtensions.Sample](samples/Leander.Configuration.MicrosoftExtensions.Sample) for a host with `IOptions<T>`.
