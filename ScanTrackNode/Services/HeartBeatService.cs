using System.Text;
using System.Text.Json;

namespace ScanTrackNode.Services;

public class HeartbeatService : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<HeartbeatService> _logger;
    private DateTimeOffset _lastForced = DateTimeOffset.MinValue;

    public HeartbeatService(
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<HeartbeatService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await SendHeartbeatAsync(ct); // direkt vid start

        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromHours(1), ct);
            await SendHeartbeatAsync(ct);
        }
    }

    public async Task<bool> SendHeartbeatAsync(CancellationToken ct = default, bool forced = false)
    {
        if (forced && DateTimeOffset.UtcNow - _lastForced < TimeSpan.FromMinutes(10))
        {
            _logger.LogWarning("forceheartbeat throttled (max 1 gång/10 min).");
            return false;
        }

        var city = _config["CITY_NAME"];
        var nodeUrl = _config["NODE_URL"];
        var registryUrl = _config["REGISTRY_URL"] ?? _config["RegistryUrl"];

        if (string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(nodeUrl) || string.IsNullOrWhiteSpace(registryUrl))
        {
            _logger.LogWarning("Heartbeat saknar CITY_NAME/NODE_URL/REGISTRY_URL.");
            return false;
        }

        var payload = new { city, url = nodeUrl };
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var endpoint = $"{registryUrl.TrimEnd('/')}/nodes";
        var client = _httpClientFactory.CreateClient();

        try
        {
            var response = await client.PostAsync(endpoint, content, ct);
            if (forced) _lastForced = DateTimeOffset.UtcNow;

            _logger.LogInformation("Heartbeat skickad för {City} till {Endpoint}. Status {StatusCode}",
                city, endpoint, (int)response.StatusCode);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Heartbeat misslyckades");
            return false;
        }
    }
}