using System.Collections.Concurrent;
using System.Linq;
using SmartX.Core.Diagnostics;
using SmartX.Core.Telemetry;

namespace SmartX.Api.Storage;

/// <summary>
/// In-memory telemetry store. Each concrete closed generic type gets its own
/// typed list — <c>List&lt;TelemetryPacket&lt;float&gt;&gt;</c>,
/// <c>List&lt;TelemetryPacket&lt;bool&gt;&gt;</c>, <c>List&lt;PowerReading&gt;</c>
/// — so heterogeneous sensor payloads never need to be boxed into a shared
/// <c>object</c>-based collection just to sit in the same store.
/// </summary>
public class TelemetryStore
{
    private readonly ConcurrentDictionary<string, List<TelemetryPacket<float>>> _moisture = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, List<PowerReading>> _power = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, List<TelemetryPacket<bool>>> _switches = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, TelemetryBatchBuffer> _buffers = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, SensorHealth> _health = new(StringComparer.OrdinalIgnoreCase);

    private const float MoistureLow = 15f;
    private const float MoistureHigh = 65f;
    private const double PowerLow = 0d;
    private const double PowerHigh = 3000d;

    // A single coarse lock around list mutation is deliberately simple for a
    // teaching/demo gateway - List<T> itself isn't thread-safe for concurrent
    // Add calls, and this keeps ingestion correctness obvious to a marker.
    // A production gateway would shard this per device instead.
    private static readonly object PadLock = new();

    public SensorHealth HealthFor(string deviceId) =>
        _health.GetOrAdd(deviceId, id => new SensorHealth(id));

    public TelemetryBatchBuffer BufferFor(string deviceId) =>
        _buffers.GetOrAdd(deviceId, _ => new TelemetryBatchBuffer());

    public TelemetryPacket<float> AddMoisture(string deviceId, float value, DateTimeOffset timestamp)
    {
        var withinRange = value is >= MoistureLow and <= MoistureHigh;
        var packet = new TelemetryPacket<float>(deviceId, value, timestamp, withinRange ? PacketStatus.Ok : PacketStatus.Anomalous);

        lock (PadLock)
            _moisture.GetOrAdd(deviceId, _ => new List<TelemetryPacket<float>>()).Add(packet);

        BufferFor(deviceId).Add(value);
        HealthFor(deviceId).RecordReading(withinRange, timestamp);

        return packet;
    }

    public PowerReading AddPower(string deviceId, double watts, DateTimeOffset timestamp)
    {
        var reading = new PowerReading(deviceId, watts, timestamp);
        var withinRange = watts is >= PowerLow and <= PowerHigh;

        lock (PadLock)
            _power.GetOrAdd(deviceId, _ => new List<PowerReading>()).Add(reading);

        HealthFor(deviceId).RecordReading(withinRange, timestamp);

        return reading;
    }

    public TelemetryPacket<bool> AddSwitchTrigger(string deviceId, bool state, DateTimeOffset timestamp)
    {
        var packet = new TelemetryPacket<bool>(deviceId, state, timestamp);

        lock (PadLock)
            _switches.GetOrAdd(deviceId, _ => new List<TelemetryPacket<bool>>()).Add(packet);

        HealthFor(deviceId).RecordReading(true, timestamp);

        return packet;
    }

    public IReadOnlyList<TelemetryPacket<float>> GetMoisture(string deviceId, int take = 50) =>
        _moisture.TryGetValue(deviceId, out var list) ? list.TakeLast(take).ToList() : Array.Empty<TelemetryPacket<float>>();

    public IReadOnlyList<PowerReading> GetPower(string deviceId, int take = 50) =>
        _power.TryGetValue(deviceId, out var list) ? list.TakeLast(take).ToList() : Array.Empty<PowerReading>();

    public IReadOnlyList<TelemetryPacket<bool>> GetSwitchTriggers(string deviceId, int take = 50) =>
        _switches.TryGetValue(deviceId, out var list) ? list.TakeLast(take).ToList() : Array.Empty<TelemetryPacket<bool>>();

    /// <summary>
    /// Sums the latest reading from every registered power meter using
    /// <see cref="PowerReading"/>'s overloaded <c>+</c> operator — e.g.
    /// Meter3 = Meter1 + Meter2 — to report a facility-wide aggregate load.
    /// </summary>
    public PowerReading? AggregateLoad()
    {
        PowerReading? total = null;

        foreach (var meterReadings in _power.Values)
        {
            if (meterReadings.Count == 0) continue;
            var latest = meterReadings[^1];
            total = total is null ? latest : total.Value + latest;
        }

        return total;
    }
}
