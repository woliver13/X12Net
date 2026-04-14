using woliver13.HL7Net.Core;
using Shouldly;
using Xunit;

namespace HL7Net.Tests.Core;

public class Hl7DelimitersTests
{
    private const string StandardMsh =
        "MSH|^~\\&|SendApp|SendFac|RecvApp|RecvFac|20230101120000||ADT^A01|MSG001|P|2.5";

    [Fact]
    public void FromMsh_DetectsFieldSeparator()
    {
        var d = Hl7Delimiters.FromMsh(StandardMsh);
        d.FieldSeparator.ShouldBe('|');
    }

    [Fact]
    public void FromMsh_DetectsComponentSeparator()
    {
        var d = Hl7Delimiters.FromMsh(StandardMsh);
        d.ComponentSeparator.ShouldBe('^');
    }

    [Fact]
    public void FromMsh_DetectsRepetitionSeparator()
    {
        var d = Hl7Delimiters.FromMsh(StandardMsh);
        d.RepetitionSeparator.ShouldBe('~');
    }

    [Fact]
    public void FromMsh_DetectsEscapeCharacter()
    {
        var d = Hl7Delimiters.FromMsh(StandardMsh);
        d.EscapeCharacter.ShouldBe('\\');
    }

    [Fact]
    public void FromMsh_DetectsSubComponentSeparator()
    {
        var d = Hl7Delimiters.FromMsh(StandardMsh);
        d.SubComponentSeparator.ShouldBe('&');
    }

    [Fact]
    public void FromMsh_ThrowsOnNonMshLine()
    {
        Should.Throw<ArgumentException>(() => Hl7Delimiters.FromMsh("PID|1|..."));
    }
}
