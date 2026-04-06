using X12Net.CLI;

namespace X12Net.Tests.CLI;

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
    public void ParseCommand_returns_ordered_segment_id_list()
    {
        var result = ParseCommand.Execute(ValidInterchange);

        Assert.Equal(
            new[] { "ISA", "GS", "ST", "AK1", "AK9", "SE", "GE", "IEA" },
            result.SegmentIds);
        Assert.True(result.Success);
    }

    // ── Cycle 14 ──────────────────────────────────────────────────────────

    [Fact]
    public void ValidateCommand_returns_empty_errors_for_valid_interchange()
    {
        var result = ValidateCommand.Execute(ValidInterchange);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    // ── Cycle 15 ──────────────────────────────────────────────────────────

    [Fact]
    public void ValidateCommand_returns_errors_for_mismatched_control_numbers()
    {
        string bad = ValidInterchange.Replace("IEA*1*000000001~", "IEA*1*000000099~");

        var result = ValidateCommand.Execute(bad);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.Contains("control number", result.Errors[0], StringComparison.OrdinalIgnoreCase);
    }

    // ── Cycle 16 ──────────────────────────────────────────────────────────

    [Fact]
    public void EditCommand_modifies_element_and_returns_updated_edi_text()
    {
        var result = EditCommand.Execute(
            input:       ValidInterchange,
            segmentId:   "GS",
            elementIndex: 2,           // GS02 = application sender code
            newValue:    "NEWSENDER");

        Assert.True(result.Success);
        Assert.Contains("GS*FA*NEWSENDER*", result.Output);
        // Original content unchanged in result.Output ISA line
        Assert.Contains("ISA*00*", result.Output);
    }
}
