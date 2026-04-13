using woliver13.X12Net.Core;

namespace woliver13.X12Net.Tests.Core;

public class X12TokenTests
{
    [Fact]
    public void Token_has_Type_and_Value_properties()
    {
        var token = new X12Token(X12TokenType.SegmentId, "ISA");

        Assert.Equal(X12TokenType.SegmentId, token.Type);
        Assert.Equal("ISA", token.Value);
    }
}
