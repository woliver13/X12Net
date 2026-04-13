using woliver13.X12Net.IO;

namespace woliver13.X12Net.Tests.IO;

public class X12WriterTests
{
    [Fact]
    public void Writer_writes_segment_with_element_separator_and_terminator()
    {
        var writer = new X12Writer();

        writer.WriteSegment("NM1", "IL", "1", "DOE", "JOHN");
        string result = writer.ToString();

        result.ShouldBe("NM1*IL*1*DOE*JOHN~");
    }

    [Fact]
    public void Writer_roundtrips_parsed_interchange_back_to_original()
    {
        const string original =
            "ISA*00*          *00*          *ZZ*SENDER         *ZZ*RECEIVER       *201909*1200*^*00501*000000001*0*P*:~" +
            "GS*FA*SENDER*RECEIVER*20190901*1200*1*X*005010X231A1~";

        using var reader = new X12Reader(original);
        var writer = new X12Writer();

        foreach (var seg in reader.ReadAllSegments())
            writer.WriteSegment(seg.SegmentId, seg.Elements.ToArray());

        writer.ToString().ShouldBe(original);
    }
}
