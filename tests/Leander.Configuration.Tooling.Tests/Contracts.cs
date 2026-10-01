using Leander.Configuration.Descriptors;
using Leander.Primitives;
using Leander.Primitives.Parsing;
using Leander.Primitives.Validation;

namespace Leander.Configuration.Tooling.Tests;

// Contracts and descriptors shared by the renderer tests.
internal static class Contracts
{
    public static readonly Primitive<int> Port = new("Port", Converters.Int32)
    {
        Validators = [Validators.InRange(1, 65535)],
        Description = "A TCP port.",
    };

    public static readonly ListPrimitive<Uri> Origins = new("Origins", Primitive.Uri)
    {
        Validators = [Validators.Collections.NotEmpty<Uri>()],
    };

    public static ContractDescriptor Describe(params ConfigurationDefinition[] definitions)
    {
        var builder = new ConfigurationContractBuilder();
        foreach (var definition in definitions)
        {
            builder.Register(definition);
        }

        return builder.Build().CreateDescriptor();
    }

    // A contract using every feature the renderers show.
    public static ContractDescriptor Server() => Describe(
        ConfigurationDefinition.Define("Server:Host", Primitive.String).Default("localhost").Describe("The host name."),
        ConfigurationDefinition.Define("Server:Port", Port).Describe("The port to listen on."),
        ConfigurationDefinition.Define("Server:MaxConnections", Primitive.Int32).Optional(),
        ConfigurationDefinition.Indexed("Server:AllowedOrigins", Origins),
        ConfigurationDefinition.Define("Server:Features", new ListPrimitive<string>("Features", Primitive.String)).Default([]),
        ConfigurationDefinition.Define("Database:Password", Primitive.String).Default("hunter2").Sensitive());
}
