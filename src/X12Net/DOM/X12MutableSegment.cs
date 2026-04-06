namespace X12Net.DOM;

/// <summary>
/// A mutable view of an EDI X12 segment held inside an <see cref="X12Document"/>.
/// Element values may be read and written by 1-based index.
/// </summary>
public sealed class X12MutableSegment
{
    private readonly List<string> _elements;

    internal X12MutableSegment(string segmentId, IEnumerable<string> elements)
    {
        SegmentId = segmentId;
        _elements = new List<string>(elements);
    }

    /// <summary>The segment identifier, e.g. "ISA", "GS", "ST".</summary>
    public string SegmentId { get; }

    /// <summary>Gets or sets the element at the given 1-based <paramref name="index"/>.</summary>
    public string this[int index]
    {
        get => index - 1 < _elements.Count ? _elements[index - 1] : string.Empty;
        set
        {
            while (_elements.Count < index)
                _elements.Add(string.Empty);
            _elements[index - 1] = value;
        }
    }

    /// <summary>The number of elements in this segment.</summary>
    public int ElementCount => _elements.Count;

    /// <summary>All element values (0-based internally).</summary>
    internal IReadOnlyList<string> Elements => _elements;
}
