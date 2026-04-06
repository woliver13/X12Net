using X12Net.Transactions;

namespace X12Net.Tests.Transactions;

public class Ts999Tests
{
    // Minimal 999 interchange: ISA + GS + ST + AK1 + AK9 + SE + GE + IEA
    private const string Input999 =
        "ISA*00*          *00*          *ZZ*SENDER         *ZZ*RECEIVER       *201909*1200*^*00501*000000001*0*P*:~" +
        "GS*FA*SENDER*RECEIVER*20190901*1200*1*X*005010X231A1~" +
        "ST*999*0001~" +
        "AK1*FA*1*005010X231A1~" +
        "AK9*A*1*1*1~" +
        "SE*4*0001~" +
        "GE*1*1~" +
        "IEA*1*000000001~";

    [Fact]
    public void Generated_999_document_has_typed_AK1_segment_property()
    {
        var ts = Ts999.Parse(Input999);

        Assert.NotNull(ts.AK1);
    }

    [Fact]
    public void Generated_999_document_populates_AK1_from_parsed_document()
    {
        var ts = Ts999.Parse(Input999);

        Assert.Equal("FA",              ts.AK1!.FunctionalIdentifierCode); // AK101
        Assert.Equal("1",               ts.AK1.GroupControlNumber);         // AK102
    }
}
