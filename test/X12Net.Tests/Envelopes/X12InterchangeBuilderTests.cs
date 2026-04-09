using X12Net.DOM;
using X12Net.Envelopes;
using X12Net.IO;

namespace X12Net.Tests.Envelopes;

public class X12InterchangeBuilderTests
{
    // ── Cycle 3 (Phase 5) ─────────────────────────────────────────────────

    [Fact]
    public void Builder_respects_custom_element_separator()
    {
        var edi = new X12InterchangeBuilder("SENDER", "RECEIVER", "190901", "1200",
                      elementSeparator: '|', componentSeparator: '>', segmentTerminator: '\n')
            .BeginFunctionalGroup("FA", "SENDER", "RECEIVER", "20190901", "005010X231A1")
            .AddRawSegment("ST|999|0001")
            .AddRawSegment("SE|2|0001")
            .EndFunctionalGroup()
            .Build();

        // ISA element 3 (index 3 in the raw string) should be the custom separator
        Assert.Equal('|', edi[3]);
        // Segments should be terminated with newline
        Assert.Contains('\n', edi);
        // Default '*' should NOT appear as element separator
        Assert.DoesNotContain("ISA*", edi);
    }

    // ── Cycle 2 (Phase 8) ─────────────────────────────────────────────────

    [Fact]
    public void Builder_with_two_functional_groups_passes_validation()
    {
        var edi = new X12InterchangeBuilder("SENDER", "RECEIVER", "190901", "1200",
                      interchangeControlNumber: 2)
            .BeginFunctionalGroup("FA", "SENDER", "RECEIVER", "20190901", "005010X231A1", groupControlNumber: 1)
            .AddRawSegment("ST*999*0001")
            .AddRawSegment("SE*2*0001")   // ST + SE = 2
            .EndFunctionalGroup()
            .BeginFunctionalGroup("HB", "SENDER", "RECEIVER", "20190901", "005010X279A1", groupControlNumber: 2)
            .AddRawSegment("ST*271*0001")
            .AddRawSegment("SE*2*0001")   // ST + SE = 2
            .EndFunctionalGroup()
            .Build();

        var result = X12Net.Validation.X12Validator.Validate(edi);
        var interchange = X12Net.DOM.X12Interchange.Parse(edi);

        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => e.Message)));
        Assert.Equal(2, interchange.FunctionalGroups.Count);
        Assert.Equal("999", interchange.FunctionalGroups[0].Transactions[0].ST[1]);
        Assert.Equal("271", interchange.FunctionalGroups[1].Transactions[0].ST[1]);
    }

    // ── Cycle 1 (Phase 7) ─────────────────────────────────────────────────

    [Fact]
    public void Builder_output_passes_full_structural_validation()
    {
        var edi = new X12InterchangeBuilder("SENDER", "RECEIVER", "190901", "1200")
            .BeginFunctionalGroup("FA", "SENDER", "RECEIVER", "20190901", "005010X231A1")
            .AddRawSegment("ST*999*0001")
            .AddRawSegment("AK1*FA*1*005010X231A1")
            .AddRawSegment("AK9*A*1*1*1")
            .AddRawSegment("SE*4*0001")
            .EndFunctionalGroup()
            .Build();

        var result = X12Net.Validation.X12Validator.Validate(edi);

        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => e.Message)));
    }

    // ── Cycle 4 (Phase 6) ─────────────────────────────────────────────────

    [Fact]
    public void Custom_delimiter_builder_output_parses_correctly()
    {
        var edi = new X12InterchangeBuilder("SENDER", "RECEIVER", "190901", "1200",
                      elementSeparator: '|', componentSeparator: '>', segmentTerminator: '\n')
            .BeginFunctionalGroup("FA", "SENDER", "RECEIVER", "20190901", "005010X231A1")
            .AddRawSegment("ST|999|0001")
            .AddRawSegment("AK1|FA|1|005010X231A1")
            .AddRawSegment("SE|3|0001")
            .EndFunctionalGroup()
            .Build();

        var interchange = X12Interchange.Parse(edi);

        Assert.Equal("ISA", interchange.ISA.SegmentId);
        Assert.Single(interchange.FunctionalGroups);
        Assert.Equal("999", interchange.FunctionalGroups[0].Transactions[0].ST[1]);
        Assert.Equal('|', interchange.Delimiters.ElementSeparator);
    }

    // ── Cycle 2 (Phase 4) ─────────────────────────────────────────────────

    [Fact]
    public void Builder_output_roundtrips_through_interchange_parse()
    {
        var edi = new X12InterchangeBuilder("SENDER", "RECEIVER", "190901", "1200")
            .BeginFunctionalGroup("FA", "SENDER", "RECEIVER", "20190901", "005010X231A1")
            .AddRawSegment("ST*999*0001")
            .AddRawSegment("AK1*FA*1*005010X231A1")
            .AddRawSegment("AK9*A*1*1*1")
            .AddRawSegment("SE*4*0001")
            .EndFunctionalGroup()
            .Build();

        var interchange = X12Interchange.Parse(edi);

        Assert.Equal("ISA", interchange.ISA.SegmentId);
        Assert.Equal("IEA", interchange.IEA.SegmentId);
        Assert.Single(interchange.FunctionalGroups);
        Assert.Single(interchange.FunctionalGroups[0].Transactions);
        Assert.Equal("999", interchange.FunctionalGroups[0].Transactions[0].ST[1]);
    }

    // ── Cycle 15 ──────────────────────────────────────────────────────────

    [Fact]
    public void Builder_creates_ISA_segment_with_correct_fixed_width()
    {
        var builder = new X12InterchangeBuilder(
            senderId:   "SENDER",
            receiverId: "RECEIVER",
            date:       "201909",
            time:       "1200");

        string output = builder.Build();

        // ISA is always exactly 106 chars before the first non-ISA segment
        string isa = output[..106];
        Assert.StartsWith("ISA*", isa);
        Assert.Equal(106, isa.Length);
    }

    // ── Cycle 16 ──────────────────────────────────────────────────────────

    [Fact]
    public void Builder_wraps_transaction_segments_with_GS_GE_pair()
    {
        var builder = new X12InterchangeBuilder("SENDER", "RECEIVER", "201909", "1200");
        builder.BeginFunctionalGroup("FA", "SENDER", "RECEIVER", "20190901", "005010X231A1");
        builder.AddRawSegment("ST*999*0001");
        builder.AddRawSegment("AK1*FA*1*005010X231A1");
        builder.AddRawSegment("AK9*A*1*1*1");
        builder.AddRawSegment("SE*4*0001");
        builder.EndFunctionalGroup();

        string output = builder.Build();

        Assert.Contains("GS*", output);
        Assert.Contains("GE*", output);
    }

    // ── Cycle 17 ──────────────────────────────────────────────────────────

    [Fact]
    public void Builder_assigns_sequential_control_numbers()
    {
        var builder = new X12InterchangeBuilder("SENDER", "RECEIVER", "201909", "1200",
            interchangeControlNumber: 42);
        builder.BeginFunctionalGroup("FA", "SENDER", "RECEIVER", "20190901", "005010X231A1",
            groupControlNumber: 7);
        builder.AddRawSegment("ST*999*0001");
        builder.AddRawSegment("AK1*FA*1*005010X231A1");
        builder.AddRawSegment("AK9*A*1*1*1");
        builder.AddRawSegment("SE*4*0001");
        builder.EndFunctionalGroup();

        string output = builder.Build();

        // ISA13 = interchange control number (9 chars, zero-padded), at fixed position
        Assert.Contains("*000000042*", output);  // ISA control number
        Assert.Contains("GE*1*7~",    output);   // GE02 = group control number
        Assert.Contains("IEA*1*000000042~", output);
    }

    // ── Issue #7 ─────────────────────────────────────────────────────────

    [Fact]
    public void BuildGS_uses_supplied_time_in_GS05()
    {
        var edi = new X12InterchangeBuilder("SENDER", "RECEIVER", "201909", "1200")
            .BeginFunctionalGroup("FA", "SENDER", "RECEIVER", "20190901", "005010X231A1",
                groupControlNumber: 1, time: "0930")
            .AddRawSegment("ST*999*0001")
            .AddRawSegment("AK1*FA*1*005010X231A1")
            .AddRawSegment("AK9*A*1*1*1")
            .AddRawSegment("SE*4*0001")
            .EndFunctionalGroup()
            .Build();

        Assert.Contains("GS*FA*SENDER*RECEIVER*20190901*0930*", edi);
    }

    [Fact]
    public void BuildGS_defaults_GS05_to_interchange_time_when_time_omitted()
    {
        const string interchangeTime = "0845";

        var edi = new X12InterchangeBuilder("SENDER", "RECEIVER", "201909", interchangeTime)
            .BeginFunctionalGroup("FA", "SENDER", "RECEIVER", "20190901", "005010X231A1")
            .AddRawSegment("ST*999*0001")
            .AddRawSegment("AK1*FA*1*005010X231A1")
            .AddRawSegment("AK9*A*1*1*1")
            .AddRawSegment("SE*4*0001")
            .EndFunctionalGroup()
            .Build();

        Assert.Contains($"GS*FA*SENDER*RECEIVER*20190901*{interchangeTime}*", edi);
    }

    // ── Cycle 18 ──────────────────────────────────────────────────────────

    [Fact]
    public void Builder_produces_fully_valid_round_trippable_interchange()
    {
        var builder = new X12InterchangeBuilder("SENDER", "RECEIVER", "201909", "1200",
            interchangeControlNumber: 1);
        builder.BeginFunctionalGroup("FA", "SENDER", "RECEIVER", "20190901", "005010X231A1",
            groupControlNumber: 1);
        builder.AddRawSegment("ST*999*0001");
        builder.AddRawSegment("AK1*FA*1*005010X231A1");
        builder.AddRawSegment("AK9*A*1*1*1");
        builder.AddRawSegment("SE*4*0001");
        builder.EndFunctionalGroup();

        string output = builder.Build();

        // Must round-trip cleanly through X12Reader
        using var reader = new X12Reader(output);
        var segments = reader.ReadAllSegments().Select(s => s.SegmentId).ToList();

        Assert.Equal(new[] { "ISA", "GS", "ST", "AK1", "AK9", "SE", "GE", "IEA" }, segments);
    }
}
