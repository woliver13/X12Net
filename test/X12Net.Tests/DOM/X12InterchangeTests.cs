using woliver13.X12Net.DOM;

namespace woliver13.X12Net.Tests.DOM;

public class X12InterchangeTests
{
    // Full interchange: ISA + GS + ST + data + SE + GE + IEA
    private const string FullInterchange = Fixtures.Edi.Valid999;

    // Two functional groups inside one interchange
    private const string TwoGroupInterchange =
        "ISA*00*          *00*          *ZZ*SENDER         *ZZ*RECEIVER       *201909*1200*^*00501*000000001*0*P*:~" +
        "GS*FA*SENDER*RECEIVER*20190901*1200*1*X*005010X231A1~" +
        "ST*999*0001~" +
        "AK1*FA*1*005010X231A1~" +
        "AK9*A*1*1*1~" +
        "SE*4*0001~" +
        "GE*1*1~" +
        "GS*HB*SENDER*RECEIVER*20190901*1200*2*X*005010X279A1~" +
        "ST*271*0001~" +
        "BHT*0022*11*10001234*20190901*1200~" +
        "SE*2*0001~" +
        "GE*1*2~" +
        "IEA*2*000000001~";

    // ── Cycle 1 ───────────────────────────────────────────────────────────

    [Fact]
    public void Interchange_has_ISA_IEA_segments()
    {
        var interchange = X12Interchange.Parse(FullInterchange);

        interchange.ISA.SegmentId.ShouldBe("ISA");
        interchange.IEA.SegmentId.ShouldBe("IEA");
    }

    // ── Cycle 2 ───────────────────────────────────────────────────────────

    [Fact]
    public void Interchange_contains_one_FunctionalGroup_per_GS_GE_pair()
    {
        var interchange = X12Interchange.Parse(FullInterchange);

        interchange.FunctionalGroups.ShouldHaveSingleItem();
        interchange.FunctionalGroups[0].GS.SegmentId.ShouldBe("GS");
        interchange.FunctionalGroups[0].GE.SegmentId.ShouldBe("GE");
    }

    // ── Cycle 3 ───────────────────────────────────────────────────────────

    [Fact]
    public void FunctionalGroup_contains_one_Transaction_per_ST_SE_pair()
    {
        var interchange = X12Interchange.Parse(FullInterchange);
        var group = interchange.FunctionalGroups[0];

        group.Transactions.ShouldHaveSingleItem();
        group.Transactions[0].ST.SegmentId.ShouldBe("ST");
        group.Transactions[0].SE.SegmentId.ShouldBe("SE");
    }

    // ── Cycle 4 ───────────────────────────────────────────────────────────

    [Fact]
    public void Transaction_exposes_all_inner_segments()
    {
        var interchange = X12Interchange.Parse(FullInterchange);
        var tx = interchange.FunctionalGroups[0].Transactions[0];

        // Inner segments: AK1 + AK9 (ST and SE are envelope, not counted as body)
        var ids = tx.Segments.Select(s => s.SegmentId).ToList();
        ids.ShouldContain("AK1");
        ids.ShouldContain("AK9");
    }

    // Two transactions inside a single functional group
    private const string TwoTxOneGroupInterchange =
        "ISA*00*          *00*          *ZZ*SENDER         *ZZ*RECEIVER       *201909*1200*^*00501*000000001*0*P*:~" +
        "GS*FA*SENDER*RECEIVER*20190901*1200*1*X*005010X231A1~" +
        "ST*999*0001~" +
        "AK1*FA*1*005010X231A1~" +
        "AK9*A*1*1*1~" +
        "SE*4*0001~" +
        "ST*999*0002~" +
        "AK1*FA*2*005010X231A1~" +
        "AK9*A*1*1*1~" +
        "SE*4*0002~" +
        "GE*2*1~" +
        "IEA*1*000000001~";

    // ISA/IEA with zero functional groups
    private const string EmptyInterchange =
        "ISA*00*          *00*          *ZZ*SENDER         *ZZ*RECEIVER       *201909*1200*^*00501*000000001*0*P*:~" +
        "IEA*0*000000001~";

    // ── Cycle 6 (Phase 4) ─────────────────────────────────────────────────

    [Fact]
    public void Interchange_parses_when_no_functional_groups_present()
    {
        var interchange = X12Interchange.Parse(EmptyInterchange);

        interchange.FunctionalGroups.ShouldBeEmpty();
        interchange.ISA.SegmentId.ShouldBe("ISA");
        interchange.IEA.SegmentId.ShouldBe("IEA");
    }

    // ── Cycle 3 (Phase 4) ─────────────────────────────────────────────────

    [Fact]
    public void Interchange_handles_multiple_transactions_in_one_functional_group()
    {
        var interchange = X12Interchange.Parse(TwoTxOneGroupInterchange);

        interchange.FunctionalGroups.ShouldHaveSingleItem();
        var group = interchange.FunctionalGroups[0];
        group.Transactions.Count.ShouldBe(2);
        group.Transactions[0].ST[2].ShouldBe("0001"); // ST02 control number
        group.Transactions[1].ST[2].ShouldBe("0002");
    }

    // ── Phase 2, Cycle 1 ─────────────────────────────────────────────────

    [Fact]
    public void Interchange_ToString_round_trips_a_simple_interchange()
    {
        var interchange = X12Interchange.Parse(FullInterchange);

        var edI = interchange.ToString();
        var reparsed = X12Interchange.Parse(edI);

        reparsed.ISA.SegmentId.ShouldBe("ISA");
        reparsed.FunctionalGroups.ShouldHaveSingleItem();
        reparsed.FunctionalGroups[0].Transactions.ShouldHaveSingleItem();
        reparsed.IEA.SegmentId.ShouldBe("IEA");
    }

    // ── Phase 2, Cycle 2 ─────────────────────────────────────────────────

    [Fact]
    public void Interchange_ToString_preserves_body_segments()
    {
        var interchange = X12Interchange.Parse(FullInterchange);

        var reparsed = X12Interchange.Parse(interchange.ToString());
        var tx = reparsed.FunctionalGroups[0].Transactions[0];

        tx.Segments[0].SegmentId.ShouldBe("AK1");
        tx.Segments[0][1].ShouldBe("FA");   // AK101
        tx.Segments[1].SegmentId.ShouldBe("AK9");
        tx.Segments[1][1].ShouldBe("A");    // AK901
    }

    // ── Phase 2, Cycle 6 ─────────────────────────────────────────────────

    private const string Multi271Interchange =
        "ISA*00*          *00*          *ZZ*RECEIVER       *ZZ*SUBMITTER      *201909*1200*^*00501*000000001*0*P*:~" +
        "GS*HB*RECEIVER*SUBMITTER*20190901*1200*1*X*005010X279A1~" +
        "ST*271*0001~" +
        "BHT*0022*11*10001234*20190901*1200~" +
        "EB*1*FAM*30*MC~" +
        "SE*4*0001~" +
        "ST*271*0002~" +
        "BHT*0022*11*10001235*20190901*1200~" +
        "EB*C*IND*30*MC~" +
        "SE*4*0002~" +
        "GE*2*1~" +
        "IEA*1*000000001~";

    [Fact]
    public void Interchange_GetTransactions_returns_typed_wrappers()
    {
        var interchange = X12Interchange.Parse(Multi271Interchange);

        var txns = interchange.GetTransactions(X12Net.Transactions.Ts271.Parse).ToList();

        txns.Count.ShouldBe(2);
        txns[0].EB!.EligibilityOrBenefitInformation.ShouldBe("1");
        txns[1].EB!.EligibilityOrBenefitInformation.ShouldBe("C");
    }

    // ── GetTransactions direct-factory overload (TD-15) ──────────────────

    [Fact]
    public void GetTransactions_with_direct_factory_returns_correct_results()
    {
        var interchange = X12Interchange.Parse(Multi271Interchange);

        // Factory receives X12Transaction directly — no ToEdi round-trip
        var stIds = interchange.GetTransactions(
            (tx, delimiters) => tx.ST[1]).ToList();

        stIds.Count.ShouldBe(2);
        stIds[0].ShouldBe("271");
        stIds[1].ShouldBe("271");
    }

    [Fact]
    public void GetTransactions_direct_factory_receives_correct_delimiters()
    {
        var interchange = X12Interchange.Parse(Multi271Interchange);

        var separators = interchange.GetTransactions(
            (tx, d) => d.ElementSeparator).ToList();

        separators.Count.ShouldBe(2);
        separators.ShouldAllBe(sep => sep == '*');
    }

    [Fact]
    public void GetTransactions_direct_factory_exposes_body_segments()
    {
        var interchange = X12Interchange.Parse(Multi271Interchange);

        var firstBodyIds = interchange.GetTransactions(
            (tx, d) => tx.Segments.Select(s => s.SegmentId).ToList()).ToList();

        firstBodyIds.Count.ShouldBe(2);
        firstBodyIds[0].ShouldContain("BHT");
        firstBodyIds[0].ShouldContain("EB");
    }

    // ── Cycle 5 ───────────────────────────────────────────────────────────

    [Fact]
    public void Interchange_parse_handles_multiple_functional_groups()
    {
        var interchange = X12Interchange.Parse(TwoGroupInterchange);

        interchange.FunctionalGroups.Count.ShouldBe(2);
        interchange.FunctionalGroups[0].GS[1].ShouldBe("FA");  // GS01
        interchange.FunctionalGroups[1].GS[1].ShouldBe("HB");  // GS01
        interchange.FunctionalGroups[0].Transactions[0].ST[1].ShouldBe("999"); // ST01
        interchange.FunctionalGroups[1].Transactions[0].ST[1].ShouldBe("271"); // ST01
    }
}
