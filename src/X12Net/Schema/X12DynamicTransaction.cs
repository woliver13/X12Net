using X12Net.IO;

namespace X12Net.Schema;

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

    private X12DynamicTransaction(
        Dictionary<string, (X12Segment, X12SegmentSchema)> index)
    {
        _index = index;
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

        foreach (var seg in reader.ReadAllSegments())
        {
            var segSchema = schema.GetSegment(seg.SegmentId);
            if (segSchema is not null && !index.ContainsKey(seg.SegmentId))
                index[seg.SegmentId] = (seg, segSchema);
        }

        return new X12DynamicTransaction(index);
    }

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
