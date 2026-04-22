using woliver13.HL7Net.IO;
using Shouldly;
using Xunit;

namespace HL7Net.Tests.IO;

public class Hl7ReaderTests
{
    // Standard HL7 v2.5 ADT message with \r terminators
    private const string SimpleMessage =
        "MSH|^~\\&|SendApp|SendFac|RecvApp|RecvFac|20230101120000||ADT^A01|MSG001|P|2.5\r" +
        "EVN|A01|20230101120000\r" +
        "PID|1||12345^^^MRN||Doe^John^A||19800101|M\r";

    [Fact]
    public void ReadAllSegments_ReturnsThreeSegments()
    {
        var reader = new Hl7Reader(SimpleMessage);
        var segments = reader.ReadAllSegments().ToList();
        segments.Count.ShouldBe(3);
    }

    [Fact]
    public void ReadAllSegments_FirstSegmentIsMsh()
    {
        var reader = new Hl7Reader(SimpleMessage);
        var segments = reader.ReadAllSegments().ToList();
        segments[0].SegmentId.ShouldBe("MSH");
    }

    [Fact]
    public void ReadAllSegments_SegmentIds_AreCorrect()
    {
        var reader = new Hl7Reader(SimpleMessage);
        var ids = reader.ReadAllSegments().Select(s => s.SegmentId).ToList();
        ids.ShouldBe(new[] { "MSH", "EVN", "PID" });
    }

    [Fact]
    public void Segment_OneBasedIndexer_ReturnsCorrectField()
    {
        var reader = new Hl7Reader(SimpleMessage);
        var pid = reader.ReadAllSegments().First(s => s.SegmentId == "PID");
        // PID|1||12345^^^MRN||Doe^John^A||19800101|M
        pid[1].ShouldBe("1");
        pid[3].ShouldBe("12345^^^MRN");
        pid[8].ShouldBe("M");
    }

    [Fact]
    public void Segment_IndexerBeyondLastField_ReturnsEmpty()
    {
        var reader = new Hl7Reader(SimpleMessage);
        var evn = reader.ReadAllSegments().First(s => s.SegmentId == "EVN");
        evn[99].ShouldBe(string.Empty);
    }

    [Fact]
    public void Segment_ComponentValues_PreservedIntact()
    {
        var reader = new Hl7Reader(SimpleMessage);
        var msh = reader.ReadAllSegments().First(s => s.SegmentId == "MSH");
        // MSH-9 = "ADT^A01" — the component separator is NOT split by the reader
        msh[9].ShouldBe("ADT^A01");
    }

    [Fact]
    public void Msh_Field1_IsFieldSeparator()
    {
        var reader = new Hl7Reader(SimpleMessage);
        var msh = reader.ReadAllSegments().First(s => s.SegmentId == "MSH");
        msh[1].ShouldBe("|");
    }

    [Fact]
    public void Msh_Field2_IsEncodingChars()
    {
        var reader = new Hl7Reader(SimpleMessage);
        var msh = reader.ReadAllSegments().First(s => s.SegmentId == "MSH");
        msh[2].ShouldBe("^~\\&");
    }

    [Fact]
    public void ReadAllSegments_AcceptsNewlineTerminator()
    {
        var msg = SimpleMessage.Replace('\r', '\n');
        var reader = new Hl7Reader(msg);
        reader.ReadAllSegments().Count().ShouldBe(3);
    }

    [Fact]
    public void ReadAllSegments_AcceptsCrLfTerminator()
    {
        var msg = "MSH|^~\\&|A|B|C|D|20230101120000||ADT^A01|1|P|2.5\r\n" +
                  "EVN|A01|20230101120000\r\n" +
                  "PID|1||123\r\n";
        var reader = new Hl7Reader(msg);
        reader.ReadAllSegments().Count().ShouldBe(3);
    }

    [Fact]
    public void ReadAllSegments_ThrowsWhenSegmentCapExceeded()
    {
        // Build a message with 3 segments but cap at 2
        var reader = new Hl7Reader(SimpleMessage, maxSegments: 2);
        var ex = Should.Throw<Hl7MemoryCapException>(() => reader.ReadAllSegments().ToList());
        ex.MaxSegments.ShouldBe(2);
        ex.Message.ShouldContain("2");
    }
}
