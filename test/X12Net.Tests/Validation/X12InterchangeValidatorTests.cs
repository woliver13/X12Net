using woliver13.X12Net.Validation;

namespace woliver13.X12Net.Tests.Validation;

public class X12InterchangeValidatorTests
{
    private const string ValidInterchange =
        "ISA*00*          *00*          *ZZ*SENDER         *ZZ*RECEIVER       *201909*1200*^*00501*000000001*0*P*:~" +
        "GS*FA*SENDER*RECEIVER*20190901*1200*1*X*005010X231A1~" +
        "ST*999*0001~" +
        "AK1*FA*1*005010X231A1~" +
        "AK9*A*1*1*1~" +
        "SE*4*0001~" +
        "GE*1*1~" +
        "IEA*1*000000001~";

    private const string MismatchedControlNumber =
        "ISA*00*          *00*          *ZZ*SENDER         *ZZ*RECEIVER       *201909*1200*^*00501*000000001*0*P*:~" +
        "GS*FA*SENDER*RECEIVER*20190901*1200*1*X*005010X231A1~" +
        "ST*999*0001~" +
        "AK1*FA*1*005010X231A1~" +
        "AK9*A*1*1*1~" +
        "SE*4*0001~" +
        "GE*1*1~" +
        "IEA*1*999999999~";  // IEA02 doesn't match ISA13

    // ── Cycle 4 ───────────────────────────────────────────────────────────

    [Fact]
    public void FluentValidator_fails_for_mismatched_control_numbers()
    {
        var validator = new X12InterchangeValidator();

        var result = validator.Validate(MismatchedControlNumber);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorCode == "ControlNumberMismatch");
    }

    // ── Cycle 3 ───────────────────────────────────────────────────────────

    [Fact]
    public void FluentValidator_passes_for_valid_interchange()
    {
        var validator = new X12InterchangeValidator();

        var result = validator.Validate(ValidInterchange);

        Assert.True(result.IsValid);
    }
}
