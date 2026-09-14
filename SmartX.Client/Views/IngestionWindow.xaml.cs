using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using SmartX.Client.Services;
using SmartX.Client.ViewModels;
using SmartX.Core.Devices;

namespace SmartX.Client.Views;

public partial class IngestionWindow : Window
{
    private readonly ApiClient _api = new();
    private readonly ObservableCollection<SensorCardViewModel> _sensors = new();
    private readonly DispatcherTimer _pollTimer;
    private readonly Random _random = new();

    public IngestionWindow()
    {
        InitializeComponent();

        SensorItemsControl.ItemsSource = _sensors;

        CategoryCombo.ItemsSource = Enum.GetValues<SensorCategory>();
        CategoryCombo.SelectedIndex = 0;

        TargetSensorCombo.ItemsSource = _sensors;
        TargetSensorCombo.DisplayMemberPath = nameof(SensorCardViewModel.MacAddress);

        _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _pollTimer.Tick += async (_, _) => await PollHealthAsync();
        _pollTimer.Start();
    }

    private async void RegisterButton_Click(object sender, RoutedEventArgs e)
    {
        var mac = MacTextBox.Text.Trim();
        var location = LocationTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(mac))
        {
            Log("MAC address is required.");
            return;
        }

        if (CategoryCombo.SelectedItem is not SensorCategory category)
        {
            Log("Please select a sensor category.");
            return;
        }

        try
        {
            var sensor = await _api.RegisterSensorAsync(mac, location, category);
            if (sensor is null)
            {
                Log("Registration failed: no response from gateway.");
                return;
            }

            var card = new SensorCardViewModel
            {
                MacAddress = sensor.MacAddress,
                Location = string.IsNullOrWhiteSpace(sensor.Location) ? sensor.MacAddress : sensor.Location,
                Category = sensor.Category.ToString(),
            };

            _sensors.Add(card);
            TargetSensorCombo.SelectedItem = card;

            Log($"Registered {sensor.MacAddress} at '{sensor.Location}'.");
            MacTextBox.Clear();
        }
        catch (Exception ex)
        {
            Log($"Registration error: {ex.Message}. Is SmartX.Api running on http://localhost:5080?");
        }
    }

    private async void AttachFileButton_Click(object sender, RoutedEventArgs e)
    {
        if (TargetSensorCombo.SelectedItem is not SensorCardViewModel target)
        {
            Log("Select a target sensor first.");
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Select a device configuration file, deployment photo, or hardware log",
            Filter = "Supported files|*.json;*.txt;*.log;*.jpg;*.jpeg;*.png|All files|*.*",
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            await _api.UploadAttachmentAsync(target.MacAddress, dialog.FileName);
            Log($"Attached '{System.IO.Path.GetFileName(dialog.FileName)}' to {target.MacAddress}.");
        }
        catch (Exception ex)
        {
            Log($"Attachment upload failed: {ex.Message}");
        }
    }

    private async void SendReadingButton_Click(object sender, RoutedEventArgs e)
    {
        if (TargetSensorCombo.SelectedItem is not SensorCardViewModel target)
        {
            Log("Select a target sensor first.");
            return;
        }

        try
        {
            var value = (float)(35 + _random.NextDouble() * 10 - 5);
            await _api.PostMoistureAsync(target.MacAddress, value);
            Log($"Sent moisture reading {value:F1} to {target.MacAddress}.");
        }
        catch (Exception ex)
        {
            Log($"Send failed: {ex.Message}");
        }
    }

    private async void SeedButton_Click(object sender, RoutedEventArgs e)
    {
        if (TargetSensorCombo.SelectedItem is not SensorCardViewModel target)
        {
            Log("Select a target sensor first.");
            return;
        }

        SeedButton.IsEnabled = false;
        try
        {
            var result = await _api.SeedAsync(target.MacAddress, 10_000);
            Log($"Seeded 10,000 readings for {target.MacAddress}: {result}");
        }
        catch (Exception ex)
        {
            Log($"Seed failed: {ex.Message}");
        }
        finally
        {
            SeedButton.IsEnabled = true;
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        _pollTimer.Stop();
        new MainWindow().Show();
        Close();
    }

    private async Task PollHealthAsync()
    {
        foreach (var sensor in _sensors.ToList())
        {
            try
            {
                var health = await _api.GetHealthAsync(sensor.MacAddress);
                if (health is null) continue;

                sensor.Score = health.Score;
                sensor.State = health.State;
                sensor.StreakText = FormatStreak(health.StreakSeconds, health.State);
            }
            catch
            {
                // Transient poll failures (e.g. the gateway briefly restarting)
                // are intentionally swallowed - the card just keeps its last value.
            }
        }
    }

    private static string FormatStreak(double seconds, string state)
    {
        if (string.Equals(state, "Disconnected", StringComparison.OrdinalIgnoreCase)) return "streak reset";
        if (string.Equals(state, "Anomalous", StringComparison.OrdinalIgnoreCase)) return "streak preserved";

        var span = TimeSpan.FromSeconds(seconds);
        return span.TotalHours >= 1
            ? $"{(int)span.TotalHours}h stable streak"
            : $"{(int)span.TotalMinutes}m stable streak";
    }

    private void Log(string message)
    {
        LogListBox.Items.Insert(0, $"[{DateTime.Now:T}] {message}");
        while (LogListBox.Items.Count > 100)
            LogListBox.Items.RemoveAt(LogListBox.Items.Count - 1);
    }
}
