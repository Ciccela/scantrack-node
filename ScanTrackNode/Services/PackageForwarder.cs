using System.Text;
using System.Text.Json;
using ScanTrackNode.Models;

namespace ScanTrackNode.Services;

public class PackageForwarder
{
    private readonly NodeRegistry _registry;
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<PackageForwarder> _logger;

    public PackageForwarder(NodeRegistry registry, IHttpClientFactory factory, ILogger<PackageForwarder> logger)
    {
        _registry = registry;
        _factory = factory;
        _logger = logger;
    }

    // DIN UPPGIFT: Vidarebefordra paketet till nästa nod i nätverket.
    //
    // Steg för steg:
    //   1. Hämta nodlistan: await _registry.GetNodesAsync()
    //      → returnerar Dictionary<string, string>  (stad → url)
    //   2. Slå upp URL:en för 'nextCity'
    //      → om staden inte finns: logga fel och returnera false
    //   3. Serialisera 'package' till JSON: JsonSerializer.Serialize(package)
    //   4. Skapa HTTP-body: new StringContent(json, Encoding.UTF8, "application/json")
    //   5. Skapa en HttpClient: _factory.CreateClient()
    //   6. Skicka: await http.PostAsync($"{url}/paket", content)
    //   7. Logga att du skickade (stad + packageId): _logger.LogInformation(...)
    //   8. Returnera response.IsSuccessStatusCode
    public async Task<bool> ForwardAsync(Package package, string nextCity)
{
    var nodes = await _registry.GetNodesAsync();

    if (!nodes.TryGetValue(nextCity, out var url))
    {
        _logger.LogError("Kunde inte vidarebefordra paket {PackageId}: okänd stad {NextCity}", package.Id, nextCity);
        return false;
    }

    var json = JsonSerializer.Serialize(package);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var http = _factory.CreateClient();
    var response = await http.PostAsync($"{url.TrimEnd('/')}/paket", content);

    _logger.LogInformation(
        "Vidarebefordrade paket {PackageId} till {NextCity} via {Url} med status {StatusCode}",
        package.Id, nextCity, url, (int)response.StatusCode);

    return response.IsSuccessStatusCode;
}
}
