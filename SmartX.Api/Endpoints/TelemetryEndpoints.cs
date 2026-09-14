using SmartX.Api.Contracts;
using SmartX.Api.Storage;

namespace SmartX.Api.Endpoints;

/// <summary>
/// API Integration Layer: the primary receiver of telemetry data. Three
/// typed routes (moisture/float, power/double, switch/bool) keep each
/// concrete <c>TelemetryPacket&lt;T&gt;</c> instantiation - and therefore its
/// no-boxing guarantee - intact all the way from HTTP body to storage.
/// </summary>
public static class TelemetryEndpoints
{
    public static void MapTelemetryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/telemetry").WithTags("Telemetry");

        group.MapPost("/{mac}/moisture", (string mac, MoistureReadingRequest req, TelemetryStore store) =>
            Results.Ok(store.AddMoisture(mac, req.Value, req.Timestamp ?? DateTimeOffset.UtcNow)));

        group.MapPost("/{mac}/power", (string mac, PowerReadingRequest req, TelemetryStore store) =>
            Results.Ok(store.AddPower(mac, req.Watts, req.Timestamp ?? DateTimeOffset.UtcNow)));

        group.MapPost("/{mac}/switch", (string mac, SwitchTriggerRequest req, TelemetryStore store) =>
            Results.Ok(store.AddSwitchTrigger(mac, req.State, req.Timestamp ?? DateTimeOffset.UtcNow)));

        group.MapGet("/{mac}/moisture", (string mac, int? take, TelemetryStore store) =>
            Results.Ok(store.GetMoisture(mac, take ?? 50)));

        group.MapGet("/{mac}/power", (string mac, int? take, TelemetryStore store) =>
            Results.Ok(store.GetPower(mac, take ?? 50)));

        group.MapGet("/{mac}/switch", (string mac, int? take, TelemetryStore store) =>
            Results.Ok(store.GetSwitchTriggers(mac, take ?? 50)));

        // Powers the Sensor Health Card engagement widget (score, streak, colour state).
        group.MapGet("/{mac}/health", (string mac, TelemetryStore store) =>
        {
            var health = store.HealthFor(mac);
            health.EvaluateTimeout(DateTimeOffset.UtcNow);

            return Results.Ok(new
            {
                health.DeviceId,
                health.Score,
                State = health.State.ToString(),
                StreakSeconds = health.Streak.TotalSeconds,
            });
        });

        // Rolling mean/std-dev computed from the jagged-array TelemetryBatchBuffer.
        group.MapGet("/{mac}/stats", (string mac, TelemetryStore store) =>
        {
            var buffer = store.BufferFor(mac);
            var (mean, stdDev) = buffer.ComputeStatistics();
            return Results.Ok(new { Count = buffer.Count, Mean = mean, StdDev = stdDev });
        });

        // Demonstrates PowerReading's overloaded '+' aggregating every meter's latest reading.
        app.MapGet("/api/power/aggregate", (TelemetryStore store) =>
            store.AggregateLoad() is { } total
                ? Results.Ok(total)
                : Results.Ok(new { Message = "No power readings recorded yet." }))
            .WithTags("Telemetry");
    }
}
