using System.Collections.Concurrent;
using System.Linq;
using SmartX.Core.Devices;

namespace SmartX.Api.Storage;

/// <summary>
/// In-memory, thread-safe registry of every sensor the dashboard has
/// registered. Backed by <see cref="ConcurrentDictionary{TKey,TValue}"/> so
/// concurrent ingestion requests from many simulated ESP32 nodes never
/// corrupt the collection.
/// </summary>
public class SensorRegistry
{
    private readonly ConcurrentDictionary<string, SensorDevice> _sensors = new(StringComparer.OrdinalIgnoreCase);

    public SensorDevice Register(SensorDevice sensor)
    {
        _sensors[sensor.MacAddress] = sensor;
        return sensor;
    }

    public SensorDevice? Get(string mac) => _sensors.GetValueOrDefault(mac);

    public IEnumerable<SensorDevice> GetAll() => _sensors.Values.OrderBy(s => s.Location);

    public bool Remove(string mac) => _sensors.TryRemove(mac, out _);

    public bool Exists(string mac) => _sensors.ContainsKey(mac);
}
