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

    private static List<X12Transaction> ReadAll(string input)
    {
        var delimiters = X12Delimiters.FromIsa(input);
        using var reader = new X12Reader(input, delimiters);
        return reader.ReadTransactions().ToList();
    }

    // ── Cycle 1 ───────────────────────────────────────────────────────────

    [Fact]
    public void Reader_ReadTransactions_yields_transactions_in_document_order()
    {
        var txns = ReadAll(TwoGroupInterchange);

        Assert.Equal(2, txns.Count);
        Assert.Equal("999", txns[0].ST[1]);
        Assert.Equal("271", txns[1].ST[1]);
    }

    // ── Cycle 2 ───────────────────────────────────────────────────────────

    [Fact]
    public void Reader_ReadTransactions_spans_multiple_functional_groups()
    {
        var txns = ReadAll(TwoGroupInterchange);

        // Body of tx[0]: AK1 only (GS/GE are envelope, not body)
        Assert.Single(txns[0].Segments);
        Assert.Equal("AK1", txns[0].Segments[0].SegmentId);

        // Body of tx[1]: BHT only
        Assert.Single(txns[1].Segments);
        Assert.Equal("BHT", txns[1].Segments[0].SegmentId);
    }

    // ── Cycle 1 (Phase 4) ─────────────────────────────────────────────────

    [Fact]
    public async Task ReadTransactionsAsync_yields_transactions_in_document_order()
    {
        var delimiters = X12Delimiters.FromIsa(TwoGroupInterchange);
        using var reader = new X12Reader(TwoGroupInterchange, delimiters);

        var txns = new List<X12Transaction>();
        await foreach (var tx in reader.ReadTransactionsAsync())
            txns.Add(tx);

        Assert.Equal(2, txns.Count);
        Assert.Equal("999", txns[0].ST[1]);
        Assert.Equal("271", txns[1].ST[1]);
    }

    // ── Cycle 1 (Phase 5) ─────────────────────────────────────────────────

    [Fact]
    public async Task CancellationToken_cancels_ReadTransactionsAsync_mid_stream()
    {
        var cts = new CancellationTokenSource();
        var delimiters = X12Delimiters.FromIsa(TwoGroupInterchange);
        using var reader = new X12Reader(TwoGroupInterchange, delimiters);

        var txns = new List<X12Transaction>();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var tx in reader.ReadTransactionsAsync(cts.Token))
            {
                txns.Add(tx);
                cts.Cancel();   // cancel after first transaction
            }
        });

        Assert.Single(txns);    // only the first transaction was yielded
    }

    // ── Cycle 5 (Phase 4) ─────────────────────────────────────────────────

    [Fact]
    public void MemoryCap_is_enforced_on_ReadTransactions()
    {
        // TwoGroupInterchange has 12 segments total; cap at 5 should throw before any SE
        var delimiters = X12Delimiters.FromIsa(TwoGroupInterchange);
        using var reader = new X12Reader(TwoGroupInterchange, delimiters, maxSegments: 5);

        Assert.Throws<X12MemoryCapException>(() => reader.ReadTransactions().ToList());
    }
}
