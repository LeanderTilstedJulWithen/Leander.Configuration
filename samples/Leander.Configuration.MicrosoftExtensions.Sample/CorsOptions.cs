namespace Leander.Configuration.MicrosoftExtensions.Sample;

// Written by the application and injected as IOptions<CorsOptions>, for code that expects Microsoft.Extensions.Options.
public sealed record CorsOptions(IReadOnlyList<Uri> AllowedOrigins)
{
    public static CorsOptions FromSnapshot(ConfigurationSnapshot configuration) =>
        new(configuration.Get(ServerConfiguration.AllowedOrigins));
}
