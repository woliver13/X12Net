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

        envelope.SenderId.ShouldBe("ACME");
        envelope.ReceiverId.ShouldBe("PARTNER");
        envelope.InterchangeControlNumber.ShouldBe(42);
        envelope.DeclaredGroupCount.ShouldBe(1);
        envelope.IsValid.ShouldBeTrue();
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

        segments.Count.ShouldBe(4);
        segments[0].SegmentId.ShouldBe("ST");
        segments[1].SegmentId.ShouldBe("AK1");
        segments[2].SegmentId.ShouldBe("AK9");
        segments[3].SegmentId.ShouldBe("SE");
    }

    // ── Cycle 21 ──────────────────────────────────────────────────────────

    [Fact]
    public void Envelope_parses_ISA_control_fields()
    {
        var env = X12Envelope.Parse(FullInterchange);

        env.SenderId.ShouldBe("SENDER");
        env.ReceiverId.ShouldBe("RECEIVER");
        env.Date.ShouldBe("201909");
        env.Time.ShouldBe("1200");
        env.InterchangeControlNumber.ShouldBe(42);
    }

    // ── Cycle 22 ──────────────────────────────────────────────────────────

    [Fact]
    public void Envelope_validates_IEA_group_count_matches_GS_count()
    {
        var env = X12Envelope.Parse(FullInterchange);

        // IEA01 (number of functional groups) must match actual GS count
        env.IsValid.ShouldBeTrue(env.ValidationMessage);
    }

    [Fact]
    public void Envelope_detects_mismatched_IEA_group_count()
    {
        // Corrupt the IEA count: IEA*1 → IEA*9 while only 1 GS exists
        string bad = FullInterchange.Replace("IEA*1*", "IEA*9*");
        var env = X12Envelope.Parse(bad);

        env.IsValid.ShouldBeFalse();
        env.ValidationMessage.ShouldContain("group count", Case.Insensitive);
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

        output.ShouldStartWith("ST*");
        output.ShouldNotContain("ISA");
        output.ShouldNotContain("GS");
        output.ShouldEndWith("SE*2*0001~");
    }
}
