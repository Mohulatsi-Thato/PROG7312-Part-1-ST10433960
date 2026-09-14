using System.Text.Json.Serialization;
using SmartX.Api.Endpoints;
using SmartX.Api.Storage;

var builder = WebApplication.CreateBuilder(args);

// Serialise enums (SensorCategory, PacketStatus, HealthState) as their
// readable names ("Environmental") instead of raw numbers in every request
// and response body - matches the examples in SmartX.Api.http.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Singletons: this is a demo/teaching gateway with in-memory storage, so one
// shared instance per store is intentional - see each store's own comments
// for the concurrency approach used.
builder.Services.AddSingleton<SensorRegistry>();
builder.Services.AddSingleton<TelemetryStore>();

builder.Services.AddCors(options =>
{
    // Wide-open CORS for local development so the WPF client (and, if you
    // later add one, a web frontend) can call this API without extra setup.
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

app.UseCors();

app.MapGet("/", () => Results.Ok(new
{
    Service = "Smart-X Ingestion Gateway",
    Status = "Online",
    Endpoints = new[]
    {
        "POST /api/sensors",
        "GET  /api/sensors",
        "POST /api/sensors/{mac}/attachments (multipart/form-data, field name: file)",
        "POST /api/telemetry/{mac}/moisture | /power | /switch",
        "GET  /api/telemetry/{mac}/health",
        "GET  /api/telemetry/{mac}/stats",
        "GET  /api/power/aggregate",
        "POST /api/simulation/seed/{mac}?count=10000",
    },
}));

app.MapSensorEndpoints();
app.MapTelemetryEndpoints();
app.MapSimulationEndpoints();
app.MapDeploymentEndpoints();

app.Run();
