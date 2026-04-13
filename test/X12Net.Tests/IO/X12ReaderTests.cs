using woliver13.X12Net.Core;
using woliver13.X12Net.IO;

namespace woliver13.X12Net.Tests.IO;

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

        segments.Count.ShouldBe(2);
        segments[0].SegmentId.ShouldBe("ISA");
        segments[1].SegmentId.ShouldBe("GS");
    }

    [Fact]
    public void Reader_exposes_segment_id_and_elements()
    {
        using var reader = new X12Reader(SimpleInput);

        var gs = reader.ReadAllSegments().First(s => s.SegmentId == "GS");

        // GS*FA*SENDER*RECEIVER*20190901*1200*1*X*005010X231A1
        gs[1].ShouldBe("FA");              // GS01 — functional identifier code
        gs[2].ShouldBe("SENDER");          // GS02
        gs[3].ShouldBe("RECEIVER");        // GS03
        gs[4].ShouldBe("20190901");        // GS04 — date
        gs[8].ShouldBe("005010X231A1");    // GS08 — version
    }

    [Fact]
    public async Task Reader_reads_async_and_returns_same_segments()
    {
        using var reader = new X12Reader(SimpleInput);

        var segments = new List<X12Segment>();
        await foreach (var seg in reader.ReadAllSegmentsAsync())
            segments.Add(seg);

        segments.Count.ShouldBe(2);
        segments[0].SegmentId.ShouldBe("ISA");
        segments[1].SegmentId.ShouldBe("GS");
    }

    // ── Issue #10 ─────────────────────────────────────────────────────────

    [Fact]
    public void Reader_uses_ISA16_component_separator_for_composite_elements()
    {
        // ISA16 is '^' instead of the default ':'
        // CLM05 is a composite element: "11^B^1" (components separated by '^')
        const string edi =
            "ISA*00*          *00*          *ZZ*SENDER         *ZZ*RECEIVER       *201909*1200*^*00501*000000001*0*P*^~" +
            "GS*HC*SENDER*RECEIVER*20190901*1200*1*X*005010X222A1~" +
            "ST*837*0001~" +
            "CLM*PATIENT1*100***11^B^1~" +
            "SE*3*0001~" +
            "GE*1*1~" +
            "IEA*1*000000001~";

        using var reader = new X12Reader(edi);
        var segments = reader.ReadAllSegments().ToList();
        var clm = segments.First(s => s.SegmentId == "CLM");

        // CLM05 should be assembled with '^' as the component separator
        clm[5].ShouldBe("11^B^1");
    }

    [Fact]
    public void Dispose_releases_stream()
    {
        var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(SimpleInput));
        var reader = new X12Reader(ms);
        reader.Dispose();
        Should.Throw<ObjectDisposedException>(() => ms.ReadByte());
    }

    [Fact]
    public void Dispose_string_constructed_prevents_further_use()
    {
        var reader = new X12Reader(SimpleInput);
        reader.Dispose();
        Should.Throw<ObjectDisposedException>(() => reader.ReadAllSegments().ToList());
    }
}
