namespace X12Net.Core;

/// <summary>
/// Converts raw EDI X12 text into a flat stream of <see cref="X12Token"/> values.
/// Delimiter auto-detection fires when the input begins with an ISA segment;
/// detection works by counting element separators (correct for both standard
/// 106-char and any non-standard-width ISA).
/// </summary>
public static class X12Tokenizer
{
    // ── ISA geometry ─────────────────────────────────────────────────────
    // ISA has 16 elements. The element separator appears 16 times within the
    // segment (once before each element). ISA16 (the component separator) is
    // always a single character; the segment terminator immediately follows it.
    private const int IsaElementCount = 16;
    private const int MinIsaLength    = 106;  // still useful as a quick pre-check

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>
    /// Detects the three delimiter characters from the beginning of an ISA segment
    /// by counting element separators — correct for both standard (106-char) and
    /// any non-standard-width ISA.
    /// </summary>
    /// <exception cref="ArgumentException">Input is null, too short, or does not start with "ISA".</exception>
    public static X12Delimiters DetectDelimiters(string isaInput)
    {
        if (string.IsNullOrEmpty(isaInput) || isaInput.Length < MinIsaLength)
            throw new ArgumentException(
                $"ISA segment must be at least {MinIsaLength} characters.", nameof(isaInput));

        if (!isaInput.StartsWith("ISA", StringComparison.Ordinal))
            throw new ArgumentException("Input does not start with 'ISA'.", nameof(isaInput));

        char elementSep = isaInput[3];

        // Walk the string counting element separators until we reach the 15th one
        // (which separates ISA15 from ISA16). ISA16 is 1 char; the segment
        // terminator is the character immediately after ISA16.
        int sepCount = 0;
        for (int i = 4; i < isaInput.Length; i++)
        {
            if (isaInput[i] == elementSep)
            {
                sepCount++;
                if (sepCount == IsaElementCount - 1)  // 15th separator: between ISA15 and ISA16
                {
                    char componentSep   = i + 1 < isaInput.Length ? isaInput[i + 1] : ':';
                    char segmentTerm    = i + 2 < isaInput.Length ? isaInput[i + 2] : '~';
                    return new X12Delimiters(elementSep, componentSep, segmentTerm);
                }
            }
        }

        // Fallback — should only be reached for malformed ISA
        return new X12Delimiters(elementSep, ':', '~');
    }

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
        if (input.Length >= MinIsaLength && input.StartsWith("ISA", StringComparison.Ordinal))
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
        int   pos          = 0;
        bool  firstInSeg   = true;
        bool  pendingFlush = false;
        char? lastDelim    = null;

        while (pos < input.Length)
        {
            int next = IndexOfAny(input, pos, elementSep, componentSep, segmentTerm);

            string value = next < 0 ? input.Substring(pos) : input.Substring(pos, next - pos);

            if (firstInSeg)
            {
                if (value.Length > 0)
                    yield return new X12Token(X12TokenType.SegmentId, value);
                firstInSeg   = false;
                pendingFlush = false;
            }
            else
            {
                var type = lastDelim == componentSep
                    ? X12TokenType.ComponentData
                    : X12TokenType.ElementData;
                yield return new X12Token(type, value);
                pendingFlush = true;
            }

            if (next < 0)
                break;

            char delim = input[next];
            lastDelim  = delim;

            if (delim == segmentTerm)
            {
                yield return new X12Token(X12TokenType.SegmentTerminator, segmentTerm.ToString());
                firstInSeg   = true;
                pendingFlush = false;
                lastDelim    = null;
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
