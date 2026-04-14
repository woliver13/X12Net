using woliver13.HL7Net.DOM;
using Shouldly;
using Xunit;

namespace HL7Net.Tests.DOM;

public class Hl7DocumentTests
{
    private const string AdtMessage =
        "MSH|^~\\&|SendApp|SendFac|RecvApp|RecvFac|20230101120000||ADT^A01|MSG001|P|2.5\r" +
        "EVN|A01|20230101120000\r" +
        "PID|1||12345^^^MRN||Doe^John^A||19800101|M\r";

    [Fact]
    public void Parse_ThenToString_RoundtripsText()
    {
        var doc = Hl7Document.Parse(AdtMessage);
        doc.ToString().ShouldBe(AdtMessage);
    }

    [Fact]
    public void Parse_ContainsThreeSegments()
    {
        var doc = Hl7Document.Parse(AdtMessage);
        doc.Segments.Count.ShouldBe(3);
    }

    [Fact]
    public void ElementEdit_ChangesFieldValue()
    {
        var doc = Hl7Document.Parse(AdtMessage);
        var pid = doc.Segments.First(s => s.SegmentId == "PID");
        // PID-5 = "Doe^John^A" — change the patient name
        pid[5] = "Smith^Jane^B";
        pid[5].ShouldBe("Smith^Jane^B");
    }

    [Fact]
    public void ElementEdit_ReflectsInToString()
    {
        var doc = Hl7Document.Parse(AdtMessage);
        var pid = doc.Segments.First(s => s.SegmentId == "PID");
        pid[5] = "Smith^Jane^B";
        doc.ToString().ShouldContain("Smith^Jane^B");
    }
}
