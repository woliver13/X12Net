namespace X12Net.Schema;

/// <summary>
/// Describes the segment structure of a transaction set.
/// Supports single-level inheritance via <see cref="Extend"/>.
/// </summary>
public sealed class X12TransactionSchema
{
    private readonly Dictionary<string, X12SegmentSchema> _segments;

    /// <summary>Initializes a schema for the given transaction set.</summary>
    public X12TransactionSchema(
        string transactionSetId,
        string description,
        params X12SegmentSchema[] segments)
    {
        TransactionSetId = transactionSetId;
        Description      = description;
        _segments        = segments.ToDictionary(s => s.SegmentId, StringComparer.OrdinalIgnoreCase);
    }

    // Private constructor used by Extend to carry inherited segments
    private X12TransactionSchema(
        string transactionSetId,
        string description,
        Dictionary<string, X12SegmentSchema> segments)
    {
        TransactionSetId = transactionSetId;
        Description      = description;
        _segments        = segments;
    }

    /// <summary>The transaction set identifier (e.g. "837", "999").</summary>
    public string TransactionSetId { get; }

    /// <summary>Human-readable description.</summary>
    public string Description { get; }

    /// <summary>Returns the segment schema for <paramref name="segmentId"/>, or <c>null</c> if not registered.</summary>
    public X12SegmentSchema? GetSegment(string segmentId) =>
        _segments.TryGetValue(segmentId, out var s) ? s : null;

    /// <summary>All segment schemas in this transaction schema.</summary>
    public IReadOnlyCollection<X12SegmentSchema> Segments => _segments.Values;

    /// <summary>
    /// Creates a derived schema that inherits all segments from this instance and
    /// adds (or overrides) the supplied <paramref name="additionalSegments"/>.
    /// </summary>
    public X12TransactionSchema Extend(
        string transactionSetId,
        string description,
        params X12SegmentSchema[] additionalSegments)
    {
        var merged = new Dictionary<string, X12SegmentSchema>(_segments, StringComparer.OrdinalIgnoreCase);
        foreach (var seg in additionalSegments)
            merged[seg.SegmentId] = seg;

        return new X12TransactionSchema(transactionSetId, description, merged);
    }
}
