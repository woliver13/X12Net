using woliver13.HL7Net.Core;
using woliver13.HL7Net.IO;
using Shouldly;
using Xunit;

namespace HL7Net.Tests.IO;

public class Hl7WriterTests
{
    private static readonly Hl7Delimiters Delimiters =
        Hl7Delimiters.FromMsh("MSH|^~\\&|");

    [Fact]
    public void WriteSegment_NonMsh_ProducesFieldSeparatedLine()
    {
        var writer = new Hl7Writer(Delimiters);
        var result = writer.WriteSegment("PID", "1", "12345", "Doe^John");
        result.ShouldBe("PID|1|12345|Doe^John\r");
    }

    [Fact]
    public void WriteSegment_NonMsh_EmptyFields_ArePreserved()
    {
        var writer = new Hl7Writer(Delimiters);
        var result = writer.WriteSegment("PV1", "", "I", "", "");
        result.ShouldBe("PV1||I||\r");
    }

    [Fact]
    public void WriteSegment_Msh_ProducesCorrectHeader()
    {
        var writer = new Hl7Writer(Delimiters);
        // WriteSegment("MSH", "|", "^~\\&", "SendApp", "SendFac")
        // → MSH|^~\&|SendApp|SendFac\r
        var result = writer.WriteSegment("MSH", "|", "^~\\&", "SendApp", "SendFac");
        result.ShouldBe("MSH|^~\\&|SendApp|SendFac\r");
    }
}
