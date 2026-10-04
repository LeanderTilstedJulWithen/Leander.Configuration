using Microsoft.Extensions.Options;

namespace Leander.Configuration.MicrosoftExtensions.Sample;

// A consumer. It takes ServerOptions as it is, and CorsOptions through IOptions.
public sealed class Server(ServerOptions options, IOptions<CorsOptions> cors)
{
    private readonly CorsOptions _cors = cors.Value;
    private readonly ServerOptions _options = options;

    public void Describe()
    {
        Console.WriteLine($"  Host            {_options.Host}");
        Console.WriteLine($"  Port            {_options.Port}");
        Console.WriteLine($"  Features        {string.Join(", ", _options.Features)}");
        Console.WriteLine($"  Certificate     {_options.CertificatePath ?? "(none, plain HTTP)"}");
        Console.WriteLine($"  AllowedOrigins  {string.Join(", ", _cors.AllowedOrigins)}");
    }
}
