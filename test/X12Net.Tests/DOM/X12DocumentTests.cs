using woliver13.X12Net.DOM;

namespace woliver13.X12Net.Tests.DOM;

public class X12DocumentTests
{
    private const string TwoSegmentInput = Fixtures.Edi.IsaGs;

    [Fact]
    public void Document_provides_segment_access_by_index()
    {
        var doc = X12Document.Parse(TwoSegmentInput);

        doc.Segments[0].SegmentId.ShouldBe("ISA");
        doc.Segments[1].SegmentId.ShouldBe("GS");
    }

    [Fact]
    public void Document_provides_element_access_by_indexer()
    {
        var doc = X12Document.Parse(TwoSegmentInput);

        // doc["GS", 1] = first element of the first GS segment
        doc["GS", 1].ShouldBe("FA");
    }

    [Fact]
    public void Document_allows_setting_element_value()
    {
        var doc = X12Document.Parse(TwoSegmentInput);

        doc["GS", 2] = "NEWSENDER";

        doc["GS", 2].ShouldBe("NEWSENDER");
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
        gsSegments.Count.ShouldBe(2);
        gsSegments[0][1].ShouldBe("FA");
        gsSegments[1][1].ShouldBe("HB");
    }

    [Fact]
    public void Document_serializes_with_edit_applied()
    {
        var doc = X12Document.Parse(TwoSegmentInput);

        doc["GS", 2] = "NEWSENDER";
        string output = doc.ToString();

        output.ShouldContain("GS*FA*NEWSENDER*");
        output.ShouldStartWith("ISA*");
    }

    // ── Cycle 1 (Phase 8) ─────────────────────────────────────────────────

    [Fact]
    public void MutableSegment_get_beyond_bounds_returns_empty_string()
    {
        var doc = X12Document.Parse(TwoSegmentInput);
        var gs = doc.Segments.First(s => s.SegmentId == "GS");

        // GS has 8 elements; index 20 is beyond current length
        var value = gs[20];

        value.ShouldBe(string.Empty);
    }

    // ── Cycle 2 (Phase 7) ─────────────────────────────────────────────────

    [Fact]
    public void MutableSegment_set_beyond_bounds_extends_with_empty_elements()
    {
        var doc = X12Document.Parse(TwoSegmentInput);
        var gs = doc.Segments.First(s => s.SegmentId == "GS");

        // GS has 8 elements; set element 10 (beyond current length)
        gs[10] = "EXTRA";

        gs[10].ShouldBe("EXTRA");
        // Elements 9 is auto-filled with empty string
        gs[9].ShouldBe(string.Empty);
    }

    // ── Cycle 4 (Phase 7) ─────────────────────────────────────────────────

    private const string MultiEbInterchange =
        "ISA*00*          *00*          *ZZ*SENDER         *ZZ*RECEIVER       *201909*1200*^*00501*000000001*0*P*:~" +
        "GS*HB*SENDER*RECEIVER*20190901*1200*1*X*005010X279A1~" +
        "ST*271*0001~" +
        "EB*1*FAM~" +
        "EB*C*IND~" +
        "SE*3*0001~" +
        "GE*1*1~" +
        "IEA*1*000000001~";

    [Fact]
    public void Document_AllSegments_multi_edit_serializes_all_changes()
    {
        var doc = X12Document.Parse(MultiEbInterchange);
        var ebs = doc.AllSegments("EB");

        ebs[0][1] = "W";
        ebs[1][1] = "X";

        var reparsed = X12Document.Parse(doc.ToString());
        var reparsedEbs = reparsed.AllSegments("EB");

        reparsedEbs[0][1].ShouldBe("W");
        reparsedEbs[1][1].ShouldBe("X");
    }

    // ── Cycle 3 (Phase 6) ─────────────────────────────────────────────────

    private const string FullInterchange = Fixtures.Edi.Valid999;

    [Fact]
    public void Document_edit_roundtrips_correctly()
    {
        var doc = X12Document.Parse(FullInterchange);
        doc["GS", 2] = "ACME";

        var reparsed = X12Document.Parse(doc.ToString());

        reparsed["GS", 2].ShouldBe("ACME");
        // Other elements unaffected
        reparsed["GS", 1].ShouldBe("FA");
        reparsed["GS", 3].ShouldBe("RECEIVER");
    }

    // ── X12MessageParser characterisation tests ───────────────────────────

    // ISA header using | as element sep, ^ as component sep, \n as segment terminator
    private const string NonstandardDelimiterInterchange =
        "ISA|00|          |00|          |ZZ|SENDER         |ZZ|RECEIVER       |201909|1200|^|00501|000000001|0|P|:\n" +
        "GS|FA|SENDER|RECEIVER|20190901|1200|1|X|005010X231A1\n" +
        "ST|999|0001\n" +
        "SE|1|0001\n" +
        "GE|1|1\n" +
        "IEA|1|000000001\n";

    [Fact]
    public void Both_Parse_methods_detect_identical_delimiters_for_nonstandard_interchange()
    {
        var doc         = X12Document.Parse(NonstandardDelimiterInterchange);
        var interchange = X12Interchange.Parse(NonstandardDelimiterInterchange);

        doc.Delimiters.ElementSeparator.ShouldBe(interchange.Delimiters.ElementSeparator);
        doc.Delimiters.ComponentSeparator.ShouldBe(interchange.Delimiters.ComponentSeparator);
        doc.Delimiters.SegmentTerminator.ShouldBe(interchange.Delimiters.SegmentTerminator);

        doc.Delimiters.ElementSeparator.ShouldBe('|');
        doc.Delimiters.ComponentSeparator.ShouldBe(':');
        doc.Delimiters.SegmentTerminator.ShouldBe('\n');
    }

    [Fact]
    public void Both_Parse_methods_yield_same_segment_count_from_same_input()
    {
        var doc         = X12Document.Parse(Fixtures.Edi.Valid999);
        var interchange = X12Interchange.Parse(Fixtures.Edi.Valid999);

        // X12Document counts all segments; X12Interchange exposes them via ISA+groups+IEA
        int interchangeSegmentCount =
            1 // ISA
            + interchange.FunctionalGroups.Sum(g =>
                1 // GS
                + g.Transactions.Sum(t => 1 + t.Segments.Count + 1) // ST + body + SE
                + 1) // GE
            + 1; // IEA

        doc.Segments.Count.ShouldBe(interchangeSegmentCount);
    }
}
