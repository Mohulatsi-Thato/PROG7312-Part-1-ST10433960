using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SmartX.Client.ViewModels;

/// <summary>
/// Backs one live Sensor Health Card. Polled and updated on a timer in
/// <see cref="Views.IngestionWindow"/> from the gateway's
/// <c>GET /api/telemetry/{mac}/health</c> endpoint, which in turn is driven
/// by <c>SmartX.Core.Diagnostics.SensorHealth</c>.
/// </summary>
public class SensorCardViewModel : INotifyPropertyChanged
{
    private int _score = 100;
    private string _state = "Healthy";
    private string _streakText = "just connected";

    public string MacAddress { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;

    public int Score
    {
        get => _score;
        set
        {
            if (_score == value) return;
            _score = value;
            OnPropertyChanged();
        }
    }

    public string State
    {
        get => _state;
        set
        {
            if (_state == value) return;
            _state = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StateColor));
            OnPropertyChanged(nameof(StateBadgeText));
        }
    }

    public string StreakText
    {
        get => _streakText;
        set
        {
            if (_streakText == value) return;
            _streakText = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Hex colour matching the report's green/amber/red health states.</summary>
    public string StateColor => State switch
    {
        "Anomalous" => "#F59E0B",
        "Disconnected" => "#EF4444",
        _ => "#22C55E",
    };

    public string StateBadgeText => State switch
    {
        "Anomalous" => "Anomalous spike detected",
        "Disconnected" => "Disconnected",
        _ => "Healthy",
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
