using Microsoft.Extensions.Logging;

namespace X12Net.Tests.Helpers;

/// <summary>
/// Simple ILogger that captures every log call for assertion in tests.
/// </summary>
public sealed class CapturingLogger : ILogger
{
    public record Entry(LogLevel Level, string Message, Exception? Exception);

    private readonly List<Entry> _entries = new();

    public IReadOnlyList<Entry> Entries => _entries;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel                         logLevel,
        EventId                          eventId,
        TState                           state,
        Exception?                       exception,
        Func<TState, Exception?, string> formatter)
    {
        _entries.Add(new Entry(logLevel, formatter(state, exception), exception));
    }
}
