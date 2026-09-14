using System.Diagnostics;
using SmartX.Api.Storage;
using SmartX.Core.Simulation;

namespace SmartX.Api.Endpoints;

/// <summary>
/// Satisfies the assignment's data-seeding note: lets a marker generate a
/// large volume of mock ESP32 telemetry through the real ingestion path (so
/// the jagged-array buffer, health engine, and typed stores are all
/// exercised under load) without needing physical hardware.
/// </summary>
public static class SimulationEndpoints
{
    public static void MapSimulationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/simulation").WithTags("Simulation");

        group.MapPost("/seed/{mac}", (string mac, int? count, TelemetryStore store) =>
        {
            var sampleCount = Math.Clamp(count ?? 1000, 1, 200_000);
            var start = DateTimeOffset.UtcNow.AddSeconds(-sampleCount * 5);

            var stopwatch = Stopwatch.StartNew();

            foreach (var packet in MockTelemetryGenerator.GenerateMoistureReadings(mac, sampleCount, start))
                store.AddMoisture(mac, packet.Value, packet.Timestamp);

            stopwatch.Stop();

            var health = store.HealthFor(mac);
            var (mean, stdDev) = store.BufferFor(mac).ComputeStatistics();

            return Results.Ok(new
            {
                DeviceId = mac,
                Seeded = sampleCount,
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                health.Score,
                State = health.State.ToString(),
                Mean = mean,
                StdDev = stdDev,
            });
        });
    }
}
