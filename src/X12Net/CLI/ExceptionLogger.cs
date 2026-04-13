using Microsoft.Extensions.Logging;

namespace woliver13.X12Net.CLI;

/// <summary>
/// Utility that walks the full <see cref="Exception.InnerException"/> chain
/// and logs each level as a separate <see cref="LogLevel.Error"/> entry.
/// </summary>
internal static class ExceptionLogger
{
    /// <summary>
    /// Logs <paramref name="ex"/> and every inner exception in the chain.
    /// The top-level exception is logged without a depth label; inner exceptions
    /// are labelled <c>Inner[1]</c>, <c>Inner[2]</c>, etc.
    /// </summary>
    internal static void LogFullChain(ILogger logger, Exception ex, string context)
    {
        logger.LogError(ex, "[{Context}] {Message}", context, ex.Message);

        var inner = ex.InnerException;
        var depth = 1;
        while (inner is not null)
        {
            logger.LogError(inner, "[{Context}] Inner[{Depth}]: {Message}", context, depth, inner.Message);
            inner = inner.InnerException;
            depth++;
        }
    }
}
