namespace woliver13.X12Net.Core;

/// <summary>
/// Converts raw EDI X12 text into a flat stream of <see cref="X12Token"/> values.
/// When the input begins with an ISA segment, delimiters are auto-detected via
/// <see cref="DetectDelimiters"/>.
/// </summary>
public static class X12Tokenizer
{
    // quick pre-check before delegating to X12IsaParser — see X12Constants.IsaMinLength

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>
    /// Detects the three delimiter characters from the beginning of an ISA segment
    /// by counting element separators — correct for both standard (106-char) and
    /// any non-standard-width ISA.
    /// </summary>
    /// <exception cref="ArgumentException">Input is null, too short, or does not start with "ISA".</exception>
    public static X12Delimiters DetectDelimiters(string isaInput) =>
        X12IsaParser.Parse(isaInput);

    /// <summary>
    /// Tokenizes a single segment (or a full interchange) using the supplied
    /// or auto-detected delimiters.
    /// </summary>
    public static IEnumerable<X12Token> Tokenize(
        string input,
        char elementSeparator   = '*',
        char componentSeparator = ':',
        char segmentTerminator  = '~')
    {
        if (string.IsNullOrEmpty(input))
            yield break;

        // Auto-detect when the input starts with ISA and is long enough
        if (input.Length >= X12Constants.IsaMinLength && input.StartsWith("ISA", StringComparison.Ordinal))
        {
            var d          = DetectDelimiters(input);
            elementSeparator   = d.ElementSeparator;
            componentSeparator = d.ComponentSeparator;
            segmentTerminator  = d.SegmentTerminator;
        }

        foreach (var token in TokenizeWithDelimiters(
            input, elementSeparator, componentSeparator, segmentTerminator))
            yield return token;
    }

    /// <summary>
    /// Tokenizes using explicit delimiters (no auto-detection).
    /// </summary>
    public static IEnumerable<X12Token> Tokenize(string input, X12Delimiters delimiters) =>
        Tokenize(input, delimiters.ElementSeparator, delimiters.ComponentSeparator, delimiters.SegmentTerminator);

    // ── Core splitting logic ──────────────────────────────────────────────

    private static IEnumerable<X12Token> TokenizeWithDelimiters(
        string input, char elementSep, char componentSep, char segmentTerm)
    {
        int   pos        = 0;
        bool  firstInSeg = true;
        char? lastDelim  = null;

        while (pos < input.Length)
        {
            int next = IndexOfAny(input, pos, elementSep, componentSep, segmentTerm);

            string value = next < 0 ? input.Substring(pos) : input.Substring(pos, next - pos);

            if (firstInSeg)
            {
                if (value.Length > 0)
                    yield return new X12Token(X12TokenType.SegmentId, value);
                firstInSeg = false;
            }
            else
            {
                var type = lastDelim == componentSep
                    ? X12TokenType.ComponentData
                    : X12TokenType.ElementData;
                yield return new X12Token(type, value);
            }

            if (next < 0)
                break;

            char delim = input[next];
            lastDelim  = delim;

            if (delim == segmentTerm)
            {
                yield return new X12Token(X12TokenType.SegmentTerminator, segmentTerm.ToString());
                firstInSeg = true;
                lastDelim  = null;
            }

            pos = next + 1;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static int IndexOfAny(string s, int startIndex, char c1, char c2, char c3)
    {
        for (int i = startIndex; i < s.Length; i++)
        {
            char c = s[i];
            if (c == c1 || c == c2 || c == c3) return i;
        }
        return -1;
    }
}
