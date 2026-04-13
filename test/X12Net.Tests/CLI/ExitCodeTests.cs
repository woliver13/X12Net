using woliver13.X12Net.CLI;

namespace woliver13.X12Net.Tests.CLI;

public class ExitCodeTests
{
    [Fact]
    public void ExitCode_constants_have_correct_values()
    {
        ExitCode.Success.ShouldBe(0);
        ExitCode.UnexpectedError.ShouldBe(1);
        ExitCode.UsageError.ShouldBe(2);
        ExitCode.TempFailure.ShouldBe(75);
        ExitCode.ConfigError.ShouldBe(78);
    }
}
