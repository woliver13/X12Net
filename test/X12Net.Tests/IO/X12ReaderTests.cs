using X12Net.Core;
using X12Net.IO;

namespace X12Net.Tests.IO;

public class X12ReaderTests
{
    // Two-segment interchange: ISA + GS (no IEA/GE for brevity)
    private const string SimpleInput =
        "ISA*00*          *00*          *ZZ*SENDER         *ZZ*RECEIVER       *201909*1200*^*00501*000000001*0*P*:~" +
        "GS*FA*SENDER*RECEIVER*20190901*1200*1*X*005010X231A1~";

    [Fact]
    public void Reader_returns_all_segments_from_simple_interchange()
    {
        using var reader = new X12Reader(SimpleInput);

        var segments = reader.ReadAllSegments().ToList();

        Assert.Equal(2, segments.Count);
        Assert.Equal("ISA", segments[0].SegmentId);
        Assert.Equal("GS",  segments[1].SegmentId);
    }

    [Fact]
    public void Reader_exposes_segment_id_and_elements()
    {
        using var reader = new X12Reader(SimpleInput);

        var gs = reader.ReadAllSegments().First(s => s.SegmentId == "GS");

        // GS*FA*SENDER*RECEIVER*20190901*1200*1*X*005010X231A1
        Assert.Equal("FA",              gs[1]);  // GS01 — functional identifier code
        Assert.Equal("SENDER",          gs[2]);  // GS02
        Assert.Equal("RECEIVER",        gs[3]);  // GS03
        Assert.Equal("20190901",        gs[4]);  // GS04 — date
        Assert.Equal("005010X231A1",    gs[8]);  // GS08 — version
    }

    [Fact]
    public async Task Reader_reads_async_and_returns_same_segments()
    {
        using var reader = new X12Reader(SimpleInput);

        var segments = new List<X12Segment>();
        await foreach (var seg in reader.ReadAllSegmentsAsync())
            segments.Add(seg);

        Assert.Equal(2, segments.Count);
        Assert.Equal("ISA", segments[0].SegmentId);
        Assert.Equal("GS",  segments[1].SegmentId);
    }
}
