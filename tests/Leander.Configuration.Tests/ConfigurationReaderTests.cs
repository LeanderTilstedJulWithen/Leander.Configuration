using static Leander.Configuration.Tests.TestHelpers;

namespace Leander.Configuration.Tests;

public class ConfigurationReaderTests
{
    private static class DatabaseConfiguration
    {
        public static readonly ConfigurationDefinition<int> CommandTimeout =
            ConfigurationDefinition.Define<int>("Database:CommandTimeout")
                .Default(30)
                .Validate(Validators.GreaterThan(0));

        public static readonly ConfigurationDefinition<string> ConnectionString =
            ConfigurationDefinition.Define<string>("Database:ConnectionString")
                .Required();

        public static readonly ConfigurationDefinition<IReadOnlyList<int>> Ports =
            ConfigurationDefinition.Define<int>("Database:Ports")
                .Validate(Validators.InRange(1, 65535))
                .Indexed();
    }

    [Fact]
    public void ThrowIfInvalid_ReportsEveryErrorAtOnce()
    {
        var reader = Reader(("Database:CommandTimeout", "abc"), ("Database:Ports:0", "0"));

        reader.Get(DatabaseConfiguration.CommandTimeout);
        reader.Get(DatabaseConfiguration.ConnectionString);
        reader.Get(DatabaseConfiguration.Ports);

        var exception = Assert.Throws<InvalidConfigurationException>(reader.ThrowIfInvalid);

        Assert.Equal(
            """
            Configuration is invalid:

              Database:CommandTimeout    'abc' is not a valid Int32
              Database:ConnectionString  value is required
              Database:Ports:0           must be between 1 and 65535
            """.ReplaceLineEndings(),
            exception.Message);
        Assert.Equal(3, exception.Diagnostics.Count);
    }

    [Fact]
    public void ThrowIfInvalid_WarningsOnly_DoesNotThrow()
    {
        var definition = ConfigurationDefinition.Define<int>("Ports").Indexed();
        var reader = Reader(("Ports:0", "80"), ("Ports:5", "85"));

        reader.Get(definition);

        Assert.False(reader.HasErrors);
        reader.ThrowIfInvalid();
    }

    [Fact]
    public void ThrowIfInvalid_ValidConfiguration_DoesNotThrow()
    {
        var reader = Reader(("Database:ConnectionString", "Server=."), ("Database:Ports:0", "5432"));

        var options = new
        {
            Timeout = reader.Get(DatabaseConfiguration.CommandTimeout),
            ConnectionString = reader.Get(DatabaseConfiguration.ConnectionString),
            Ports = reader.Get(DatabaseConfiguration.Ports),
        };

        reader.ThrowIfInvalid();
        Assert.Equal(30, options.Timeout);
        Assert.Equal("Server=.", options.ConnectionString);
        Assert.Equal([5432], options.Ports);
    }
}
