using woliver13.X12Net.CLI;

namespace woliver13.X12Net.Tests.CLI;

public class X12ToolCommandTests
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

    // ── Cycle 13 ──────────────────────────────────────────────────────────

    [Fact]
    public void Parse_returns_ordered_segment_id_list()
    {
        var result = X12Tool.Parse(ValidInterchange);

        result.SegmentIds.ShouldBe(new[] { "ISA", "GS", "ST", "AK1", "AK9", "SE", "GE", "IEA" });
        result.Success.ShouldBeTrue();
    }

    // ── Cycle 14 ──────────────────────────────────────────────────────────

    [Fact]
    public void Validate_returns_empty_errors_for_valid_interchange()
    {
        var result = X12Tool.Validate(ValidInterchange);

        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    // ── Cycle 15 ──────────────────────────────────────────────────────────

    [Fact]
    public void Validate_returns_errors_for_mismatched_control_numbers()
    {
        string bad = ValidInterchange.Replace("IEA*1*000000001~", "IEA*1*000000099~");

        var result = X12Tool.Validate(bad);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldNotBeEmpty();
        result.Errors[0].ShouldContain("control number", Case.Insensitive);
    }

    // ── Cycle 16 ──────────────────────────────────────────────────────────

    [Fact]
    public void Edit_modifies_element_and_returns_updated_edi_text()
    {
        var result = X12Tool.Edit(
            input:        ValidInterchange,
            segmentId:    "GS",
            elementIndex: 2,           // GS02 = application sender code
            newValue:     "NEWSENDER");

        result.Success.ShouldBeTrue();
        result.Output.ShouldContain("GS*FA*NEWSENDER*");
        result.Output.ShouldContain("ISA*00*");
    }

    // ── Cycle 1 (Issue 4) ─────────────────────────────────────────────────

    [Fact]
    public void X12Tool_Parse_returns_ordered_segment_ids()
    {
        var result = X12Tool.Parse(ValidInterchange);

        result.Success.ShouldBeTrue();
        result.SegmentIds.ShouldBe(new[] { "ISA", "GS", "ST", "AK1", "AK9", "SE", "GE", "IEA" });
    }

    [Fact]
    public void X12Tool_Validate_returns_no_errors_for_valid_interchange()
    {
        var result = X12Tool.Validate(ValidInterchange);

        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void X12Tool_Edit_modifies_element_and_returns_updated_edi()
    {
        var result = X12Tool.Edit(ValidInterchange, "GS", 2, "NEWSENDER");

        result.Success.ShouldBeTrue();
        result.Output.ShouldContain("GS*FA*NEWSENDER*");
    }
}
