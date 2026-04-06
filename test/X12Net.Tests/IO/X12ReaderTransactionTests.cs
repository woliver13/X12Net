using X12Net.Core;
using X12Net.DOM;
using X12Net.IO;

namespace X12Net.Tests.IO;

public class X12ReaderTransactionTests
{
    private const string TwoGroupInterchange =
        "ISA*00*          *00*          *ZZ*SENDER         *ZZ*RECEIVER       *201909*1200*^*00501*000000001*0*P*:~" +
        "GS*FA*SENDER*RECEIVER*20190901*1200*1*X*005010X231A1~" +
        "ST*999*0001~" +
        "AK1*FA*1*005010X231A1~" +
        "SE*3*0001~" +
        "GE*1*1~" +
        "GS*HB*SENDER*RECEIVER*20190901*1200*2*X*005010X279A1~" +
        "ST*271*0001~" +
        "BHT*0022*11*10001234*20190901*1200~" +
        "SE*2*0001~" +
        "GE*1*2~" +
        "IEA*2*000000001~";

    // ── Cycle 2 ───────────────────────────────────────────────────────────

    [Fact]
    public void Reader_ReadTransactions_spans_multiple_functional_groups()
    {
        var delimiters = X12Delimiters.FromIsa(TwoGroupInterchange);
        using var reader = new X12Reader(TwoGroupInterchange, delimiters);

        var txns = reader.ReadTransactions().ToList();

        // Body of tx[0]: AK1 only (GS/GE are envelope, not body)
        Assert.Single(txns[0].Segments);
        Assert.Equal("AK1", txns[0].Segments[0].SegmentId);

        // Body of tx[1]: BHT only
        Assert.Single(txns[1].Segments);
        Assert.Equal("BHT", txns[1].Segments[0].SegmentId);
    }

    // ── Cycle 1 ───────────────────────────────────────────────────────────

    [Fact]
    public void Reader_ReadTransactions_yields_transactions_in_document_order()
    {
        var delimiters = X12Delimiters.FromIsa(TwoGroupInterchange);
        using var reader = new X12Reader(TwoGroupInterchange, delimiters);

        var txns = reader.ReadTransactions().ToList();

        Assert.Equal(2, txns.Count);
        Assert.Equal("999", txns[0].ST[1]);
        Assert.Equal("271", txns[1].ST[1]);
    }
}
