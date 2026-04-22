using System.Text;
using woliver13.X12Net.Core;

namespace woliver13.X12Net.DOM;

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
        Delimiters          = new X12Delimiters(elementSeparator, componentSeparator, segmentTerminator);
    }

    /// <summary>The delimiters used when this document was parsed.</summary>
    public X12Delimiters Delimiters { get; }

    // ── Factory ───────────────────────────────────────────────────────────

    /// <summary>Parses raw EDI X12 text into an <see cref="X12Document"/>.</summary>
    public static X12Document Parse(string input)
    {
        var d = X12Delimiters.FromIsa(input);

        var segments = X12SegmentParser
            .ParseAll(input, d)
            .Select(s => new X12MutableSegment(s.SegmentId, s.Elements))
            .ToList();

        return new X12Document(segments, d.ElementSeparator, d.ComponentSeparator, d.SegmentTerminator);
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
        var sb = new StringBuilder();
        foreach (var seg in _segments)
        {
            sb.Append(seg.SegmentId);
            foreach (var el in seg.Elements)
            {
                sb.Append(_elementSeparator);
                sb.Append(el);
            }
            sb.Append(_segmentTerminator);
        }
        return sb.ToString();
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
