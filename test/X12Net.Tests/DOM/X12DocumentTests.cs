using X12Net.DOM;

namespace X12Net.Tests.DOM;

public class X12DocumentTests
{
    private const string TwoSegmentInput =
        "ISA*00*          *00*          *ZZ*SENDER         *ZZ*RECEIVER       *201909*1200*^*00501*000000001*0*P*:~" +
        "GS*FA*SENDER*RECEIVER*20190901*1200*1*X*005010X231A1~";

    [Fact]
    public void Document_provides_segment_access_by_index()
    {
        var doc = X12Document.Parse(TwoSegmentInput);

        Assert.Equal("ISA", doc.Segments[0].SegmentId);
        Assert.Equal("GS",  doc.Segments[1].SegmentId);
    }

    [Fact]
    public void Document_provides_element_access_by_indexer()
    {
        var doc = X12Document.Parse(TwoSegmentInput);

        // doc["GS", 1] = first element of the first GS segment
        Assert.Equal("FA", doc["GS", 1]);
    }

    [Fact]
    public void Document_allows_setting_element_value()
    {
        var doc = X12Document.Parse(TwoSegmentInput);

        doc["GS", 2] = "NEWSENDER";

        Assert.Equal("NEWSENDER", doc["GS", 2]);
    }

    [Fact]
    public void Document_generic_indexer_returns_all_segments_with_given_id()
    {
        // Interchange with two GS segments (two functional groups)
        const string multi =
            "ISA*00*          *00*          *ZZ*SENDER         *ZZ*RECEIVER       *201909*1200*^*00501*000000001*0*P*:~" +
            "GS*FA*SENDER*RECEIVER*20190901*1200*1*X*005010X231A1~" +
            "ST*999*0001~" +
            "AK1*FA*1*005010X231A1~" +
            "AK9*A*1*1*1~" +
            "SE*4*0001~" +
            "GE*1*1~" +
            "GS*HB*SENDER*RECEIVER*20190901*1200*2*X*005010X279A1~" +
            "SE*1*0002~" +
            "GE*1*2~" +
            "IEA*2*000000001~";

        var doc = X12Document.Parse(multi);

        // Generic indexer: returns ALL mutable segments with given segment ID
        var gsSegments = doc.AllSegments("GS");
        Assert.Equal(2, gsSegments.Count);
        Assert.Equal("FA", gsSegments[0][1]);
        Assert.Equal("HB", gsSegments[1][1]);
    }

    [Fact]
    public void Document_serializes_with_edit_applied()
    {
        var doc = X12Document.Parse(TwoSegmentInput);

        doc["GS", 2] = "NEWSENDER";
        string output = doc.ToString();

        Assert.Contains("GS*FA*NEWSENDER*", output);
        Assert.StartsWith("ISA*", output);
    }
}
