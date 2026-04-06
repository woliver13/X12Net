using X12Net.Envelopes;
using X12Net.IO;

namespace X12Net.Tests.Envelopes;

public class X12InterchangeBuilderTests
{
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
