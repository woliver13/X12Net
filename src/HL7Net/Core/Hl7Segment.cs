namespace woliver13.HL7Net.Core;

/// <summary>
/// A parsed HL7 v2.x segment: its identifier and all field values in order.
/// </summary>
public sealed class Hl7Segment
{
    private readonly IReadOnlyList<string> _fields;

    /// <summary>Initializes an <see cref="Hl7Segment"/>.</summary>
    public Hl7Segment(string segmentId, IReadOnlyList<string> fields)
    {
        SegmentId = segmentId;
        _fields   = fields;
    }

    /// <summary>The segment identifier, e.g. "MSH", "PID", "EVN".</summary>
    public string SegmentId { get; }

    /// <summary>All field values in order (excluding the segment identifier itself).</summary>
    public IReadOnlyList<string> Fields => _fields;

    /// <summary>Gets the field at <paramref name="index"/> (1-based, matching HL7 field numbering).
    /// Returns an empty string if the index is beyond the last field.</summary>
    public string this[int index] => index - 1 < _fields.Count ? _fields[index - 1] : string.Empty;

    /// <summary>The number of fields in this segment.</summary>
    public int FieldCount => _fields.Count;

    /// <summary>
    /// Returns the individual repetitions within the field at <paramref name="fieldIndex"/>
    /// by splitting on <paramref name="repetitionSeparator"/>.
    /// When the field contains no repetition separator the result has a single entry.
    /// </summary>
    public IReadOnlyList<string> GetRepetitions(int fieldIndex, char repetitionSeparator) =>
        this[fieldIndex].Split(repetitionSeparator);
}
