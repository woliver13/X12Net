using Microsoft.Extensions.Logging;
using woliver13.X12Net.CLI;

namespace woliver13.X12Net.Tests.CLI;

public class ExceptionLoggerTests
{
    private static List<(LogLevel Level, string Message)> GetLogEntries(ILogger logger) =>
        logger.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == nameof(ILogger.Log))
            .Select(c => (
                Level:   (LogLevel)c.GetArguments()[0]!,
                Message: c.GetArguments()[2]!.ToString()!
            ))
            .ToList();

    [Fact]
    public void LogFullChain_logs_top_level_exception()
    {
        var logger = Substitute.For<ILogger>();
        var ex = new Exception("top-level error");

        ExceptionLogger.LogFullChain(logger, ex, "TestCtx");

        var entries = GetLogEntries(logger);
        entries.ShouldHaveSingleItem();
        entries[0].Level.ShouldBe(LogLevel.Error);
        entries[0].Message.ShouldContain("top-level error");
    }

    [Fact]
    public void LogFullChain_logs_inner_exception_at_depth_1()
    {
        var logger = Substitute.For<ILogger>();
        var inner = new Exception("inner error");
        var ex    = new Exception("outer error", inner);

        ExceptionLogger.LogFullChain(logger, ex, "TestCtx");

        var entries = GetLogEntries(logger);
        entries.Count.ShouldBe(2);
        entries[0].Message.ShouldContain("outer error");
        entries[1].Message.ShouldContain("Inner[1]");
        entries[1].Message.ShouldContain("inner error");
    }

    [Fact]
    public void LogFullChain_logs_deep_chain()
    {
        var logger  = Substitute.For<ILogger>();
        var level2  = new Exception("deepest error");
        var level1  = new Exception("middle error", level2);
        var ex      = new Exception("outer error",  level1);

        ExceptionLogger.LogFullChain(logger, ex, "TestCtx");

        var entries = GetLogEntries(logger);
        entries.Count.ShouldBe(3);
        entries[0].Message.ShouldContain("outer error");
        entries[1].Message.ShouldContain("Inner[1]");
        entries[1].Message.ShouldContain("middle error");
        entries[2].Message.ShouldContain("Inner[2]");
        entries[2].Message.ShouldContain("deepest error");
    }
}
