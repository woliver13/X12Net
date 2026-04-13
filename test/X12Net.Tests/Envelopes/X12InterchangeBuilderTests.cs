using woliver13.X12Net.DOM;
using woliver13.X12Net.Envelopes;
using woliver13.X12Net.IO;

namespace woliver13.X12Net.Tests.Envelopes;

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
        edi[3].ShouldBe('|');
        // Segments should be terminated with newline
        edi.ShouldContain("\n");
        // Default '*' should NOT appear as element separator
        edi.ShouldNotContain("ISA*");
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

        result.IsValid.ShouldBeTrue(string.Join("; ", result.Errors.Select(e => e.Message)));
        interchange.FunctionalGroups.Count.ShouldBe(2);
        interchange.FunctionalGroups[0].Transactions[0].ST[1].ShouldBe("999");
        interchange.FunctionalGroups[1].Transactions[0].ST[1].ShouldBe("271");
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

        result.IsValid.ShouldBeTrue(string.Join("; ", result.Errors.Select(e => e.Message)));
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

        interchange.ISA.SegmentId.ShouldBe("ISA");
        interchange.FunctionalGroups.ShouldHaveSingleItem();
        interchange.FunctionalGroups[0].Transactions[0].ST[1].ShouldBe("999");
        interchange.Delimiters.ElementSeparator.ShouldBe('|');
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

        interchange.ISA.SegmentId.ShouldBe("ISA");
        interchange.IEA.SegmentId.ShouldBe("IEA");
        interchange.FunctionalGroups.ShouldHaveSingleItem();
        interchange.FunctionalGroups[0].Transactions.ShouldHaveSingleItem();
        interchange.FunctionalGroups[0].Transactions[0].ST[1].ShouldBe("999");
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
        isa.ShouldStartWith("ISA*");
        isa.Length.ShouldBe(106);
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

        output.ShouldContain("GS*");
        output.ShouldContain("GE*");
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
        output.ShouldContain("*000000042*");  // ISA control number
        output.ShouldContain("GE*1*7~");      // GE02 = group control number
        output.ShouldContain("IEA*1*000000042~");
    }

    // ── Issue #9 ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void GE01_reflects_ST_segment_count(int transactionCount)
    {
        var builder = new X12InterchangeBuilder("SENDER", "RECEIVER", "201909", "1200");
        builder.BeginFunctionalGroup("FA", "SENDER", "RECEIVER", "20190901", "005010X231A1", groupControlNumber: 1);
        for (int i = 1; i <= transactionCount; i++)
        {
            builder.AddRawSegment($"ST*999*{i:D4}");
            builder.AddRawSegment($"SE*2*{i:D4}");
        }
        builder.EndFunctionalGroup();

        string edi = builder.Build();

        edi.ShouldContain($"GE*{transactionCount}*1~");
    }

    [Fact]
    public void Multi_transaction_group_passes_X12_validation()
    {
        var edi = new X12InterchangeBuilder("SENDER", "RECEIVER", "201909", "1200",
                      interchangeControlNumber: 1)
            .BeginFunctionalGroup("FA", "SENDER", "RECEIVER", "20190901", "005010X231A1", groupControlNumber: 1)
            .AddRawSegment("ST*999*0001")
            .AddRawSegment("AK1*FA*1*005010X231A1")
            .AddRawSegment("AK9*A*1*1*1")
            .AddRawSegment("SE*4*0001")
            .AddRawSegment("ST*999*0002")
            .AddRawSegment("AK1*FA*2*005010X231A1")
            .AddRawSegment("AK9*A*1*1*1")
            .AddRawSegment("SE*4*0002")
            .EndFunctionalGroup()
            .Build();

        var result = X12Net.Validation.X12Validator.Validate(edi);

        result.IsValid.ShouldBeTrue(string.Join("; ", result.Errors.Select(e => e.Message)));
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

        edi.ShouldContain("GS*FA*SENDER*RECEIVER*20190901*0930*");
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

        edi.ShouldContain($"GS*FA*SENDER*RECEIVER*20190901*{interchangeTime}*");
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

        segments.ShouldBe(new[] { "ISA", "GS", "ST", "AK1", "AK9", "SE", "GE", "IEA" });
    }

    // ── Issue #11 ─────────────────────────────────────────────────────────

    [Fact]
    public void ISA11_reflects_repetition_separator()
    {
        var edi = new X12InterchangeBuilder("SENDER", "RECEIVER", "201909", "1200",
                      repetitionSeparator: '!')
            .Build();

        edi[..106].ShouldContain("*!*");
    }

    [Fact]
    public void ISA12_reflects_isa_version()
    {
        var edi = new X12InterchangeBuilder("SENDER", "RECEIVER", "201909", "1200",
                      isaVersion: "00401")
            .Build();

        edi[..106].ShouldContain("*00401*");
    }

    [Fact]
    public void Build_004010_style_ISA_roundtrips_through_reader()
    {
        var edi = new X12InterchangeBuilder("SENDER", "RECEIVER", "201909", "1200",
                      isaVersion: "00401", repetitionSeparator: ':')
            .BeginFunctionalGroup("FA", "SENDER", "RECEIVER", "20190901", "004010X231A1", groupControlNumber: 1)
            .AddRawSegment("ST*999*0001")
            .AddRawSegment("SE*2*0001")
            .EndFunctionalGroup()
            .Build();

        edi[..106].ShouldContain("*00401*");
        edi[..106].ShouldContain("*:*");

        using var reader = new X12Reader(edi);
        var segIds = reader.ReadAllSegments().Select(s => s.SegmentId).ToList();
        segIds.ShouldBe(new[] { "ISA", "GS", "ST", "SE", "GE", "IEA" });
    }
}
