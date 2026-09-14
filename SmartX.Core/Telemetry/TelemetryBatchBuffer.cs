using System.Linq;

namespace SmartX.Core.Telemetry;

/// <summary>
/// Buffers raw numeric telemetry into sequential, fixed-width historical
/// batches before handing them off to an optimised <see cref="List{T}"/> for
/// downstream processing (e.g. rolling-average anomaly detection).
///
/// Technical Requirement: Advanced Arrays and Lists. Backed by a
/// <b>jagged array</b> (<c>double[][]</c>) rather than a rectangular
/// multi-dimensional array (<c>double[,]</c>) because each row represents an
/// independently-sized "batch window" of readings for one device - jagged
/// arrays let each row be allocated and grown independently, which matches
/// how ESP32 nodes report at uneven intervals under real network jitter.
/// </summary>
public class TelemetryBatchBuffer
{
    private readonly int _chunkSize;
    private double[][] _chunks;
    private int _currentChunk;
    private int _currentIndex;

    public TelemetryBatchBuffer(int chunkSize = 50, int initialChunks = 4)
    {
        if (chunkSize <= 0) throw new ArgumentOutOfRangeException(nameof(chunkSize));
        if (initialChunks <= 0) throw new ArgumentOutOfRangeException(nameof(initialChunks));

        _chunkSize = chunkSize;
        _chunks = new double[initialChunks][];
        for (var i = 0; i < initialChunks; i++)
            _chunks[i] = new double[chunkSize];
    }

    /// <summary>Total number of readings currently buffered.</summary>
    public int Count => _currentChunk * _chunkSize + _currentIndex;

    /// <summary>Appends one raw reading, rolling over into a new jagged-array chunk when the current one fills up.</summary>
    public void Add(double value)
    {
        if (_currentIndex >= _chunkSize)
        {
            _currentChunk++;
            _currentIndex = 0;
            if (_currentChunk >= _chunks.Length)
                GrowChunks();
        }

        _chunks[_currentChunk][_currentIndex++] = value;
    }

    /// <summary>Doubles the number of chunk "rows" in the jagged array, preserving existing data.</summary>
    private void GrowChunks()
    {
        var grown = new double[_chunks.Length * 2][];
        Array.Copy(_chunks, grown, _chunks.Length);
        for (var i = _chunks.Length; i < grown.Length; i++)
            grown[i] = new double[_chunkSize];
        _chunks = grown;
    }

    /// <summary>Flattens all buffered jagged-array chunks into a single, contiguous <see cref="List{T}"/>.</summary>
    public List<double> FlushToList()
    {
        var result = new List<double>(Count);
        for (var chunk = 0; chunk <= _currentChunk; chunk++)
        {
            var upperBound = chunk == _currentChunk ? _currentIndex : _chunkSize;
            for (var i = 0; i < upperBound; i++)
                result.Add(_chunks[chunk][i]);
        }
        return result;
    }

    /// <summary>Simple rolling statistics used by the anomaly/health-score engine.</summary>
    public (double Mean, double StdDev) ComputeStatistics()
    {
        var values = FlushToList();
        if (values.Count == 0) return (0, 0);

        var mean = values.Average();
        var variance = values.Sum(v => Math.Pow(v - mean, 2)) / values.Count;
        return (mean, Math.Sqrt(variance));
    }
}
