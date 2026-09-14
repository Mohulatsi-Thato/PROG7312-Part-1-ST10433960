namespace SmartX.Core.Diagnostics;

/// <summary>Visual/behavioural state of a sensor's live health card on the dashboard.</summary>
public enum HealthState
{
    Healthy,
    Anomalous,
    Disconnected
}

/// <summary>
/// Drives the "Sensor Health Score + Anomaly-Streak Gamification" engagement
/// feature described in the Part 1 research report: each registered sensor
/// gets a live 0-100 score, a clean-reading streak, and a colour state
/// (green/amber/red) that the WPF dashboard renders as an animated card.
///
/// Rules (matching the report's justification):
///  - A reading that arrives on schedule and within range increases the
///    score, extends the streak, and marks the sensor Healthy (green).
///  - An anomalous reading (out of range, but it still arrived) drops the
///    score and marks the sensor Amber, but the streak is preserved, since a
///    single spike does not necessarily indicate hardware failure.
///  - A disconnect (no reading within <see cref="DisconnectThreshold"/>)
///    resets the streak to zero and marks the sensor Red immediately.
/// </summary>
public class SensorHealth
{
    public static readonly TimeSpan DisconnectThreshold = TimeSpan.FromSeconds(30);

    public string DeviceId { get; }
    public int Score { get; private set; } = 100;
    public TimeSpan Streak { get; private set; } = TimeSpan.Zero;
    public HealthState State { get; private set; } = HealthState.Healthy;
    public DateTimeOffset LastSeen { get; private set; }

    public SensorHealth(string deviceId, DateTimeOffset? firstSeen = null)
    {
        DeviceId = deviceId;
        LastSeen = firstSeen ?? DateTimeOffset.UtcNow;
    }

    /// <summary>Feeds one new telemetry sample's outcome into the health engine.</summary>
    /// <param name="withinExpectedRange">False if the value itself is an outlier/anomaly.</param>
    /// <param name="timestamp">When the sample was received by the gateway.</param>
    public void RecordReading(bool withinExpectedRange, DateTimeOffset timestamp)
    {
        var gapSinceLast = timestamp - LastSeen;
        LastSeen = timestamp;

        if (gapSinceLast > DisconnectThreshold)
        {
            State = HealthState.Disconnected;
            Streak = TimeSpan.Zero;
            Score = Math.Max(0, Score - 40);
            return;
        }

        if (!withinExpectedRange)
        {
            State = HealthState.Anomalous;
            Score = Math.Max(0, Score - 10);
            return; // streak intentionally preserved
        }

        State = HealthState.Healthy;
        Streak += gapSinceLast;
        Score = Math.Min(100, Score + 2);
    }

    /// <summary>Call periodically (e.g. from a UI timer) so a sensor that has simply gone quiet is still flagged red.</summary>
    public void EvaluateTimeout(DateTimeOffset now)
    {
        if (now - LastSeen > DisconnectThreshold && State != HealthState.Disconnected)
        {
            State = HealthState.Disconnected;
            Streak = TimeSpan.Zero;
            Score = Math.Max(0, Score - 40);
        }
    }
}
