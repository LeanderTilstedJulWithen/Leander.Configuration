using Leander.Primitives;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Leander.Configuration.MicrosoftExtensions.Tests;

public class ReloadTests
{
    private static readonly ConfigurationDefinition<IReadOnlyList<string>> Hosts =
        ConfigurationDefinition.Indexed("Server:Hosts", new ListPrimitive<string>("Hosts", Primitive.String)).Default([]);

    private static readonly ConfigurationDefinition<int> Port =
        ConfigurationDefinition.Define("Server:Port", Primitive.Int32).Default(80);

    private static readonly ConfigurationDefinition<int> Timeout =
        ConfigurationDefinition.Define("Database:Timeout", Primitive.Int32).Default(30);

    // After the definitions, which it needs when initialized.
    private static readonly ConfigurationContract Contract =
        new ConfigurationContractBuilder().Register(Port).Register(Hosts).Register(Timeout).Build();

    [Fact]
    public void Monitor_WithoutReload_KeepsStartupSnapshot()
    {
        var configuration = Configuration(("Server:Port", "8080"));
        using var provider = Provider(configuration, reload: false);
        var monitor = provider.GetRequiredService<ConfigurationSnapshotMonitor>();

        Reload(configuration, ("Server:Port", "9090"));

        Assert.False(monitor.ReloadsOnChange);
        Assert.Same(provider.GetRequiredService<ConfigurationSnapshot>(), monitor.Current);
        Assert.Equal(8080, monitor.Current.Get(Port));
    }

    [Fact]
    public void Monitor_StartsWithTheStartupSnapshot()
    {
        using var provider = Provider(Configuration(("Server:Port", "8080")));

        Assert.Same(
            provider.GetRequiredService<ConfigurationSnapshot>(),
            provider.GetRequiredService<ConfigurationSnapshotMonitor>().Current);
    }

    [Fact]
    public void Monitor_Reload_ReadsNewSnapshot()
    {
        var configuration = Configuration(("Server:Port", "8080"));
        using var provider = Provider(configuration);
        var monitor = provider.GetRequiredService<ConfigurationSnapshotMonitor>();

        Reload(configuration, ("Server:Port", "9090"));

        Assert.True(monitor.ReloadsOnChange);
        Assert.Equal(9090, monitor.Current.Get(Port));
    }

    [Fact]
    public void Monitor_Reload_KeepsStartupSnapshotService()
    {
        var configuration = Configuration(("Server:Port", "8080"));
        using var provider = Provider(configuration);
        var startup = provider.GetRequiredService<ConfigurationSnapshot>();

        Reload(configuration, ("Server:Port", "9090"));

        Assert.Same(startup, provider.GetRequiredService<ConfigurationSnapshot>());
        Assert.Equal(8080, provider.GetRequiredService<ConfigurationSnapshot>().Get(Port));
    }

    [Fact]
    public void Monitor_OnChange_IsCalledWithTheNewSnapshot()
    {
        var configuration = Configuration(("Server:Port", "8080"));
        using var provider = Provider(configuration);
        var monitor = provider.GetRequiredService<ConfigurationSnapshotMonitor>();
        var changes = new List<ConfigurationSnapshot>();
        monitor.OnChange(changes.Add);

        Reload(configuration, ("Server:Port", "9090"));

        var change = Assert.Single(changes);
        Assert.Same(monitor.Current, change);
        Assert.Equal(9090, change.Get(Port));
    }

    [Fact]
    public void Monitor_ReloadWithoutChange_KeepsSnapshot()
    {
        var configuration = Configuration(("Server:Port", "8080"), ("Server:Hosts:0", "a"));
        using var provider = Provider(configuration);
        var monitor = provider.GetRequiredService<ConfigurationSnapshotMonitor>();
        var before = monitor.Current;
        var changes = 0;
        monitor.OnChange(_ => changes++);

        configuration.Reload();

        Assert.Same(before, monitor.Current);
        Assert.Equal(0, changes);
    }

    [Fact]
    public void Monitor_ChangeOutsideTheContract_KeepsSnapshot()
    {
        var configuration = Configuration(("Server:Port", "8080"), ("Logging:LogLevel:Default", "Information"));
        using var provider = Provider(configuration);
        var monitor = provider.GetRequiredService<ConfigurationSnapshotMonitor>();
        var before = monitor.Current;

        Reload(configuration, ("Logging:LogLevel:Default", "Debug"));

        Assert.Same(before, monitor.Current);
    }

    [Fact]
    public void Monitor_ChangedListItem_IsAChange()
    {
        var configuration = Configuration(("Server:Hosts:0", "a"), ("Server:Hosts:1", "b"));
        using var provider = Provider(configuration);
        var monitor = provider.GetRequiredService<ConfigurationSnapshotMonitor>();

        Reload(configuration, ("Server:Hosts:1", "c"));

        Assert.Equal(["a", "c"], monitor.Current.Get(Hosts));
    }

    [Fact]
    public void Monitor_AddedListItem_IsAChange()
    {
        var configuration = Configuration(("Server:Hosts:0", "a"));
        using var provider = Provider(configuration);
        var monitor = provider.GetRequiredService<ConfigurationSnapshotMonitor>();

        Reload(configuration, ("Server:Hosts:1", "b"));

        Assert.Equal(["a", "b"], monitor.Current.Get(Hosts));
    }

    [Fact]
    public void Monitor_InvalidReload_KeepsLastValidSnapshot()
    {
        var configuration = Configuration(("Server:Port", "8080"));
        using var provider = Provider(configuration);
        var monitor = provider.GetRequiredService<ConfigurationSnapshotMonitor>();
        var before = monitor.Current;
        var changes = 0;
        monitor.OnChange(_ => changes++);

        Reload(configuration, ("Server:Port", "abc"));

        Assert.Same(before, monitor.Current);
        Assert.Equal(0, changes);
    }

    [Fact]
    public void Monitor_InvalidReload_CallsOnReloadFailedWithEveryError()
    {
        var configuration = Configuration(("Server:Port", "8080"));
        using var provider = Provider(configuration);
        var monitor = provider.GetRequiredService<ConfigurationSnapshotMonitor>();
        var failures = new List<InvalidConfigurationException>();
        monitor.OnReloadFailed(failures.Add);

        Reload(configuration, ("Server:Port", "abc"), ("Database:Timeout", "xyz"));

        var failure = Assert.Single(failures);
        Assert.Equal(["Server:Port", "Database:Timeout"], failure.Diagnostics.Select(diagnostic => diagnostic.Key));
    }

    [Fact]
    public void Monitor_InvalidReload_DoesNotApplyTheValidPart()
    {
        var configuration = Configuration(("Server:Port", "8080"));
        using var provider = Provider(configuration);
        var monitor = provider.GetRequiredService<ConfigurationSnapshotMonitor>();

        Reload(configuration, ("Server:Port", "9090"), ("Database:Timeout", "xyz"));

        Assert.Equal(8080, monitor.Current.Get(Port));
    }

    [Fact]
    public void Monitor_ValidReloadAfterInvalid_Recovers()
    {
        var configuration = Configuration(("Server:Port", "8080"));
        using var provider = Provider(configuration);
        var monitor = provider.GetRequiredService<ConfigurationSnapshotMonitor>();

        Reload(configuration, ("Server:Port", "abc"));
        Reload(configuration, ("Server:Port", "9090"));

        Assert.Equal(9090, monitor.Current.Get(Port));
    }

    [Fact]
    public void Monitor_WarningsAsErrors_AppliesToReload()
    {
        var configuration = Configuration(("Server:Port", "8080"));
        var options = new ConfigurationContractOptions
        {
            ReadOptions = new ReadOptions { CheckedSections = ["Server"], WarningsAsErrors = true },
            ReloadOnChange = true,
        };
        using var provider = Services(configuration, options).BuildServiceProvider();
        var monitor = provider.GetRequiredService<ConfigurationSnapshotMonitor>();
        var failures = new List<InvalidConfigurationException>();
        monitor.OnReloadFailed(failures.Add);

        Reload(configuration, ("Server:Prot", "9090"));

        Assert.Equal("Server:Prot", Assert.Single(Assert.Single(failures).Diagnostics).Key);
    }

    [Fact]
    public void Monitor_DisposedListener_IsNotCalled()
    {
        var configuration = Configuration(("Server:Port", "8080"));
        using var provider = Provider(configuration);
        var changes = 0;

        provider.GetRequiredService<ConfigurationSnapshotMonitor>().OnChange(_ => changes++).Dispose();
        Reload(configuration, ("Server:Port", "9090"));

        Assert.Equal(0, changes);
    }

    [Fact]
    public void Monitor_DisposedProvider_StopsReloading()
    {
        var configuration = Configuration(("Server:Port", "8080"));
        var provider = Provider(configuration);
        var monitor = provider.GetRequiredService<ConfigurationSnapshotMonitor>();

        provider.Dispose();
        Reload(configuration, ("Server:Port", "9090"));

        Assert.Equal(8080, monitor.Current.Get(Port));
    }

    [Fact]
    public void IOptionsMonitor_FollowsReload()
    {
        var configuration = Configuration(("Server:Port", "8080"));
        using var provider = Provider(configuration);
        var monitor = provider.GetRequiredService<IOptionsMonitor<ServerOptions>>();

        Assert.Equal(8080, monitor.CurrentValue.Port);
        Reload(configuration, ("Server:Port", "9090"));

        Assert.Equal(9090, monitor.CurrentValue.Port);
        Assert.Equal(9090, monitor.Get(Options.DefaultName).Port);
        Assert.Equal(9090, monitor.Get(null).Port);
    }

    [Fact]
    public void IOptionsMonitor_OnChange_IsCalledWithTheNewValue()
    {
        var configuration = Configuration(("Server:Port", "8080"));
        using var provider = Provider(configuration);
        var monitor = provider.GetRequiredService<IOptionsMonitor<ServerOptions>>();
        var changes = new List<(ServerOptions Value, string? Name)>();
        monitor.OnChange((value, name) => changes.Add((value, name)));

        Reload(configuration, ("Server:Port", "9090"));

        var (value, name) = Assert.Single(changes);
        Assert.Same(monitor.CurrentValue, value);
        Assert.Equal(9090, value.Port);
        Assert.Equal(Options.DefaultName, name);
    }

    [Fact]
    public void IOptionsMonitor_NamedOptions_Throw()
    {
        using var provider = Provider(Configuration(("Server:Port", "8080")));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            provider.GetRequiredService<IOptionsMonitor<ServerOptions>>().Get("other"));

        Assert.StartsWith("Named options 'other' of ServerOptions are not supported", exception.Message);
    }

    [Fact]
    public void IOptions_KeepsStartupValue()
    {
        var configuration = Configuration(("Server:Port", "8080"));
        using var provider = Provider(configuration);
        provider.GetRequiredService<ConfigurationSnapshot>();   // As the host does at start.

        Reload(configuration, ("Server:Port", "9090"));

        Assert.Equal(8080, provider.GetRequiredService<IOptions<ServerOptions>>().Value.Port);
    }

    [Fact]
    public void Options_AreBuiltOncePerSnapshot()
    {
        var created = 0;
        var configuration = Configuration(("Server:Port", "8080"));
        using var provider = Services(configuration, new ConfigurationContractOptions { ReloadOnChange = true })
            .AddOptionsFrom(snapshot =>
            {
                created++;
                return new ServerOptions { Port = snapshot.Get(Port) };
            })
            .BuildServiceProvider();
        var monitor = provider.GetRequiredService<IOptionsMonitor<ServerOptions>>();
        using var scope = provider.CreateScope();

        Assert.Same(provider.GetRequiredService<IOptions<ServerOptions>>().Value, monitor.CurrentValue);
        Assert.Same(monitor.CurrentValue, scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<ServerOptions>>().Value);
        Assert.Equal(1, created);

        Reload(configuration, ("Server:Port", "9090"));

        Assert.Same(monitor.CurrentValue, monitor.CurrentValue);
        Assert.Equal(2, created);
    }

    [Fact]
    public void IOptionsSnapshot_NewScope_SeesReload()
    {
        var configuration = Configuration(("Server:Port", "8080"));
        using var provider = Provider(configuration);

        Assert.Equal(8080, ScopedPort(provider));
        Reload(configuration, ("Server:Port", "9090"));

        Assert.Equal(9090, ScopedPort(provider));
    }

    [Fact]
    public void IOptionsSnapshot_IsFixedForTheScope()
    {
        var configuration = Configuration(("Server:Port", "8080"));
        using var provider = Provider(configuration);
        using var scope = provider.CreateScope();

        Assert.Equal(8080, scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<ServerOptions>>().Value.Port);
        Reload(configuration, ("Server:Port", "9090"));

        Assert.Equal(8080, scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<ServerOptions>>().Value.Port);
    }

    [Fact]
    public void IOptionsSnapshot_EveryTypeInAScope_ComesFromTheSameRead()
    {
        var configuration = Configuration(("Server:Port", "8080"), ("Database:Timeout", "30"));
        using var provider = Provider(configuration);
        using var scope = provider.CreateScope();

        var server = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<ServerOptions>>().Value;
        Reload(configuration, ("Server:Port", "9090"), ("Database:Timeout", "60"));
        var database = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<DatabaseOptions>>().Value;

        Assert.Equal(8080, server.Port);
        Assert.Equal(30, database.Timeout);
    }

    [Fact]
    public void IOptionsSnapshot_NamedOptions_Throw()
    {
        using var provider = Provider(Configuration(("Server:Port", "8080")));
        using var scope = provider.CreateScope();

        Assert.Throws<InvalidOperationException>(() =>
            scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<ServerOptions>>().Get("other"));
    }

    [Fact]
    public void IOptionsMonitor_WithoutReload_Throws()
    {
        using var provider = Provider(Configuration(("Server:Port", "8080")), reload: false);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            provider.GetRequiredService<IOptionsMonitor<ServerOptions>>());

        Assert.Contains("ReloadOnChange", exception.Message);
    }

    // Sets the values and reloads, as a file provider does when its file changes.
    private static void Reload(IConfigurationRoot configuration, params (string Key, string? Value)[] values)
    {
        foreach (var (key, value) in values)
        {
            configuration[key] = value;
        }

        configuration.Reload();
    }

    private static IConfigurationRoot Configuration(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(pair => KeyValuePair.Create(pair.Key, pair.Value)))
            .Build();

    // AddOptions() registers the open generic IOptionsSnapshot<> and IOptionsMonitor<>, as a host does.
    private static ServiceProvider Provider(IConfiguration configuration, bool reload = true) =>
        Services(configuration, new ConfigurationContractOptions { ReloadOnChange = reload })
            .AddOptions()
            .AddOptionsFrom(snapshot => new ServerOptions { Port = snapshot.Get(Port) })
            .AddOptionsFrom(snapshot => new DatabaseOptions { Timeout = snapshot.Get(Timeout) })
            .BuildServiceProvider();

    private static int ScopedPort(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<ServerOptions>>().Value.Port;
    }

    // The configuration is in the container, as in a host.
    private static IServiceCollection Services(IConfiguration configuration, ConfigurationContractOptions options) =>
        new ServiceCollection()
            .AddSingleton(configuration)
            .AddConfigurationContract(Contract, options);

    private sealed class DatabaseOptions
    {
        public required int Timeout { get; init; }
    }

    private sealed class ServerOptions
    {
        public required int Port { get; init; }
    }
}
