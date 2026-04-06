using X12Net.Core;
using X12Net.DOM;
using X12Net.IO;

namespace X12Net.Tests.DOM;

public class X12TransactionTests
{
    private static readonly X12Delimiters Delimiters = X12Delimiters.Default;

    private const string FullInterchange =
        "ISA*00*          *00*          *ZZ*SENDER         *ZZ*RECEIVER       *201909*1200*^*00501*000000001*0*P*:~" +
        "GS*FA*SENDER*RECEIVER*20190901*1200*1*X*005010X231A1~" +
        "ST*999*0001~" +
        "AK1*FA*1*005010X231A1~" +
        "AK9*A*1*1*1~" +
        "SE*4*0001~" +
        "GE*1*1~" +
        "IEA*1*000000001~";

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

        Assert.Equal(4, segments.Count);
        Assert.Equal("ST",  segments[0].SegmentId);
        Assert.Equal("AK1", segments[1].SegmentId);
        Assert.Equal("AK9", segments[2].SegmentId);
        Assert.Equal("SE",  segments[3].SegmentId);
        Assert.Equal("999", segments[0][1]);   // ST01 transaction set ID
    }

    // ── Cycle 3 ───────────────────────────────────────────────────────────

    [Fact]
    public void Transaction_ToEdi_produces_valid_st_se_text()
    {
        var interchange = X12Interchange.Parse(FullInterchange);
        var tx = interchange.FunctionalGroups[0].Transactions[0];

        var edi = tx.ToEdi(interchange.Delimiters);

        // Must start with ST and end with SE~
        Assert.StartsWith("ST*", edi);
        Assert.EndsWith("SE*4*0001~", edi);

        // Re-parsing the text must yield ST, body segments, SE
        using var reader = new X12Reader(edi, interchange.Delimiters);
        var segments = reader.ReadAllSegments().ToList();
        Assert.Equal("ST",  segments[0].SegmentId);
        Assert.Equal("AK1", segments[1].SegmentId);
        Assert.Equal("AK9", segments[2].SegmentId);
        Assert.Equal("SE",  segments[3].SegmentId);
    }
}
