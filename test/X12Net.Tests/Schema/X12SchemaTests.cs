using woliver13.X12Net.Schema;
using woliver13.X12Net.Validation;

namespace woliver13.X12Net.Tests.Schema;

public class X12SchemaTests
{
    private static readonly X12TransactionSchema Ts271Schema = new("271", "Eligibility Response",
        new X12SegmentSchema("BHT", new[] { "HierarchicalStructureCode" }, isRequired: true),
        new X12SegmentSchema("EB",  new[] { "EligibilityCode" },           isRequired: false));

    // ── Cycle 9 ───────────────────────────────────────────────────────────

    [Fact]
    public void SchemaRegistry_can_register_and_retrieve_custom_schema()
    {
        var registry = new X12SchemaRegistry();
        var schema   = new X12TransactionSchema("ZZZ", "Custom Test Transaction",
            new X12SegmentSchema("ZZZ", new[] { "Field1", "Field2" }));

        registry.Register(schema);

        var retrieved = registry.Get("ZZZ");
        Assert.NotNull(retrieved);
        Assert.Equal("ZZZ", retrieved!.TransactionSetId);
    }

    // ── Cycle 10 ──────────────────────────────────────────────────────────

    [Fact]
    public void DynamicTransaction_accesses_element_by_schema_defined_name()
    {
        var schema = new X12TransactionSchema("ZZZ", "Custom Test",
            new X12SegmentSchema("ZZZ", new[] { "Field1", "Field2" }));

        const string input = "ZZZ*HELLO*WORLD~";
        var tx = X12DynamicTransaction.Parse(input, schema);

        Assert.Equal("HELLO", tx["ZZZ", "Field1"]);
        Assert.Equal("WORLD", tx["ZZZ", "Field2"]);
    }

    // ── Cycle 11 ──────────────────────────────────────────────────────────

    [Fact]
    public void Schema_inheritance_extends_base_schema_with_additional_segments()
    {
        var baseSchema = new X12TransactionSchema("837", "Base 837",
            new X12SegmentSchema("CLM", new[] { "PatientControlNumber", "TotalCharge" }));

        // Derived schema adds a dental-specific segment
        var dentalSchema = baseSchema.Extend("837D", "Dental 837",
            new X12SegmentSchema("DN1", new[] { "ToothCode", "Surface" }));

        // Inherited segments are present
        Assert.NotNull(dentalSchema.GetSegment("CLM"));
        // Extended segment is also present
        Assert.NotNull(dentalSchema.GetSegment("DN1"));
        Assert.Equal("837D", dentalSchema.TransactionSetId);
    }

    // ── Cycle 3 (Phase 4) ─────────────────────────────────────────────────

    [Fact]
    public void SchemaValidator_fails_when_required_segment_is_missing()
    {
        const string input = "ST*271*0001~EB*1~SE*2*0001~";  // BHT missing

        var result = X12SchemaValidator.Validate(input, Ts271Schema);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Message.Contains("BHT"));
    }

    // ── Cycle 4 (Phase 4) ─────────────────────────────────────────────────

    [Fact]
    public void SchemaValidator_passes_when_all_required_segments_present()
    {
        const string input = "ST*271*0001~BHT*0022~SE*2*0001~";  // BHT present, EB optional and absent

        var result = X12SchemaValidator.Validate(input, Ts271Schema);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    // ── Cycle 2 (Phase 5) ─────────────────────────────────────────────────

    private const string TwoTxInterchange =
        "ISA*00*          *00*          *ZZ*SENDER         *ZZ*RECEIVER       *201909*1200*^*00501*000000001*0*P*:~" +
        "GS*HB*SENDER*RECEIVER*20190901*1200*1*X*005010X279A1~" +
        "ST*271*0001~" +
        "BHT*0022*11*10001234*20190901*1200~" +
        "SE*2*0001~" +
        "ST*271*0002~" +
        "SE*1*0002~" +                          // BHT missing in second transaction
        "GE*2*1~" +
        "IEA*1*000000001~";

    [Fact]
    public void SchemaRegistry_validates_all_transactions_in_interchange()
    {
        var registry = new X12SchemaRegistry();
        registry.Register(new X12TransactionSchema("271", "Eligibility Response",
            new X12SegmentSchema("BHT", new[] { "HierarchicalStructureCode" }, isRequired: true)));

        var interchange = X12Net.DOM.X12Interchange.Parse(TwoTxInterchange);
        var errors = X12SchemaValidator.ValidateInterchange(interchange, registry);

        // First transaction has BHT → valid; second is missing BHT → one error
        Assert.Single(errors);
        Assert.Contains("BHT", errors[0].Message);
    }

    // ── Cycle 3 (Phase 7) ─────────────────────────────────────────────────

    [Fact]
    public void ValidateInterchange_skips_transactions_with_no_registered_schema()
    {
        // Registry has no schema for 999 — the transaction should be silently skipped
        var registry = new X12SchemaRegistry();
        registry.Register(new X12TransactionSchema("270", "Eligibility Inquiry",
            new X12SegmentSchema("BHT", new[] { "Code" }, isRequired: true)));

        const string input =
            "ISA*00*          *00*          *ZZ*SENDER         *ZZ*RECEIVER       *201909*1200*^*00501*000000001*0*P*:~" +
            "GS*FA*SENDER*RECEIVER*20190901*1200*1*X*005010X231A1~" +
            "ST*999*0001~" +
            "AK1*FA*1*005010X231A1~" +
            "SE*3*0001~" +
            "GE*1*1~" +
            "IEA*1*000000001~";

        var interchange = X12Net.DOM.X12Interchange.Parse(input);
        var errors = X12SchemaValidator.ValidateInterchange(interchange, registry);

        Assert.Empty(errors);
    }

    // ── Cycle 4 (Phase 5) ─────────────────────────────────────────────────

    [Fact]
    public void DynamicTransaction_AllSegments_returns_every_occurrence()
    {
        var schema = new X12TransactionSchema("271", "Eligibility Response",
            new X12SegmentSchema("EB", new[] { "EligibilityCode" }));

        const string input = "ST*271*0001~EB*1~EB*C~EB*W~SE*4*0001~";
        var tx = X12DynamicTransaction.Parse(input, schema);

        var allEb = tx.AllSegments("EB").ToList();

        Assert.Equal(3, allEb.Count);
        Assert.Equal("1", allEb[0][1]);  // EB01
        Assert.Equal("C", allEb[1][1]);
        Assert.Equal("W", allEb[2][1]);
    }

    // ── Cycle 4 (Phase 8) ─────────────────────────────────────────────────

    [Fact]
    public void DynamicTransaction_AllSegments_returns_empty_for_unregistered_segment()
    {
        var schema = new X12TransactionSchema("271", "Eligibility Response",
            new X12SegmentSchema("EB", new[] { "EligibilityCode" }));

        const string input = "ST*271*0001~EB*1~SE*2*0001~";
        var tx = X12DynamicTransaction.Parse(input, schema);

        var result = tx.AllSegments("NM1");  // NM1 not in schema or input

        Assert.Empty(result);
    }

    // ── Cycle 5 (Phase 4) ─────────────────────────────────────────────────

    [Fact]
    public void DynamicTransaction_throws_when_segment_not_found_in_schema()
    {
        var schema = new X12TransactionSchema("ZZZ", "Custom",
            new X12SegmentSchema("ZZZ", new[] { "Field1" }));

        const string input = "ZZZ*HELLO~";
        var tx = X12DynamicTransaction.Parse(input, schema);

        // "MISSING" segment was never in the input or schema
        Assert.Throws<KeyNotFoundException>(() => _ = tx["MISSING", "Field1"]);
    }

    // ── Cycle 12 ──────────────────────────────────────────────────────────

    [Fact]
    public void Document_ParseWithSchema_maps_element_to_named_property()
    {
        var schema = new X12TransactionSchema("999", "Functional Ack",
            new X12SegmentSchema("AK1", new[] { "FunctionalIdentifierCode", "GroupControlNumber", "Version" }),
            new X12SegmentSchema("AK9", new[] { "AckCode", "NumberIncluded", "NumberReceived", "NumberAccepted" }));

        const string input =
            "ISA*00*          *00*          *ZZ*SENDER         *ZZ*RECEIVER       *201909*1200*^*00501*000000001*0*P*:~" +
            "GS*FA*SENDER*RECEIVER*20190901*1200*1*X*005010X231A1~" +
            "ST*999*0001~" +
            "AK1*FA*1*005010X231A1~" +
            "AK9*A*1*1*1~" +
            "SE*4*0001~" +
            "GE*1*1~" +
            "IEA*1*000000001~";

        var tx = X12DynamicTransaction.Parse(input, schema);

        Assert.Equal("FA",           tx["AK1", "FunctionalIdentifierCode"]);
        Assert.Equal("1",            tx["AK1", "GroupControlNumber"]);
        Assert.Equal("005010X231A1", tx["AK1", "Version"]);
        Assert.Equal("A",            tx["AK9", "AckCode"]);
    }
}
