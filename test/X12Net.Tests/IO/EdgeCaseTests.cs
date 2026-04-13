using woliver13.X12Net.IO;

namespace woliver13.X12Net.Tests.IO;

public class EdgeCaseTests
{
    // ── Cycle 7 ───────────────────────────────────────────────────────────

    [Fact]
    public void Reader_accepts_explicit_delimiters_via_overload()
    {
        // Non-ISA input with pipe/caret/newline delimiters — auto-detection won't fire
        const string input = "NM1|IL|1|SMITH|\n";
        var delimiters = new X12Net.Core.X12Delimiters('|', '^', '\n');

        using var reader = new X12Reader(input, delimiters);
        var seg = reader.ReadAllSegments().Single();

        Assert.Equal("NM1",   seg.SegmentId);
        Assert.Equal("IL",    seg[1]);
        Assert.Equal("1",     seg[2]);
        Assert.Equal("SMITH", seg[3]);
        Assert.Equal("",      seg[4]);
    }

    // ── Cycle 2 ───────────────────────────────────────────────────────────

    [Fact]
    public void Writer_roundtrips_composite_element_with_component_separator()
    {
        var writer = new X12Writer();
        writer.WriteSegment("EB", "1", "30:UC");  // EB01=1, EB02=composite "30:UC"
        string edi = writer.ToString();

        using var reader = new X12Reader(edi);
        var seg = reader.ReadAllSegments().Single();

        Assert.Equal("EB",    seg.SegmentId);
        Assert.Equal("1",     seg[1]);
        Assert.Equal("30:UC", seg[2]);   // composite preserved through round-trip
    }

    // ── Cycle 1 ───────────────────────────────────────────────────────────

    [Fact]
    public void Reader_handles_empty_element_between_delimiters()
    {
        // NM1 with ISA skipped — raw segment, empty NM1-09 via consecutive **
        const string input = "NM1*IL*1*SMITH*JOHN**A*MI~";

        using var reader = new X12Reader(input);
        var seg = reader.ReadAllSegments().Single();

        Assert.Equal("NM1", seg.SegmentId);
        Assert.Equal(7, seg.Elements.Count);
        Assert.Equal("IL",    seg[1]);
        Assert.Equal("1",     seg[2]);
        Assert.Equal("SMITH", seg[3]);
        Assert.Equal("JOHN",  seg[4]);
        Assert.Equal("",      seg[5]);   // empty element
        Assert.Equal("A",     seg[6]);
        Assert.Equal("MI",    seg[7]);
    }
}
