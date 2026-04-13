using woliver13.X12Net.Core;
using woliver13.X12Net.IO;

namespace woliver13.X12Net.Schema;

/// <summary>
/// A schema-driven view of a parsed EDI X12 interchange.
/// Elements can be accessed by segment ID and element name as defined in the
/// <see cref="X12TransactionSchema"/> supplied at parse time.
/// </summary>
public sealed class X12DynamicTransaction
{
    // Segment ID → (segment, schema) pairs, keyed for fast lookup.
    // Multiple segments with the same ID are supported; the first is returned by default.
    private readonly Dictionary<string, (X12Segment Segment, X12SegmentSchema Schema)> _index;
    private readonly Dictionary<string, List<X12Segment>> _all;

    private X12DynamicTransaction(
        Dictionary<string, (X12Segment, X12SegmentSchema)> index,
        Dictionary<string, List<X12Segment>> all)
    {
        _index = index;
        _all   = all;
    }

    // ── Factory ───────────────────────────────────────────────────────────

    /// <summary>
    /// Parses raw EDI X12 text and builds an indexed view using the supplied schema.
    /// </summary>
    public static X12DynamicTransaction Parse(string input, X12TransactionSchema schema)
    {
        using var reader = new X12Reader(input);
        var index = new Dictionary<string, (X12Segment, X12SegmentSchema)>(
            StringComparer.OrdinalIgnoreCase);
        var all = new Dictionary<string, List<X12Segment>>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var seg in reader.ReadAllSegments())
        {
            var segSchema = schema.GetSegment(seg.SegmentId);
            if (segSchema is null) continue;

            if (!index.ContainsKey(seg.SegmentId))
                index[seg.SegmentId] = (seg, segSchema);

            if (!all.TryGetValue(seg.SegmentId, out var list))
                all[seg.SegmentId] = list = new List<X12Segment>();
            list.Add(seg);
        }

        return new X12DynamicTransaction(index, all);
    }

    // ── Multi-segment access ──────────────────────────────────────────────

    /// <summary>
    /// Returns all occurrences of <paramref name="segmentId"/> in document order,
    /// or an empty list if none were found.
    /// </summary>
    public IReadOnlyList<X12Segment> AllSegments(string segmentId) =>
        _all.TryGetValue(segmentId, out var list) ? list : new List<X12Segment>();

    // ── Element access ────────────────────────────────────────────────────

    /// <summary>
    /// Gets the element value identified by <paramref name="segmentId"/> and
    /// <paramref name="elementName"/> as defined in the schema.
    /// </summary>
    /// <exception cref="KeyNotFoundException">
    /// Thrown if the segment or element name is not found.
    /// </exception>
    public string this[string segmentId, string elementName]
    {
        get
        {
            if (!_index.TryGetValue(segmentId, out var pair))
                throw new KeyNotFoundException(
                    $"Segment '{segmentId}' was not found in the parsed input or schema.");

            int idx = pair.Schema.IndexOf(elementName);
            if (idx < 0)
                throw new KeyNotFoundException(
                    $"Element name '{elementName}' is not defined in the schema for segment '{segmentId}'.");

            return pair.Segment[idx];
        }
    }
}
