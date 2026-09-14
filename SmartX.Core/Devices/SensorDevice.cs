namespace SmartX.Core.Devices;

/// <summary>Broad classification of a registered sensor, as required by the sensor payload management brief.</summary>
public enum SensorCategory
{
    Environmental,
    PowerConsumption,
    Actuator
}

/// <summary>
/// A registered physical device: its identity, where it is deployed, what
/// kind of sensor it is, and any attached configuration files, deployment
/// photos, or hardware logs uploaded through the dashboard.
/// </summary>
public class SensorDevice
{
    /// <summary>Device MAC address / unique identifier (e.g. "24:6F:28:AE:11:9C").</summary>
    public string MacAddress { get; set; } = string.Empty;

    /// <summary>Human-readable deployment location, e.g. "Room 4 / Node 12".</summary>
    public string Location { get; set; } = string.Empty;

    public SensorCategory Category { get; set; }

    /// <summary>Server-side paths of any attached config files, deployment photos, or hardware logs.</summary>
    public List<string> AttachmentPaths { get; set; } = new();

    public DateTimeOffset RegisteredAt { get; set; } = DateTimeOffset.UtcNow;

    public override string ToString() => $"{MacAddress} [{Category}] @ {Location}";
}
