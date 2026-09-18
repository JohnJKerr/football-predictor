namespace Api.IntegrationTests;

using External.Jev;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Hosts the real API in-process, talking to the real Jev endpoint.
/// The API key comes from the Api project's user secrets:
/// <c>dotnet user-secrets set "Jev:ApiKey" "&lt;key&gt;" --project src/Api</c>
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
        => builder.UseEnvironment("Development");

    /// <summary>Null when no key is configured, so integration tests can skip rather than fail.</summary>
    public string? ApiKey
    {
        get
        {
            var key = Services.GetRequiredService<IJevSettings>().ApiKey;
            return string.IsNullOrWhiteSpace(key) ? null : key;
        }
    }
}
