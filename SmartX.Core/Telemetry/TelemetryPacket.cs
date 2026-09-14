namespace SmartX.Core.Telemetry;

/// <summary>
/// The lifecycle state of a single telemetry sample, used by the ingestion
/// pipeline and the dashboard health-score engine to decide how to react.
/// </summary>
public enum PacketStatus
{
    Ok,
    Anomalous,
    Stale
}

/// <summary>
/// Generic wrapper (Technical Requirement: Generics) that lets the ingestion
/// gateway handle disparate incoming ESP32 payload shapes - a <c>float</c> for
/// soil moisture, an <c>int</c> for power wattage, a <c>bool</c> for a smart
/// switch trigger - through a single, uniform type.
///
/// <para>
/// <b>Why this avoids boxing/unboxing:</b> <c>TelemetryPacket&lt;T&gt;</c> is a
/// <c>readonly struct</c> (a value type) and <typeparamref name="T"/> is
/// constrained to <c>struct</c>. The .NET JIT compiles a specialised, fully
/// unboxed version of this type for every concrete value type it is closed
/// over (e.g. <c>TelemetryPacket&lt;float&gt;</c>,
/// <c>TelemetryPacket&lt;int&gt;</c>, <c>TelemetryPacket&lt;bool&gt;</c>).
/// Because both the wrapper and the payload are value types, packets can be
/// created, copied and read on resource-constrained gateway hardware without
/// ever allocating on the managed heap or boxing <typeparamref name="T"/> into
/// an <c>object</c> - which is exactly what would happen with a naive
/// <c>object Value</c> field.
/// </para>
/// </summary>
/// <typeparam name="T">
/// The underlying sensor value type. Constrained to <c>struct</c> so only
/// value types (float, int, bool, double, etc.) can be used - reference types
/// are rejected at compile time.
/// </typeparam>
public readonly struct TelemetryPacket<T> where T : struct
{
    /// <summary>Unique identifier / MAC address of the reporting device.</summary>
    public string DeviceId { get; }

    /// <summary>The raw, strongly-typed sensor value for this sample.</summary>
    public T Value { get; }

    /// <summary>When the gateway received this sample.</summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>Validation/anomaly state assigned by the ingestion pipeline.</summary>
    public PacketStatus Status { get; }

    public TelemetryPacket(string deviceId, T value, DateTimeOffset timestamp, PacketStatus status = PacketStatus.Ok)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("DeviceId cannot be empty.", nameof(deviceId));

        DeviceId = deviceId;
        Value = value;
        Timestamp = timestamp;
        Status = status;
    }

    /// <summary>Returns a copy of this packet re-flagged with a new status.</summary>
    public TelemetryPacket<T> WithStatus(PacketStatus status) => new(DeviceId, Value, Timestamp, status);

    public override string ToString() => $"[{Timestamp:HH:mm:ss}] {DeviceId} => {Value} ({Status})";
}
