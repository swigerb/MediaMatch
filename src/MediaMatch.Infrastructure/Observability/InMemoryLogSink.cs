using System.Collections.Concurrent;
using Serilog.Core;
using Serilog.Events;

namespace MediaMatch.Infrastructure.Observability;

/// <summary>
/// In-memory Serilog sink that buffers recent log events for UI display.
/// Thread-safe circular buffer with configurable capacity.
/// </summary>
public sealed class InMemoryLogSink : ILogEventSink
{
    private readonly ConcurrentQueue<LogEvent> _events = new();
    private readonly int _capacity;

    /// <summary>Fires when a new log event is written.</summary>
    public event Action<LogEvent>? LogReceived;

    public InMemoryLogSink(int capacity = 2000)
    {
        _capacity = capacity;
    }

    public void Emit(LogEvent logEvent)
    {
        _events.Enqueue(logEvent);

        // Trim oldest if over capacity
        while (_events.Count > _capacity && _events.TryDequeue(out _)) { }

        LogReceived?.Invoke(logEvent);
    }

    /// <summary>Returns a snapshot of all buffered log events.</summary>
    public IReadOnlyList<LogEvent> GetEvents() => _events.ToArray();

    /// <summary>Clears all buffered log events.</summary>
    public void Clear()
    {
        while (_events.TryDequeue(out _)) { }
    }

    /// <summary>Shared singleton instance wired into Serilog.</summary>
    public static InMemoryLogSink Instance { get; } = new();
}
