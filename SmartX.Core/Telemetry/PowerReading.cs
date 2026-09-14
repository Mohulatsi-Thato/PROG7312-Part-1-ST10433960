namespace SmartX.Core.Telemetry;

/// <summary>
/// Represents a single smart-meter power reading in watts.
///
/// Technical Requirement: Operator Overloading. Overloading arithmetic and
/// comparison operators directly on the domain type lets calling code read
/// like the physical operation it represents, e.g.:
/// <code>
///   var meter3 = meter1 + meter2;      // aggregate load of two meters
///   var delta  = readingNow - readingPrevious; // change since last sample
///   if (meter1 > meter2) { ... }        // which meter is drawing more
/// </code>
/// instead of manually unwrapping <c>.Watts</c> everywhere.
/// </summary>
public readonly struct PowerReading : IComparable<PowerReading>, IEquatable<PowerReading>
{
    public string MeterId { get; }
    public double Watts { get; }
    public DateTimeOffset Timestamp { get; }

    public PowerReading(string meterId, double watts, DateTimeOffset timestamp)
    {
        MeterId = meterId;
        Watts = watts;
        Timestamp = timestamp;
    }

    /// <summary>Aggregate load of two meters, e.g. Meter3 = Meter1 + Meter2.</summary>
    public static PowerReading operator +(PowerReading a, PowerReading b) =>
        new($"{a.MeterId}+{b.MeterId}", a.Watts + b.Watts, Latest(a.Timestamp, b.Timestamp));

    /// <summary>Delta between two readings, e.g. how much load changed between samples.</summary>
    public static PowerReading operator -(PowerReading a, PowerReading b) =>
        new($"{a.MeterId}-{b.MeterId}", a.Watts - b.Watts, Latest(a.Timestamp, b.Timestamp));

    public static bool operator >(PowerReading a, PowerReading b) => a.Watts > b.Watts;
    public static bool operator <(PowerReading a, PowerReading b) => a.Watts < b.Watts;
    public static bool operator >=(PowerReading a, PowerReading b) => a.Watts >= b.Watts;
    public static bool operator <=(PowerReading a, PowerReading b) => a.Watts <= b.Watts;

    public static bool operator ==(PowerReading a, PowerReading b) => a.Equals(b);
    public static bool operator !=(PowerReading a, PowerReading b) => !a.Equals(b);

    public int CompareTo(PowerReading other) => Watts.CompareTo(other.Watts);

    public bool Equals(PowerReading other) =>
        MeterId == other.MeterId && Watts.Equals(other.Watts);

    public override bool Equals(object? obj) => obj is PowerReading other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(MeterId, Watts);

    public override string ToString() => $"{MeterId}: {Watts:F1} W @ {Timestamp:T}";

    private static DateTimeOffset Latest(DateTimeOffset a, DateTimeOffset b) => a > b ? a : b;
}
