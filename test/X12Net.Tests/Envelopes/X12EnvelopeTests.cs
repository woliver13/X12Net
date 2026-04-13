using woliver13.X12Net.Envelopes;
using woliver13.X12Net.IO;

namespace woliver13.X12Net.Tests.Envelopes;

public class X12EnvelopeTests
{
    // ── Cycle 3 (Phase 8) ─────────────────────────────────────────────────

    [Fact]
    public void Envelope_Parse_extracts_fields_from_builder_output()
    {
        var edi = new X12InterchangeBuilder(
                      senderId: "ACME",
                      receiverId: "PARTNER",
                      date: "190901",
                      time: "1200",
                      interchangeControlNumber: 42)
            .BeginFunctionalGroup("FA", "ACME", "PARTNER", "20190901", "005010X231A1")
            .AddRawSegment("ST*999*0001")
            .AddRawSegment("SE*2*0001")
            .EndFunctionalGroup()
            .Build();

        var envelope = X12Envelope.Parse(edi);

        Assert.Equal("ACME",    envelope.SenderId);
        Assert.Equal("PARTNER", envelope.ReceiverId);
        Assert.Equal(42,        envelope.InterchangeControlNumber);
        Assert.Equal(1,         envelope.DeclaredGroupCount);
        Assert.True(envelope.IsValid);
    }


    private const string FullInterchange =
        "ISA*00*          *00*          *ZZ*SENDER         *ZZ*RECEIVER       *201909*1200*^*00501*000000042*0*P*:~" +
        "GS*FA*SENDER*RECEIVER*20190901*1200*7*X*005010X231A1~" +
        "ST*999*0001~" +
        "AK1*FA*1*005010X231A1~" +
        "AK9*A*1*1*1~" +
        "SE*4*0001~" +
        "GE*1*7~" +
        "IEA*1*000000042~";

    // ── Cycle 20 ──────────────────────────────────────────────────────────

    [Fact]
    public void Reader_parses_transaction_without_ISA_GS_envelope()
    {
        // Transaction-only mode: just the ST/SE block
        const string txOnly =
            "ST*999*0001~" +
            "AK1*FA*1*005010X231A1~" +
            "AK9*A*1*1*1~" +
            "SE*4*0001~";

        using var reader = new X12Reader(txOnly);
        var segments = reader.ReadAllSegments().ToList();

        Assert.Equal(4, segments.Count);
        Assert.Equal("ST",  segments[0].SegmentId);
        Assert.Equal("AK1", segments[1].SegmentId);
        Assert.Equal("AK9", segments[2].SegmentId);
        Assert.Equal("SE",  segments[3].SegmentId);
    }

    // ── Cycle 21 ──────────────────────────────────────────────────────────

    [Fact]
    public void Envelope_parses_ISA_control_fields()
    {
        var env = X12Envelope.Parse(FullInterchange);

        Assert.Equal("SENDER",    env.SenderId);
        Assert.Equal("RECEIVER",  env.ReceiverId);
        Assert.Equal("201909",    env.Date);
        Assert.Equal("1200",      env.Time);
        Assert.Equal(42,          env.InterchangeControlNumber);
    }

    // ── Cycle 22 ──────────────────────────────────────────────────────────

    [Fact]
    public void Envelope_validates_IEA_group_count_matches_GS_count()
    {
        var env = X12Envelope.Parse(FullInterchange);

        // IEA01 (number of functional groups) must match actual GS count
        Assert.True(env.IsValid, env.ValidationMessage);
    }

    [Fact]
    public void Envelope_detects_mismatched_IEA_group_count()
    {
        // Corrupt the IEA count: IEA*1 → IEA*9 while only 1 GS exists
        string bad = FullInterchange.Replace("IEA*1*", "IEA*9*");
        var env = X12Envelope.Parse(bad);

        Assert.False(env.IsValid);
        Assert.Contains("group count", env.ValidationMessage, StringComparison.OrdinalIgnoreCase);
    }

    // ── Cycle 23 ──────────────────────────────────────────────────────────

    [Fact]
    public void Writer_produces_transaction_segment_only_output()
    {
        var writer = new X12Writer();
        writer.WriteSegment("ST",  "999", "0001");
        writer.WriteSegment("AK1", "FA",  "1", "005010X231A1");
        writer.WriteSegment("SE",  "2",   "0001");

        string output = writer.ToString();

        Assert.StartsWith("ST*", output);
        Assert.DoesNotContain("ISA", output);
        Assert.DoesNotContain("GS",  output);
        Assert.EndsWith("SE*2*0001~", output);
    }
}
