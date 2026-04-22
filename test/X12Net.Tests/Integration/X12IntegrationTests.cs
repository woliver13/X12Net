using woliver13.X12Net.DOM;
using woliver13.X12Net.IO;

namespace woliver13.X12Net.Tests.Integration;

/// <summary>
/// End-to-end integration tests covering parse → edit → serialize → re-parse workflows.
/// These tests exercise the full stack rather than individual components in isolation.
/// </summary>
public class X12IntegrationTests
{
    // ── Shared test data ──────────────────────────────────────────────────

    /// <summary>
    /// Minimal but structurally complete 837P claim interchange.
    /// ISA → GS → ST(837) → CLM → SE → GE → IEA
    /// </summary>
    private const string Claim837 =
        "ISA*00*          *00*          *ZZ*SENDER         *ZZ*RECEIVER       *230101*1200*^*00501*000000001*0*P*:~" +
        "GS*HC*SENDER*RECEIVER*20230101*1200*1*X*005010X222A1~" +
        "ST*837*0001*005010X222A1~" +
        "BPR*I*100.00*C*ACH*CCP*01*999999999*DA*123456789*1234567890**01*999999999*DA*987654321*20230101~" +
        "CLM*CLAIMID001*250.00***11:B:1*Y*A*Y*I~" +
        "SE*3*0001~" +
        "GE*1*1~" +
        "IEA*1*000000001~";

    /// <summary>
    /// Complete 999 functional acknowledgement interchange.
    /// </summary>
    private const string Ack999 =
        "ISA*00*          *00*          *ZZ*RECEIVER       *ZZ*SENDER         *230101*1201*^*00501*000000002*0*P*:~" +
        "GS*FA*RECEIVER*SENDER*20230101*1201*1*X*005010X231A1~" +
        "ST*999*0001~" +
        "AK1*HC*1*005010X222A1~" +
        "AK9*A*1*1*1~" +
        "SE*4*0001~" +
        "GE*1*1~" +
        "IEA*1*000000002~";

    /// <summary>
    /// Two-group interchange: one 837 group and one 999 group.
    /// </summary>
    private const string MultiGroup =
        "ISA*00*          *00*          *ZZ*SENDER         *ZZ*RECEIVER       *230101*1200*^*00501*000000003*0*P*:~" +
        "GS*HC*SENDER*RECEIVER*20230101*1200*1*X*005010X222A1~" +
        "ST*837*0001*005010X222A1~" +
        "CLM*CLAIMID001*250.00***11:B:1*Y*A*Y*I~" +
        "SE*2*0001~" +
        "GE*1*1~" +
        "GS*FA*SENDER*RECEIVER*20230101*1200*2*X*005010X231A1~" +
        "ST*999*0001~" +
        "AK1*HC*1*005010X222A1~" +
        "AK9*A*1*1*1~" +
        "SE*4*0002~" +
        "GE*1*2~" +
        "IEA*2*000000003~";

    // ── Cycle 1: parse → edit → re-parse round-trip ───────────────────────

    [Fact]
    public void Parse_Edit_Reparse_Roundtrip_ChangedElement_Persists()
    {
        // Parse
        var doc = X12Document.Parse(Claim837);

        // Edit CLM-02 (claim charge amount)
        doc["CLM", 2] = "999.99";

        // Serialize
        var edited = doc.ToString();

        // Re-parse and assert
        var doc2 = X12Document.Parse(edited);
        doc2["CLM", 2].ShouldBe("999.99");
    }

    [Fact]
    public void Parse_Edit_Reparse_Roundtrip_UnchangedElements_Preserved()
    {
        var doc = X12Document.Parse(Claim837);
        doc["CLM", 2] = "999.99";
        var doc2 = X12Document.Parse(doc.ToString());

        // ISA and GS structure should survive the round-trip untouched
        doc2["ISA", 13].ShouldBe("000000001"); // ICN
        doc2["GS", 1].ShouldBe("HC");          // function code
        doc2.Segments.Count.ShouldBe(8);
    }

    // ── Cycle 2: X12Interchange round-trip ───────────────────────────────

    [Fact]
    public void Interchange_Roundtrip_PreservesIsaControlNumber()
    {
        var interchange = X12Interchange.Parse(Ack999);
        var serialized  = interchange.ToString();
        var reparsed    = X12Interchange.Parse(serialized);

        reparsed.ISA[13].ShouldBe("000000002");
    }

    [Fact]
    public void Interchange_Roundtrip_PreservesAllSegments()
    {
        var interchange = X12Interchange.Parse(Ack999);
        var reparsed    = X12Interchange.Parse(interchange.ToString());

        reparsed.FunctionalGroups.Count.ShouldBe(1);
        reparsed.FunctionalGroups[0].Transactions.Count.ShouldBe(1);
        reparsed.FunctionalGroups[0].Transactions[0].Segments.Count.ShouldBe(2); // AK1 + AK9
    }

    // ── Cycle 3: multi-group interchange ─────────────────────────────────

    [Fact]
    public void MultiGroup_Parse_BothGroupsSurvive_Reserialize()
    {
        var interchange = X12Interchange.Parse(MultiGroup);
        var reparsed    = X12Interchange.Parse(interchange.ToString());

        reparsed.FunctionalGroups.Count.ShouldBe(2);
    }

    [Fact]
    public void MultiGroup_Parse_GroupControlNumbers_Correct()
    {
        var interchange = X12Interchange.Parse(MultiGroup);

        interchange.FunctionalGroups[0].GS[6].ShouldBe("1");
        interchange.FunctionalGroups[1].GS[6].ShouldBe("2");
    }

    // ── Cycle 4 & 5: streaming memory cap ────────────────────────────────

    [Fact]
    public void StreamingRead_WithinCap_ReadsAllSegments()
    {
        // Claim837 has 8 segments; cap of 10 should be fine
        using var reader = new X12Reader(Claim837, maxSegments: 10);
        var segments = reader.ReadAllSegments().ToList();
        segments.Count.ShouldBe(8);
    }

    [Fact]
    public void StreamingRead_ExceedingCap_Throws_X12MemoryCapException()
    {
        // Claim837 has 8 segments; cap of 5 should throw
        using var reader = new X12Reader(Claim837, maxSegments: 5);
        var ex = Should.Throw<X12MemoryCapException>(() => reader.ReadAllSegments().ToList());
        ex.MaxSegments.ShouldBe(5);
    }
}
