using System.Text;
using woliver13.X12Net.IO;

namespace woliver13.X12Net.Tests.IO;

public class X12StreamingReaderTests
{
    private static string MakeInterchange(int bodySegmentCount)
    {
        var w = new X12Writer();
        w.WriteSegment("ISA", "00", "          ", "00", "          ",
            "ZZ", "SENDER         ", "ZZ", "RECEIVER       ",
            "201909", "1200", "^", "00501", "000000001", "0", "P", ":");
        w.WriteSegment("GS", "FA", "SENDER", "RECEIVER", "20190901", "1200", "1", "X", "005010X231A1");
        w.WriteSegment("ST", "999", "0001");
        for (int i = 0; i < bodySegmentCount; i++)
            w.WriteSegment("AK1", "FA", i.ToString(), "005010X231A1");
        w.WriteSegment("SE", (bodySegmentCount + 2).ToString(), "0001");
        w.WriteSegment("GE", "1", "1");
        w.WriteSegment("IEA", "1", "000000001");
        return w.ToString();
    }

    private static MemoryStream ToStream(string edi) =>
        new(Encoding.UTF8.GetBytes(edi));

    // ── Stream-based constructor (TD-14) ──────────────────────────────────

    [Fact]
    public void Stream_ReadAllSegments_returns_correct_segments()
    {
        string edi = MakeInterchange(bodySegmentCount: 2);
        using var stream = ToStream(edi);
        using var reader = new X12Reader(stream);

        var segments = reader.ReadAllSegments().ToList();

        Assert.True(segments.Count >= 3);
        Assert.Equal("ISA", segments[0].SegmentId);
        Assert.Equal("GS",  segments[1].SegmentId);
        Assert.Equal("IEA", segments[^1].SegmentId);
    }

    [Fact]
    public async Task Stream_ReadAllSegmentsAsync_yields_segments()
    {
        string edi = MakeInterchange(bodySegmentCount: 2);
        using var stream = ToStream(edi);
        using var reader = new X12Reader(stream);

        var segments = new List<string>();
        await foreach (var seg in reader.ReadAllSegmentsAsync())
            segments.Add(seg.SegmentId);

        Assert.Contains("ISA", segments);
        Assert.Contains("IEA", segments);
    }

    [Fact]
    public void Stream_ReadTransactions_returns_correct_transactions()
    {
        string edi = MakeInterchange(bodySegmentCount: 2);
        using var stream = ToStream(edi);
        using var reader = new X12Reader(stream);

        var txns = reader.ReadTransactions((st, body, se) => st[1]).ToList();

        Assert.Single(txns);
        Assert.Equal("999", txns[0]);
    }

    [Fact]
    public async Task Stream_ReadTransactionsAsync_returns_correct_transactions()
    {
        string edi = MakeInterchange(bodySegmentCount: 2);
        using var stream = ToStream(edi);
        using var reader = new X12Reader(stream);

        var txns = new List<string>();
        await foreach (var id in reader.ReadTransactionsAsync((st, body, se) => st[1]))
            txns.Add(id);

        Assert.Single(txns);
        Assert.Equal("999", txns[0]);
    }

    [Fact]
    public void Stream_MaxSegments_cap_enforced()
    {
        string edi = MakeInterchange(bodySegmentCount: 10); // 15 segments total
        using var stream = ToStream(edi);
        using var reader = new X12Reader(stream, maxSegments: 5);

        Assert.Throws<X12MemoryCapException>(() => reader.ReadAllSegments().ToList());
    }

    // ── Cycle 6 ───────────────────────────────────────────────────────────

    [Fact]
    public void StreamingReader_yields_segments_lazily_without_full_buffering()
    {
        string input = MakeInterchange(bodySegmentCount: 3);
        using var reader = new X12Reader(input);

        // IAsyncEnumerable lazy enumeration — each segment is yielded on demand
        var enumerator = reader.ReadAllSegmentsAsync().GetAsyncEnumerator();
        try
        {
            // Advance to first segment only
            var moved = enumerator.MoveNextAsync().AsTask().GetAwaiter().GetResult();
            Assert.True(moved);
            Assert.Equal("ISA", enumerator.Current.SegmentId);
        }
        finally
        {
            enumerator.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }

    // ── Cycle 7 ───────────────────────────────────────────────────────────

    [Fact]
    public void StreamingReader_throws_X12MemoryCapException_when_segment_limit_exceeded()
    {
        string input = MakeInterchange(bodySegmentCount: 10);  // ISA+GS+ST+10×AK1+SE+GE+IEA = 15 segs
        using var reader = new X12Reader(input, maxSegments: 5);

        var ex = Assert.Throws<X12MemoryCapException>(() =>
            reader.ReadAllSegments().ToList());

        Assert.Contains("5", ex.Message);
    }

    // ── Cycle 8 ───────────────────────────────────────────────────────────

    [Fact]
    public async Task StreamingReader_async_also_respects_segment_cap()
    {
        string input = MakeInterchange(bodySegmentCount: 10);
        using var reader = new X12Reader(input, maxSegments: 5);

        await Assert.ThrowsAsync<X12MemoryCapException>(async () =>
        {
            await foreach (var _ in reader.ReadAllSegmentsAsync()) { }
        });
    }
}
