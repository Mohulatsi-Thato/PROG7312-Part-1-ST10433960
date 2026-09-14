using SmartX.Core.Devices;

namespace SmartX.Api.Contracts;

/// <summary>Body of POST /api/sensors — registers a new device.</summary>
public record RegisterSensorRequest(string MacAddress, string Location, SensorCategory Category);

/// <summary>Body of POST /api/telemetry/{mac}/moisture — a float environmental reading.</summary>
public record MoistureReadingRequest(float Value, DateTimeOffset? Timestamp = null);

/// <summary>Body of POST /api/telemetry/{mac}/power — a double-precision wattage reading.</summary>
public record PowerReadingRequest(double Watts, DateTimeOffset? Timestamp = null);

/// <summary>Body of POST /api/telemetry/{mac}/switch — a boolean actuator/relay trigger.</summary>
public record SwitchTriggerRequest(bool State, DateTimeOffset? Timestamp = null);
