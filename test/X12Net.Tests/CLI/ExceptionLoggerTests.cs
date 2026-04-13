using Microsoft.Extensions.Logging;
using woliver13.X12Net.CLI;
using X12Net.Tests.Helpers;

namespace X12Net.Tests.CLI;

public class ExceptionLoggerTests
{
    [Fact]
    public void LogFullChain_logs_top_level_exception()
    {
        var logger = new CapturingLogger();
        var ex = new Exception("top-level error");

        ExceptionLogger.LogFullChain(logger, ex, "TestCtx");

        Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, logger.Entries[0].Level);
        Assert.Contains("top-level error", logger.Entries[0].Message);
    }

    [Fact]
    public void LogFullChain_logs_inner_exception_at_depth_1()
    {
        var logger = new CapturingLogger();
        var inner = new Exception("inner error");
        var ex    = new Exception("outer error", inner);

        ExceptionLogger.LogFullChain(logger, ex, "TestCtx");

        Assert.Equal(2, logger.Entries.Count);
        Assert.Contains("outer error",  logger.Entries[0].Message);
        Assert.Contains("Inner[1]",     logger.Entries[1].Message);
        Assert.Contains("inner error",  logger.Entries[1].Message);
    }

    [Fact]
    public void LogFullChain_logs_deep_chain()
    {
        var logger  = new CapturingLogger();
        var level2  = new Exception("deepest error");
        var level1  = new Exception("middle error", level2);
        var ex      = new Exception("outer error",  level1);

        ExceptionLogger.LogFullChain(logger, ex, "TestCtx");

        Assert.Equal(3, logger.Entries.Count);
        Assert.Contains("outer error",   logger.Entries[0].Message);
        Assert.Contains("Inner[1]",      logger.Entries[1].Message);
        Assert.Contains("middle error",  logger.Entries[1].Message);
        Assert.Contains("Inner[2]",      logger.Entries[2].Message);
        Assert.Contains("deepest error", logger.Entries[2].Message);
    }
}
