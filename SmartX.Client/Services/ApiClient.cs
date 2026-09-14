using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SmartX.Core.Devices;

namespace SmartX.Client.Services;

/// <summary>Mirrors the JSON shape returned by GET /api/telemetry/{mac}/health.</summary>
public record SensorHealthDto(string DeviceId, int Score, string State, double StreakSeconds);

/// <summary>
/// Thin HTTP wrapper around the SmartX.Api Minimal API endpoints. This is a
/// local, single-user demo client talking to a gateway on the same machine,
/// so no retry/circuit-breaker policies are layered on top - failures are
/// surfaced to the caller and logged in the UI instead.
/// </summary>
public class ApiClient
{
    private readonly HttpClient _http;

    // JsonSerializerDefaults.Web = camelCase + case-insensitive property
    // matching, matching ASP.NET Core's own Minimal API JSON defaults, plus
    // a string enum converter so "Environmental" round-trips as text, not 0/1/2.
    private static readonly JsonSerializerOptions JsonOptions = CreateOptions();

    public ApiClient(string baseAddress = "http://localhost:5080")
    {
        _http = new HttpClient { BaseAddress = new Uri(baseAddress) };
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    public async Task<SensorDevice?> RegisterSensorAsync(string mac, string location, SensorCategory category)
    {
        var payload = new { macAddress = mac, location, category = category.ToString() };
        var response = await _http.PostAsJsonAsync("/api/sensors", payload, JsonOptions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SensorDevice>(JsonOptions);
    }

    public async Task<string> UploadAttachmentAsync(string mac, string filePath)
    {
        using var form = new MultipartFormDataContent();
        var bytes = await File.ReadAllBytesAsync(filePath);
        using var fileContent = new ByteArrayContent(bytes);
        form.Add(fileContent, "file", Path.GetFileName(filePath));

        var response = await _http.PostAsync($"/api/sensors/{Uri.EscapeDataString(mac)}/attachments", form);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task PostMoistureAsync(string mac, float value)
    {
        var response = await _http.PostAsJsonAsync($"/api/telemetry/{Uri.EscapeDataString(mac)}/moisture", new { value }, JsonOptions);
        response.EnsureSuccessStatusCode();
    }

    public async Task<SensorHealthDto?> GetHealthAsync(string mac)
    {
        var response = await _http.GetAsync($"/api/telemetry/{Uri.EscapeDataString(mac)}/health");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<SensorHealthDto>(JsonOptions);
    }

    public async Task<string> SeedAsync(string mac, int count)
    {
        var response = await _http.PostAsync($"/api/simulation/seed/{Uri.EscapeDataString(mac)}?count={count}", content: null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}
