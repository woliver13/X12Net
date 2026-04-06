using X12Net.DOM;

namespace X12Net.Tests.DOM;

public class X12InterchangeTests
{
    // Full interchange: ISA + GS + ST + data + SE + GE + IEA
    private const string FullInterchange =
        "ISA*00*          *00*          *ZZ*SENDER         *ZZ*RECEIVER       *201909*1200*^*00501*000000001*0*P*:~" +
        "GS*FA*SENDER*RECEIVER*20190901*1200*1*X*005010X231A1~" +
        "ST*999*0001~" +
        "AK1*FA*1*005010X231A1~" +
        "AK9*A*1*1*1~" +
        "SE*4*0001~" +
        "GE*1*1~" +
        "IEA*1*000000001~";

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

        Assert.Equal("ISA", interchange.ISA.SegmentId);
        Assert.Equal("IEA", interchange.IEA.SegmentId);
    }

    // ── Cycle 2 ───────────────────────────────────────────────────────────

    [Fact]
    public void Interchange_contains_one_FunctionalGroup_per_GS_GE_pair()
    {
        var interchange = X12Interchange.Parse(FullInterchange);

        Assert.Single(interchange.FunctionalGroups);
        Assert.Equal("GS", interchange.FunctionalGroups[0].GS.SegmentId);
        Assert.Equal("GE", interchange.FunctionalGroups[0].GE.SegmentId);
    }

    // ── Cycle 3 ───────────────────────────────────────────────────────────

    [Fact]
    public void FunctionalGroup_contains_one_Transaction_per_ST_SE_pair()
    {
        var interchange = X12Interchange.Parse(FullInterchange);
        var group = interchange.FunctionalGroups[0];

        Assert.Single(group.Transactions);
        Assert.Equal("ST", group.Transactions[0].ST.SegmentId);
        Assert.Equal("SE", group.Transactions[0].SE.SegmentId);
    }

    // ── Cycle 4 ───────────────────────────────────────────────────────────

    [Fact]
    public void Transaction_exposes_all_inner_segments()
    {
        var interchange = X12Interchange.Parse(FullInterchange);
        var tx = interchange.FunctionalGroups[0].Transactions[0];

        // Inner segments: AK1 + AK9 (ST and SE are envelope, not counted as body)
        var ids = tx.Segments.Select(s => s.SegmentId).ToList();
        Assert.Contains("AK1", ids);
        Assert.Contains("AK9", ids);
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

        Assert.Empty(interchange.FunctionalGroups);
        Assert.Equal("ISA", interchange.ISA.SegmentId);
        Assert.Equal("IEA", interchange.IEA.SegmentId);
    }

    // ── Cycle 3 (Phase 4) ─────────────────────────────────────────────────

    [Fact]
    public void Interchange_handles_multiple_transactions_in_one_functional_group()
    {
        var interchange = X12Interchange.Parse(TwoTxOneGroupInterchange);

        Assert.Single(interchange.FunctionalGroups);
        var group = interchange.FunctionalGroups[0];
        Assert.Equal(2, group.Transactions.Count);
        Assert.Equal("0001", group.Transactions[0].ST[2]); // ST02 control number
        Assert.Equal("0002", group.Transactions[1].ST[2]);
    }

    // ── Cycle 5 ───────────────────────────────────────────────────────────

    [Fact]
    public void Interchange_parse_handles_multiple_functional_groups()
    {
        var interchange = X12Interchange.Parse(TwoGroupInterchange);

        Assert.Equal(2, interchange.FunctionalGroups.Count);
        Assert.Equal("FA",  interchange.FunctionalGroups[0].GS[1]);  // GS01
        Assert.Equal("HB",  interchange.FunctionalGroups[1].GS[1]);  // GS01
        Assert.Equal("999", interchange.FunctionalGroups[0].Transactions[0].ST[1]); // ST01
        Assert.Equal("271", interchange.FunctionalGroups[1].Transactions[0].ST[1]); // ST01
    }
}
