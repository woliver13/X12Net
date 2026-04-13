namespace woliver13.X12Net.Core;

/// <summary>
/// Converts raw EDI X12 text into a flat sequence of <see cref="X12Segment"/> values.
/// This is the pure-domain parsing layer — no I/O, no streams, no logging.
/// <see cref="woliver13.X12Net.IO.X12Reader"/> builds on top of this class to add stream
/// support, memory caps, async enumeration, and structured logging.
/// </summary>
internal static class X12SegmentParser
{
    /// <summary>
    /// Parses <paramref name="input"/> using the supplied <paramref name="delimiters"/> and
    /// returns all segments in document order.
    /// </summary>
    internal static IEnumerable<X12Segment> ParseAll(string input, X12Delimiters delimiters)
    {
        var segmentId = string.Empty;
        var elements  = new List<string>();
        var composite = new System.Text.StringBuilder();
        // True once the first ElementData token has been seen in the current segment.
        // Used to know whether to flush composite before starting the next element.
        bool pendingFlush = false;

        var tokens = X12Tokenizer.Tokenize(input, delimiters);
        foreach (var token in tokens)
        {
            switch (token.Type)
            {
                case X12TokenType.SegmentId:
                    segmentId    = token.Value;
                    elements     = new List<string>();
                    composite.Clear();
                    pendingFlush = false;
                    break;

                case X12TokenType.ElementData:
                    if (pendingFlush)
                    {
                        // Flush previous element (may be empty, simple, or composite)
                        elements.Add(composite.ToString());
                        composite.Clear();
                    }
                    composite.Append(token.Value);
                    pendingFlush = true;
                    break;

                case X12TokenType.ComponentData:
                    composite.Append(delimiters.ComponentSeparator);
                    composite.Append(token.Value);
                    break;

                case X12TokenType.SegmentTerminator:
                    if (pendingFlush)
                        elements.Add(composite.ToString());

                    if (segmentId.Length > 0)
                        yield return new X12Segment(segmentId, elements.AsReadOnly());

                    segmentId    = string.Empty;
                    elements     = new List<string>();
                    composite.Clear();
                    pendingFlush = false;
                    break;
            }
        }

        // Yield any trailing segment without a terminator
        if (pendingFlush)
            elements.Add(composite.ToString());
        if (segmentId.Length > 0)
            yield return new X12Segment(segmentId, elements.AsReadOnly());
    }
}
