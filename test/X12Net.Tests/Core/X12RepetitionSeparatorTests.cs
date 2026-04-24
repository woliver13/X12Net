using woliver13.X12Net.Core;

namespace woliver13.X12Net.Tests.Core;

public class X12RepetitionSeparatorTests
{
    // Standard 005010 interchange with ^ as ISA11 (repetition separator)
    private const string StandardInterchange =
        "ISA*00*          *00*          *ZZ*SENDER         *ZZ*RECEIVER       *201909*1200*^*00501*000000001*0*P*:~" +
        "GS*FA*SENDER*RECEIVER*20190901*1200*1*X*005010X231A1~" +
        "IEA*1*000000001~";

    // Non-standard interchange using | as element sep and + as repetition separator (ISA11)
    private const string NonstandardRepSepInterchange =
        "ISA|00|          |00|          |ZZ|SENDER         |ZZ|RECEIVER       |201909|1200|+|00501|000000001|0|P|:\n" +
        "GS|FA|SENDER|RECEIVER|20190901|1200|1|X|005010X231A1\n" +
        "IEA|1|000000001\n";

    [Fact]
    public void IsaParser_extracts_repetition_separator_from_standard_interchange()
    {
        var d = X12Delimiters.FromIsa(StandardInterchange);

        d.RepetitionSeparator.ShouldBe('^');
    }

    [Fact]
    public void IsaParser_extracts_nonstandard_repetition_separator()
    {
        var d = X12Delimiters.FromIsa(NonstandardRepSepInterchange);

        d.RepetitionSeparator.ShouldBe('+');
    }

    [Fact]
    public void Delimiters_default_has_caret_as_repetition_separator()
    {
        X12Delimiters.Default.RepetitionSeparator.ShouldBe('^');
    }

    [Fact]
    public void Segment_GetRepetitions_splits_element_on_repetition_separator()
    {
        // GS03 contains two repetitions: "SENDER" and "ALIAS" joined by ^
        const string edi = "GS*FA*SENDER*SENDER^ALIAS*20190901*1200*1*X*005010~";
        var segment = X12SegmentParser.ParseAll(edi, X12Delimiters.Default).First();

        var reps = segment.GetRepetitions(3, '^');

        reps.Count.ShouldBe(2);
        reps[0].ShouldBe("SENDER");
        reps[1].ShouldBe("ALIAS");
    }

    [Fact]
    public void Segment_GetRepetitions_returns_single_entry_when_no_repetition_present()
    {
        const string edi = "GS*FA*SENDER*RECEIVER*20190901*1200*1*X*005010~";
        var segment = X12SegmentParser.ParseAll(edi, X12Delimiters.Default).First();

        var reps = segment.GetRepetitions(3, '^');

        reps.Count.ShouldBe(1);
        reps[0].ShouldBe("RECEIVER");
    }

    [Fact]
    public void Delimiters_FromIsa_preserves_repetition_separator_through_full_parse()
    {
        // Interchange where ISA11 is + (non-default)
        var d = X12Delimiters.FromIsa(NonstandardRepSepInterchange);
        var segments = X12SegmentParser.ParseAll(NonstandardRepSepInterchange, d).ToList();
        var gs = segments.First(s => s.SegmentId == "GS");

        // GS02 has two repetitions joined by +
        const string ediWithRep =
            "ISA|00|          |00|          |ZZ|SENDER         |ZZ|RECEIVER       |201909|1200|+|00501|000000001|0|P|:\n" +
            "GS|FA|SENDER+ALIAS|RECEIVER|20190901|1200|1|X|005010X231A1\n" +
            "IEA|1|000000001\n";

        var d2 = X12Delimiters.FromIsa(ediWithRep);
        var segs = X12SegmentParser.ParseAll(ediWithRep, d2).ToList();
        var gs2 = segs.First(s => s.SegmentId == "GS");

        var reps = gs2.GetRepetitions(2, d2.RepetitionSeparator);
        reps.Count.ShouldBe(2);
        reps[0].ShouldBe("SENDER");
        reps[1].ShouldBe("ALIAS");
    }
}
