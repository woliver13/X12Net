using X12Net.Schema;

namespace X12Net.Tests.Schema;

public class X12SchemaTests
{
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
