using woliver13.HL7Net.DOM;
using Shouldly;
using Xunit;

namespace HL7Net.Tests.DOM;

public class Hl7MessageTests
{
    private const string AdtMessage =
        "MSH|^~\\&|SendApp|SendFac|RecvApp|RecvFac|20230101120000||ADT^A01|MSG001|P|2.5\r" +
        "EVN|A01|20230101120000\r" +
        "PID|1||12345^^^MRN||Doe^John^A||19800101|M\r";

    [Fact]
    public void Parse_BuildsDomWithAllSegments()
    {
        var msg = Hl7Message.Parse(AdtMessage);
        msg.Segments.Count.ShouldBe(3);
    }

    [Fact]
    public void Parse_FirstSegmentIsMsh()
    {
        var msg = Hl7Message.Parse(AdtMessage);
        msg.Segments[0].SegmentId.ShouldBe("MSH");
    }

    [Fact]
    public void MessageType_ReturnsFullMsh9Value()
    {
        var msg = Hl7Message.Parse(AdtMessage);
        // MSH-9 = "ADT^A01"
        msg.MessageType.ShouldBe("ADT^A01");
    }

    [Fact]
    public void Version_ReturnsMsh12Value()
    {
        var msg = Hl7Message.Parse(AdtMessage);
        // MSH-12 = "2.5"
        msg.Version.ShouldBe("2.5");
    }
}
