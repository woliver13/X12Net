using woliver13.X12Net.Core;
using woliver13.X12Net.DOM;
using woliver13.X12Net.IO;

namespace woliver13.X12Net.Tests.DOM;

public class X12TransactionTests
{
    private static readonly X12Delimiters Delimiters = X12Delimiters.Default;

    private const string FullInterchange = Fixtures.Edi.Valid999;

    // ── Cycle 4 ───────────────────────────────────────────────────────────

    [Fact]
    public void Reader_handles_transaction_only_fragment()
    {
        // No ISA/GS envelope — just the ST…SE transaction body
        const string fragment =
            "ST*999*0001~" +
            "AK1*FA*1*005010X231A1~" +
            "AK9*A*1*1*1~" +
            "SE*4*0001~";

        using var reader = new X12Reader(fragment, Delimiters);
        var segments = reader.ReadAllSegments().ToList();

        segments.Count.ShouldBe(4);
        segments[0].SegmentId.ShouldBe("ST");
        segments[1].SegmentId.ShouldBe("AK1");
        segments[2].SegmentId.ShouldBe("AK9");
        segments[3].SegmentId.ShouldBe("SE");
        segments[0][1].ShouldBe("999");   // ST01 transaction set ID
    }

    // ── Cycle 3 ───────────────────────────────────────────────────────────

    [Fact]
    public void Transaction_ToEdi_produces_valid_st_se_text()
    {
        var interchange = X12Interchange.Parse(FullInterchange);
        var tx = interchange.FunctionalGroups[0].Transactions[0];

        var edi = tx.ToEdi(interchange.Delimiters);

        // Must start with ST and end with SE~
        edi.ShouldStartWith("ST*");
        edi.ShouldEndWith("SE*4*0001~");

        // Re-parsing the text must yield ST, body segments, SE
        using var reader = new X12Reader(edi, interchange.Delimiters);
        var segments = reader.ReadAllSegments().ToList();
        segments[0].SegmentId.ShouldBe("ST");
        segments[1].SegmentId.ShouldBe("AK1");
        segments[2].SegmentId.ShouldBe("AK9");
        segments[3].SegmentId.ShouldBe("SE");
    }
}
