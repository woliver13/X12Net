namespace woliver13.HL7Net.DOM;

/// <summary>
/// A mutable view of an HL7 v2.x segment held inside an <see cref="Hl7Document"/>.
/// Field values may be read and written by 1-based index.
/// </summary>
public sealed class Hl7MutableSegment
{
    private readonly List<string> _fields;

    internal Hl7MutableSegment(string segmentId, IEnumerable<string> fields)
    {
        SegmentId = segmentId;
        _fields   = new List<string>(fields);
    }

    /// <summary>The segment identifier, e.g. "MSH", "PID".</summary>
    public string SegmentId { get; }

    /// <summary>Gets or sets the field at the given 1-based <paramref name="index"/>.</summary>
    public string this[int index]
    {
        get => index - 1 < _fields.Count ? _fields[index - 1] : string.Empty;
        set
        {
            while (_fields.Count < index)
                _fields.Add(string.Empty);
            _fields[index - 1] = value;
        }
    }

    /// <summary>The number of fields in this segment.</summary>
    public int FieldCount => _fields.Count;

    /// <summary>All field values (0-based internally).</summary>
    internal IReadOnlyList<string> Fields => _fields;
}
