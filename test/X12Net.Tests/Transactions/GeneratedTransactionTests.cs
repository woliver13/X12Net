using woliver13.X12Net.Transactions;

namespace woliver13.X12Net.Tests.Transactions;

// Phase 2 — typed segment collections

/// <summary>
/// Verifies that the source generator emits a typed class for each built-in
/// transaction set with the correct key-segment property.
/// </summary>
public class GeneratedTransactionTests
{
    // ── 835 Payment/Remittance ────────────────────────────────────────────

    private const string Input835 =
        "ISA*00*          *00*          *ZZ*PAYER          *ZZ*PROVIDER       *201909*1200*^*00501*000000001*0*P*:~" +
        "GS*HP*PAYER*PROVIDER*20190901*1200*1*X*005010X221A1~" +
        "ST*835*0001~" +
        "BPR*I*100*C*ACH*CTX*01*999999999*DA*123456789*1234567890*01*999988880*DA*987654321*20190901~" +
        "CLP*PATIENT-1*1*500*400*100*12*ICN12345*01~" +
        "SE*4*0001~" +
        "GE*1*1~" +
        "IEA*1*000000001~";

    [Fact]
    public void Generated_835_has_typed_CLP_segment()
    {
        var ts = Ts835.Parse(Input835);

        Assert.NotNull(ts.CLP);
        Assert.Equal("PATIENT-1", ts.CLP!.PatientControlNumber);  // CLP01
        Assert.Equal("1",         ts.CLP.ClaimStatusCode);         // CLP02
    }

    // ── 837P Professional ─────────────────────────────────────────────────

    private const string Input837P =
        "ISA*00*          *00*          *ZZ*SUBMITTER      *ZZ*RECEIVER       *201909*1200*^*00501*000000001*0*P*:~" +
        "GS*HC*SUBMITTER*RECEIVER*20190901*1200*1*X*005010X222A2~" +
        "ST*837*0001~" +
        "BHT*0019*00*123*20190901*1200*CH~" +
        "CLM*CLAIM001*500***11:B:1*Y*A*Y*I~" +
        "SE*4*0001~" +
        "GE*1*1~" +
        "IEA*1*000000001~";

    [Fact]
    public void Generated_837P_has_typed_CLM_segment()
    {
        var ts = Ts837P.Parse(Input837P);

        Assert.NotNull(ts.CLM);
        Assert.Equal("CLAIM001", ts.CLM!.PatientControlNumber);  // CLM01
        Assert.Equal("500",      ts.CLM.TotalClaimChargeAmount); // CLM02
    }

    // ── 837I Institutional ────────────────────────────────────────────────

    [Fact]
    public void Generated_837I_has_typed_CLM_segment()
    {
        // 837I uses the same CLM segment structure as 837P
        var ts = Ts837I.Parse(Input837P);  // reuse same test data — CLM is identical

        Assert.NotNull(ts.CLM);
        Assert.Equal("CLAIM001", ts.CLM!.PatientControlNumber);
    }

    // ── 837D Dental ───────────────────────────────────────────────────────

    [Fact]
    public void Generated_837D_has_typed_CLM_segment()
    {
        var ts = Ts837D.Parse(Input837P);

        Assert.NotNull(ts.CLM);
        Assert.Equal("CLAIM001", ts.CLM!.PatientControlNumber);
    }

    // ── 270 Eligibility Inquiry ───────────────────────────────────────────

    private const string Input270 =
        "ISA*00*          *00*          *ZZ*SUBMITTER      *ZZ*RECEIVER       *201909*1200*^*00501*000000001*0*P*:~" +
        "GS*HS*SUBMITTER*RECEIVER*20190901*1200*1*X*005010X279A1~" +
        "ST*270*0001~" +
        "BHT*0022*13*10001234*20190901*1200~" +
        "EQ*30~" +
        "SE*4*0001~" +
        "GE*1*1~" +
        "IEA*1*000000001~";

    [Fact]
    public void Generated_270_has_typed_EQ_segment()
    {
        var ts = Ts270.Parse(Input270);

        Assert.NotNull(ts.EQ);
        Assert.Equal("30", ts.EQ!.ServiceTypeCode);  // EQ01
    }

    // ── Phase 2, Cycle 5 — typed segment collections ──────────────────────

    [Fact]
    public void Ts271_AllEB_returns_all_EB_segments_in_order()
    {
        const string multi271 =
            "ISA*00*          *00*          *ZZ*RECEIVER       *ZZ*SUBMITTER      *201909*1200*^*00501*000000001*0*P*:~" +
            "GS*HB*RECEIVER*SUBMITTER*20190901*1200*1*X*005010X279A1~" +
            "ST*271*0001~" +
            "BHT*0022*11*10001234*20190901*1200~" +
            "EB*1*FAM*30*MC~" +
            "EB*C*IND*30*MC~" +
            "EB*W*FAM*30*MC~" +
            "SE*6*0001~" +
            "GE*1*1~" +
            "IEA*1*000000001~";

        var ts = Ts271.Parse(multi271);
        var all = ts.AllEB().ToList();

        Assert.Equal(3, all.Count);
        Assert.Equal("1", all[0].EligibilityOrBenefitInformation);
        Assert.Equal("C", all[1].EligibilityOrBenefitInformation);
        Assert.Equal("W", all[2].EligibilityOrBenefitInformation);
    }

    // ── 271 Eligibility Response ──────────────────────────────────────────

    private const string Input271 =
        "ISA*00*          *00*          *ZZ*RECEIVER       *ZZ*SUBMITTER      *201909*1200*^*00501*000000001*0*P*:~" +
        "GS*HB*RECEIVER*SUBMITTER*20190901*1200*1*X*005010X279A1~" +
        "ST*271*0001~" +
        "BHT*0022*11*10001234*20190901*1200~" +
        "EB*1*FAM*30*MC~" +
        "SE*4*0001~" +
        "GE*1*1~" +
        "IEA*1*000000001~";

    [Fact]
    public void Generated_271_has_typed_EB_segment()
    {
        var ts = Ts271.Parse(Input271);

        Assert.NotNull(ts.EB);
        Assert.Equal("1", ts.EB!.EligibilityOrBenefitInformation);  // EB01
    }

    // ── 834 Benefit Enrollment ────────────────────────────────────────────

    private const string Input834 =
        "ISA*00*          *00*          *ZZ*SENDER         *ZZ*RECEIVER       *201909*1200*^*00501*000000001*0*P*:~" +
        "GS*BE*SENDER*RECEIVER*20190901*1200*1*X*005010X220A1~" +
        "ST*834*0001~" +
        "BGN*00*REF001*20190901*1200****2~" +
        "INS*Y*18*021*28*A*E**FT~" +
        "SE*4*0001~" +
        "GE*1*1~" +
        "IEA*1*000000001~";

    [Fact]
    public void Generated_834_has_typed_INS_segment()
    {
        var ts = Ts834.Parse(Input834);

        Assert.NotNull(ts.INS);
        Assert.Equal("Y",   ts.INS!.MemberIndicator);       // INS01
        Assert.Equal("18",  ts.INS.IndividualRelationshipCode); // INS02
    }

    // ── 276 Claim Status Request ──────────────────────────────────────────

    private const string Input276 =
        "ISA*00*          *00*          *ZZ*SUBMITTER      *ZZ*RECEIVER       *201909*1200*^*00501*000000001*0*P*:~" +
        "GS*HR*SUBMITTER*RECEIVER*20190901*1200*1*X*005010X212~" +
        "ST*276*0001~" +
        "BHT*0010*13*10001234*20190901*1200~" +
        "STC*A0:20*20190901~" +
        "SE*4*0001~" +
        "GE*1*1~" +
        "IEA*1*000000001~";

    [Fact]
    public void Generated_276_has_typed_STC_segment()
    {
        var ts = Ts276.Parse(Input276);

        Assert.NotNull(ts.STC);
        Assert.Equal("20190901", ts.STC!.StatusEffectiveDate);  // STC02
    }

    // ── AllXxx() multi-segment coverage (guards the no-re-parse refactor) ────

    [Fact]
    public void Ts835_AllCLP_returns_multiple_CLP_segments_in_order()
    {
        const string input =
            "ISA*00*          *00*          *ZZ*PAYER          *ZZ*PROVIDER       *201909*1200*^*00501*000000001*0*P*:~" +
            "GS*HP*PAYER*PROVIDER*20190901*1200*1*X*005010X221A1~" +
            "ST*835*0001~" +
            "BPR*I*100*C*ACH*CTX*01*999999999*DA*123456789*1234567890*01*999988880*DA*987654321*20190901~" +
            "CLP*PATIENT-A*1*500*400*100*12*ICN001*01~" +
            "CLP*PATIENT-B*2*300*200*50*12*ICN002*01~" +
            "CLP*PATIENT-C*3*100*80*20*12*ICN003*01~" +
            "SE*6*0001~" +
            "GE*1*1~" +
            "IEA*1*000000001~";

        var ts  = Ts835.Parse(input);
        var all = ts.AllCLP().ToList();

        Assert.Equal(3, all.Count);
        Assert.Equal("PATIENT-A", all[0].PatientControlNumber);
        Assert.Equal("PATIENT-B", all[1].PatientControlNumber);
        Assert.Equal("PATIENT-C", all[2].PatientControlNumber);
        Assert.Equal("1", all[0].ClaimStatusCode);
        Assert.Equal("2", all[1].ClaimStatusCode);
        Assert.Equal("3", all[2].ClaimStatusCode);
    }

    [Fact]
    public void Ts837P_AllCLM_returns_multiple_CLM_segments_in_order()
    {
        const string input =
            "ISA*00*          *00*          *ZZ*SUBMITTER      *ZZ*RECEIVER       *201909*1200*^*00501*000000001*0*P*:~" +
            "GS*HC*SUBMITTER*RECEIVER*20190901*1200*1*X*005010X222A2~" +
            "ST*837*0001~" +
            "BHT*0019*00*123*20190901*1200*CH~" +
            "CLM*CLAIM-A*500***11:B:1*Y*A*Y*I~" +
            "CLM*CLAIM-B*750***11:B:1*Y*A*Y*I~" +
            "SE*5*0001~" +
            "GE*1*1~" +
            "IEA*1*000000001~";

        var ts  = Ts837P.Parse(input);
        var all = ts.AllCLM().ToList();

        Assert.Equal(2, all.Count);
        Assert.Equal("CLAIM-A", all[0].PatientControlNumber);
        Assert.Equal("CLAIM-B", all[1].PatientControlNumber);
        Assert.Equal("500", all[0].TotalClaimChargeAmount);
        Assert.Equal("750", all[1].TotalClaimChargeAmount);
    }

    [Fact]
    public void Ts834_AllINS_returns_multiple_INS_segments_in_order()
    {
        const string input =
            "ISA*00*          *00*          *ZZ*SENDER         *ZZ*RECEIVER       *201909*1200*^*00501*000000001*0*P*:~" +
            "GS*BE*SENDER*RECEIVER*20190901*1200*1*X*005010X220A1~" +
            "ST*834*0001~" +
            "BGN*00*REF001*20190901*1200****2~" +
            "INS*Y*18*021*28*A*E**FT~" +
            "INS*N*01*021*28*A*E**FT~" +
            "SE*5*0001~" +
            "GE*1*1~" +
            "IEA*1*000000001~";

        var ts  = Ts834.Parse(input);
        var all = ts.AllINS().ToList();

        Assert.Equal(2, all.Count);
        Assert.Equal("Y",  all[0].MemberIndicator);
        Assert.Equal("N",  all[1].MemberIndicator);
        Assert.Equal("18", all[0].IndividualRelationshipCode);
        Assert.Equal("01", all[1].IndividualRelationshipCode);
    }

    [Fact]
    public void Ts276_AllSTC_returns_multiple_STC_segments_in_order()
    {
        const string input =
            "ISA*00*          *00*          *ZZ*SUBMITTER      *ZZ*RECEIVER       *201909*1200*^*00501*000000001*0*P*:~" +
            "GS*HR*SUBMITTER*RECEIVER*20190901*1200*1*X*005010X212~" +
            "ST*276*0001~" +
            "BHT*0010*13*10001234*20190901*1200~" +
            "STC*A0:20*20190901*WQ*500~" +
            "STC*E0:30*20190902*U*200~" +
            "SE*5*0001~" +
            "GE*1*1~" +
            "IEA*1*000000001~";

        var ts  = Ts276.Parse(input);
        var all = ts.AllSTC().ToList();

        Assert.Equal(2, all.Count);
        Assert.Equal("20190901", all[0].StatusEffectiveDate);
        Assert.Equal("20190902", all[1].StatusEffectiveDate);
        Assert.Equal("WQ", all[0].ActionCode);
        Assert.Equal("U",  all[1].ActionCode);
        Assert.Equal("500", all[0].MonetaryAmount);
        Assert.Equal("200", all[1].MonetaryAmount);
    }

    // ── 277 Claim Status Response ─────────────────────────────────────────

    private const string Input277 =
        "ISA*00*          *00*          *ZZ*RECEIVER       *ZZ*SUBMITTER      *201909*1200*^*00501*000000001*0*P*:~" +
        "GS*HN*RECEIVER*SUBMITTER*20190901*1200*1*X*005010X212~" +
        "ST*277*0001~" +
        "BHT*0010*08*10001234*20190901*1200~" +
        "STC*A0:20*20190901*WQ*500~" +
        "SE*4*0001~" +
        "GE*1*1~" +
        "IEA*1*000000001~";

    [Fact]
    public void Generated_277_has_typed_STC_segment()
    {
        var ts = Ts277.Parse(Input277);

        Assert.NotNull(ts.STC);
        Assert.Equal("20190901", ts.STC!.StatusEffectiveDate);  // STC02
        Assert.Equal("WQ",       ts.STC.ActionCode);            // STC03
        Assert.Equal("500",      ts.STC.MonetaryAmount);        // STC04
    }
}
