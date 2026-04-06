using X12Net.Core;

namespace X12Net.Tests.Core;

public class X12TokenizerTests
{
    // ── Cycles ───────────────────────────────────────────────────────────────

    [Fact]
    public void Tokenizer_splits_segment_into_element_tokens()
    {
        // NM1*IL*1*DOE*JOHN~  (element sep=*, segment terminator=~)
        var tokens = X12Tokenizer.Tokenize("NM1*IL*1*DOE*JOHN~").ToList();

        Assert.Equal(X12TokenType.SegmentId,  tokens[0].Type);
        Assert.Equal("NM1",                   tokens[0].Value);

        Assert.Equal(X12TokenType.ElementData, tokens[1].Type);
        Assert.Equal("IL",                    tokens[1].Value);

        Assert.Equal(X12TokenType.ElementData, tokens[2].Type);
        Assert.Equal("1",                     tokens[2].Value);

        Assert.Equal(X12TokenType.ElementData, tokens[3].Type);
        Assert.Equal("DOE",                   tokens[3].Value);

        Assert.Equal(X12TokenType.ElementData, tokens[4].Type);
        Assert.Equal("JOHN",                  tokens[4].Value);
    }

    // A minimal 106-char ISA using | as element sep, ^ as component sep, \n as segment terminator.
    // ISA(3) + element-sep(1) + 15 fixed-width fields = 106 chars total.
    private const string IsaCustomDelimiters =
        "ISA|00|          |00|          |ZZ|SENDER         |ZZ|RECEIVER       |201909|1200|~|00501|000000001|0|P|^" + "\n";
    //                                                                                   ^^                     ^^ ^^
    //  pos 3 = '|' (element sep)                                         pos 104 = '^' (component sep)   pos 105 = '\n' (segment term)

    [Fact]
    public void Tokenizer_detects_delimiters_from_ISA_header()
    {
        var d = X12Tokenizer.DetectDelimiters(IsaCustomDelimiters);

        Assert.Equal('|',  d.ElementSeparator);   // position 3
        Assert.Equal('^',  d.ComponentSeparator);  // ISA16
        Assert.Equal('\n', d.SegmentTerminator);   // char after ISA16
    }

    [Fact]
    public void Tokenizer_uses_detected_delimiters_for_subsequent_segments()
    {
        // After ISA, GS uses the same custom element separator '|' and terminator '\n'
        string input = IsaCustomDelimiters + "GS|FA|SENDER|RECEIVER|20190901|1200|1|X|005010X231A1\n";

        var tokens = X12Tokenizer.Tokenize(input).ToList();
        var gsId = tokens.First(t => t.Type == X12TokenType.SegmentId && t.Value == "GS");
        var gsIndex = tokens.IndexOf(gsId);

        Assert.Equal("FA", tokens[gsIndex + 1].Value);
        Assert.Equal(X12TokenType.ElementData, tokens[gsIndex + 1].Type);
    }

    [Fact]
    public void Tokenizer_emits_ComponentData_token_for_component_separator()
    {
        // CLM*1234*B:1:1~  — "B:1:1" is a composite element
        var tokens = X12Tokenizer.Tokenize("CLM*1234*B:1:1~").ToList();

        Assert.Equal(X12TokenType.SegmentId,   tokens[0].Type); // CLM
        Assert.Equal(X12TokenType.ElementData,  tokens[1].Type); // 1234
        Assert.Equal(X12TokenType.ElementData,  tokens[2].Type); // B   ← first in composite = ElementData
        Assert.Equal(X12TokenType.ComponentData, tokens[3].Type); // 1   ← component
        Assert.Equal(X12TokenType.ComponentData, tokens[4].Type); // 1   ← component
        Assert.Equal("B", tokens[2].Value);
        Assert.Equal("1", tokens[3].Value);
        Assert.Equal("1", tokens[4].Value);
    }

    [Fact]
    public void Tokenizer_emits_SegmentTerminator_token_at_end_of_segment()
    {
        var tokens = X12Tokenizer.Tokenize("NM1*IL~").ToList();

        var terminator = tokens.Last();
        Assert.Equal(X12TokenType.SegmentTerminator, terminator.Type);
        Assert.Equal("~", terminator.Value);
    }
}
