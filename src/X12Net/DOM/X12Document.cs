using X12Net.IO;

namespace X12Net.DOM;

/// <summary>
/// An in-memory, editable representation of an EDI X12 interchange.
/// Segments are accessible by index; individual elements may be read and modified
/// using the <c>doc["SegmentId", elementIndex]</c> indexer.
/// </summary>
public sealed class X12Document
{
    private readonly List<X12MutableSegment> _segments;
    private readonly char _elementSeparator;
    private readonly char _componentSeparator;
    private readonly char _segmentTerminator;

    private X12Document(
        List<X12MutableSegment> segments,
        char elementSeparator,
        char componentSeparator,
        char segmentTerminator)
    {
        _segments           = segments;
        _elementSeparator   = elementSeparator;
        _componentSeparator = componentSeparator;
        _segmentTerminator  = segmentTerminator;
    }

    // ── Factory ───────────────────────────────────────────────────────────

    /// <summary>Parses raw EDI X12 text into an <see cref="X12Document"/>.</summary>
    public static X12Document Parse(string input)
    {
        using var reader = new X12Reader(input);

        // Detect delimiters from the ISA header if present
        char elementSep   = '*';
        char componentSep = ':';
        char segmentTerm  = '~';

        if (input.Length >= 106 && input.StartsWith("ISA", StringComparison.Ordinal))
        {
            elementSep   = input[3];
            componentSep = input[104];
            segmentTerm  = input[105];
        }

        var segments = reader
            .ReadAllSegments()
            .Select(s => new X12MutableSegment(s.SegmentId, s.Elements))
            .ToList();

        return new X12Document(segments, elementSep, componentSep, segmentTerm);
    }

    // ── Segment access ────────────────────────────────────────────────────

    /// <summary>All segments in document order.</summary>
    public IReadOnlyList<X12MutableSegment> Segments => _segments;

    // ── Generic multi-segment indexer ────────────────────────────────────

    /// <summary>
    /// Returns all mutable segments whose <see cref="X12MutableSegment.SegmentId"/>
    /// matches <paramref name="segmentId"/>, in document order.
    /// </summary>
    public IReadOnlyList<X12MutableSegment> AllSegments(string segmentId) =>
        _segments.Where(s => s.SegmentId == segmentId).ToList();

    // ── Element indexer ───────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets an element by segment identifier and 1-based element index.
    /// When multiple segments share the same identifier, the first one is targeted.
    /// </summary>
    public string this[string segmentId, int elementIndex]
    {
        get
        {
            var seg = FindFirst(segmentId);
            return seg[elementIndex];
        }
        set
        {
            var seg = FindFirst(segmentId);
            seg[elementIndex] = value;
        }
    }

    // ── Serialization ─────────────────────────────────────────────────────

    /// <summary>Serializes the document back to EDI X12 text.</summary>
    public override string ToString()
    {
        var writer = new X12Writer(_elementSeparator, _componentSeparator, _segmentTerminator);
        foreach (var seg in _segments)
            writer.WriteSegment(seg.SegmentId, seg.Elements.ToArray());
        return writer.ToString();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private X12MutableSegment FindFirst(string segmentId)
    {
        var seg = _segments.FirstOrDefault(s => s.SegmentId == segmentId);
        if (seg is null)
            throw new KeyNotFoundException($"Segment '{segmentId}' not found in document.");
        return seg;
    }
}
