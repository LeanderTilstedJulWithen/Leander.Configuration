using Leander.Primitives;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Leander.Configuration.MicrosoftExtensions.Tests;

public class ServiceCollectionExtensionsTests
{
    private static readonly ConfigurationDefinition<int> Port =
        ConfigurationDefinition.Define("Server:Port", Primitive.Int32).Default(80);

    private static readonly ConfigurationContract Contract =
        new ConfigurationContractBuilder().Register(Port).Build();

    private sealed class ServerOptions
    {
        public required int Port { get; init; }
    }

    private static IConfigurationRoot Configuration(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(pair => KeyValuePair.Create(pair.Key, pair.Value)))
            .Build();

    private sealed class FixedOptions(ServerOptions value) : IOptionsSnapshot<ServerOptions>
    {
        private readonly ServerOptions _value = value;

        public ServerOptions Value => _value;

        public ServerOptions Get(string? name) => _value;
    }

    // The configuration is in the container, as in a host.
    private static IServiceCollection Services(IConfiguration configuration, ConfigurationContractOptions? options = null) =>
        new ServiceCollection()
            .AddSingleton(configuration)
            .AddConfigurationContract(Contract, options);

    private static ServiceProvider Provider(IConfiguration configuration, Action<IServiceCollection>? then = null)
    {
        var services = Services(configuration)
            .AddOptionsFrom(snapshot => new ServerOptions { Port = snapshot.Get(Port) });

        then?.Invoke(services);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void AddConfigurationContract_RegistersContractAndSnapshot()
    {
        using var provider = Services(Configuration(("Server:Port", "8080"))).BuildServiceProvider();

        Assert.Same(Contract, provider.GetRequiredService<ConfigurationContract>());
        var snapshot = provider.GetRequiredService<ConfigurationSnapshot>();
        Assert.Equal(8080, snapshot.Get(Port));
        Assert.Same(snapshot, provider.GetRequiredService<ConfigurationSnapshot>());
    }

    [Fact]
    public void AddConfigurationContract_InvalidConfiguration_ThrowsOnResolve()
    {
        using var provider = Services(Configuration(("Server:Port", "abc"))).BuildServiceProvider();

        var exception = Assert.Throws<InvalidConfigurationException>(() =>
            provider.GetRequiredService<ConfigurationSnapshot>());

        var error = Assert.Single(exception.Diagnostics);
        Assert.Equal("Server:Port", error.Key);
    }

    [Fact]
    public void AddConfigurationContract_InvalidConfiguration_FailsStartupValidation()
    {
        using var provider = Services(Configuration(("Server:Port", "abc"))).BuildServiceProvider();

        Assert.Throws<InvalidConfigurationException>(() => provider.GetRequiredService<IStartupValidator>().Validate());
    }

    [Fact]
    public void AddConfigurationContract_KeepsOtherStartupValidation()
    {
        using var provider = Services(Configuration(("Server:Port", "8080")))
            .AddOptions<ServerOptions>()
            .Validate(_ => false, "other")
            .ValidateOnStart()
            .Services
            .BuildServiceProvider();

        var exception = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IStartupValidator>().Validate());
        Assert.Equal("other", Assert.Single(exception.Failures));
    }

    [Fact]
    public void AddConfigurationContract_WithoutValidateOnStart_HasNoStartupValidation()
    {
        using var provider = Services(Configuration(("Server:Port", "abc")), new ConfigurationContractOptions { ValidateOnStart = false })
            .BuildServiceProvider();

        Assert.Null(provider.GetService<IStartupValidator>());
    }

    [Fact]
    public void AddConfigurationContract_ReadsOnce()
    {
        var configuration = Configuration(("Server:Port", "8080"));
        using var provider = Services(configuration).BuildServiceProvider();

        Assert.Equal(8080, provider.GetRequiredService<ConfigurationSnapshot>().Get(Port));
        configuration["Server:Port"] = "9090";

        Assert.Equal(8080, provider.GetRequiredService<ConfigurationSnapshot>().Get(Port));
    }

    [Fact]
    public void AddConfigurationContract_WithoutOptions_ChecksNothing()
    {
        using var provider = Services(Configuration(("Server:Prot", "8080"))).BuildServiceProvider();

        Assert.Empty(provider.GetRequiredService<ConfigurationSnapshot>().Diagnostics);
    }

    [Fact]
    public void AddConfigurationContract_WithReadOptions_PassesThemToRead()
    {
        var options = new ConfigurationContractOptions { ReadOptions = new ReadOptions { CheckedSections = ["Server"] } };
        using var provider = Services(Configuration(("Server:Prot", "8080")), options).BuildServiceProvider();

        var warning = Assert.Single(provider.GetRequiredService<ConfigurationSnapshot>().Diagnostics);
        Assert.Equal("Server:Prot", warning.Key);
    }

    [Fact]
    public void AddConfigurationContract_WarningsAsErrors_ThrowsOnResolve()
    {
        var options = new ConfigurationContractOptions
        {
            ReadOptions = new ReadOptions { CheckedSections = ["Server"], WarningsAsErrors = true },
        };
        using var provider = Services(Configuration(("Server:Prot", "8080")), options).BuildServiceProvider();

        Assert.Throws<InvalidConfigurationException>(() => provider.GetRequiredService<ConfigurationSnapshot>());
    }

    [Fact]
    public void AddOptionsFrom_IOptions_IsBuiltFromSnapshot()
    {
        using var provider = Provider(Configuration(("Server:Port", "8080")));

        Assert.Equal(8080, provider.GetRequiredService<IOptions<ServerOptions>>().Value.Port);
    }

    [Fact]
    public void AddOptionsFrom_IOptions_IsBuiltOnce()
    {
        var created = 0;
        using var provider = Services(Configuration(("Server:Port", "8080")))
            .AddOptionsFrom(snapshot =>
            {
                created++;
                return new ServerOptions { Port = snapshot.Get(Port) };
            })
            .BuildServiceProvider();

        Assert.Same(
            provider.GetRequiredService<IOptions<ServerOptions>>().Value,
            provider.GetRequiredService<IOptions<ServerOptions>>().Value);
        Assert.Equal(1, created);
    }

    [Fact]
    public void AddOptionsFrom_DoesNotReload()
    {
        var configuration = Configuration(("Server:Port", "8080"));
        using var provider = Provider(configuration);

        Assert.Equal(8080, provider.GetRequiredService<IOptions<ServerOptions>>().Value.Port);
        configuration["Server:Port"] = "9090";
        configuration.Reload();

        Assert.Equal(8080, provider.GetRequiredService<IOptions<ServerOptions>>().Value.Port);
    }

    // AddOptions() registers the open generic IOptionsSnapshot<> and IOptionsMonitor<>, as a host does.
    [Fact]
    public void AddOptionsFrom_IOptionsSnapshot_Throws()
    {
        using var provider = Provider(Configuration(("Server:Port", "8080")), services => services.AddOptions());
        using var scope = provider.CreateScope();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<ServerOptions>>());

        Assert.StartsWith("IOptionsSnapshot<ServerOptions> is not supported", exception.Message);
        Assert.EndsWith("Use IOptions<ServerOptions>.", exception.Message);
    }

    [Fact]
    public void AddOptionsFrom_IOptionsMonitor_Throws()
    {
        using var provider = Provider(Configuration(("Server:Port", "8080")), services => services.AddOptions());

        var exception = Assert.Throws<InvalidOperationException>(() =>
            provider.GetRequiredService<IOptionsMonitor<ServerOptions>>());

        Assert.StartsWith("IOptionsMonitor<ServerOptions> is not supported", exception.Message);
    }

    [Fact]
    public void AddOptionsFrom_OwnRegistrationBefore_IsKept()
    {
        var own = new FixedOptions(new ServerOptions { Port = 1 });
        using var provider = new ServiceCollection()
            .AddScoped<IOptionsSnapshot<ServerOptions>>(_ => own)
            .AddSingleton<IConfiguration>(Configuration(("Server:Port", "8080")))
            .AddConfigurationContract(Contract)
            .AddOptionsFrom(snapshot => new ServerOptions { Port = snapshot.Get(Port) })
            .BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.Same(own, scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<ServerOptions>>());
    }

    [Fact]
    public void AddOptionsFrom_OwnRegistrationAfter_IsUsed()
    {
        var own = new FixedOptions(new ServerOptions { Port = 1 });
        using var provider = Provider(
            Configuration(("Server:Port", "8080")),
            services => services.AddScoped<IOptionsSnapshot<ServerOptions>>(_ => own));
        using var scope = provider.CreateScope();

        Assert.Same(own, scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<ServerOptions>>());
    }

    [Fact]
    public void AddOptionsFrom_WithoutContract_FailsOnResolve()
    {
        using var provider = new ServiceCollection()
            .AddOptionsFrom(snapshot => new ServerOptions { Port = snapshot.Get(Port) })
            .BuildServiceProvider();

        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IOptions<ServerOptions>>().Value);
    }
}
