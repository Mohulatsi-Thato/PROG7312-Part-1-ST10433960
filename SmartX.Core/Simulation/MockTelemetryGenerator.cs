using SmartX.Core.Telemetry;

namespace SmartX.Core.Simulation;

/// <summary>
/// Generates high-volume mock ESP32 telemetry so the ingestion pipeline and
/// its data structures (TelemetryPacket&lt;T&gt;, the jagged-array batch
/// buffer, PowerReading aggregation) can be demonstrated and stress-tested
/// without physical hardware, per the assignment's data-seeding note.
/// </summary>
public static class MockTelemetryGenerator
{
    private static readonly Random Rng = new();

    /// <summary>Generates <paramref name="count"/> soil-moisture float packets for a device.</summary>
    public static IEnumerable<TelemetryPacket<float>> GenerateMoistureReadings(string deviceId, int count, DateTimeOffset start)
    {
        for (var i = 0; i < count; i++)
        {
            var value = (float)(35 + Rng.NextDouble() * 10 - 5); // ~30-45% with noise
            var isSpike = Rng.NextDouble() < 0.03;               // ~3% anomaly rate
            if (isSpike) value += (float)(Rng.NextDouble() > 0.5 ? 40 : -40);

            yield return new TelemetryPacket<float>(
                deviceId,
                value,
                start.AddSeconds(i * 5),
                isSpike ? PacketStatus.Anomalous : PacketStatus.Ok);
        }
    }

    /// <summary>Generates <paramref name="count"/> power-meter wattage readings for a device.</summary>
    public static List<PowerReading> GeneratePowerReadings(string meterId, int count, DateTimeOffset start)
    {
        var readings = new List<PowerReading>(count);
        for (var i = 0; i < count; i++)
        {
            var watts = 800 + Rng.NextDouble() * 200; // baseline load with noise
            readings.Add(new PowerReading(meterId, watts, start.AddSeconds(i * 5)));
        }
        return readings;
    }

    /// <summary>Generates smart-switch boolean trigger packets for a device.</summary>
    public static IEnumerable<TelemetryPacket<bool>> GenerateSwitchTriggers(string deviceId, int count, DateTimeOffset start)
    {
        var state = false;
        for (var i = 0; i < count; i++)
        {
            if (Rng.NextDouble() < 0.15) state = !state; // occasional toggles
            yield return new TelemetryPacket<bool>(deviceId, state, start.AddSeconds(i * 20));
        }
    }

    /// <summary>
    /// Pushes <paramref name="sampleCount"/> mock readings straight into a
    /// jagged-array <see cref="TelemetryBatchBuffer"/> to demonstrate the
    /// buffer growing under a realistic load (e.g. 10,000+ samples).
    /// </summary>
    public static TelemetryBatchBuffer SeedBatchBuffer(int sampleCount, int chunkSize = 100)
    {
        var buffer = new TelemetryBatchBuffer(chunkSize);
        for (var i = 0; i < sampleCount; i++)
            buffer.Add(20 + Rng.NextDouble() * 5);
        return buffer;
    }
}
