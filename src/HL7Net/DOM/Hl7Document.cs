using System.Text;
using woliver13.HL7Net.Core;
using woliver13.HL7Net.IO;

namespace woliver13.HL7Net.DOM;

/// <summary>
/// A mutable DOM representation of an HL7 v2.x message.
/// Segments may be edited via <see cref="Hl7MutableSegment"/> indexers.
/// </summary>
public sealed class Hl7Document
{
    private readonly List<Hl7MutableSegment> _segments;
    private readonly Hl7Delimiters _delimiters;

    private Hl7Document(List<Hl7MutableSegment> segments, Hl7Delimiters delimiters)
    {
        _segments  = segments;
        _delimiters = delimiters;
    }

    /// <summary>All mutable segments in document order.</summary>
    public IReadOnlyList<Hl7MutableSegment> Segments => _segments;

    /// <summary>Parses an HL7 v2.x message from text into a mutable document.</summary>
    public static Hl7Document Parse(string text)
    {
        if (text is null) throw new ArgumentNullException(nameof(text));

        var reader   = new Hl7Reader(text);
        var parsed   = reader.ReadAllSegments().ToList();

        if (parsed.Count == 0 || parsed[0].SegmentId != "MSH")
            throw new ArgumentException("HL7 message must begin with an MSH segment.", nameof(text));

        var firstLine  = text.TrimStart().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0];
        var delimiters = Hl7Delimiters.FromMsh(firstLine);

        var segments = parsed
            .Select(s => new Hl7MutableSegment(s.SegmentId, s.Fields))
            .ToList();

        return new Hl7Document(segments, delimiters);
    }

    /// <summary>Serializes the document back to HL7 text.</summary>
    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var seg in _segments)
        {
            if (seg.SegmentId == "MSH")
            {
                // MSH|^~\&|field3|...
                sb.Append("MSH");
                sb.Append(_delimiters.FieldSeparator);
                // seg[1] = field separator itself (MSH-1), seg[2] = encoding chars (MSH-2)
                // Start serializing from MSH-2 (fields[1])
                for (int i = 1; i <= seg.FieldCount; i++)
                {
                    if (i == 1)
                    {
                        // MSH-1 is the field separator — it is already written above as the separator
                        // but we still need to separate MSH-2 from it with nothing extra
                        // Actually MSH line looks like: MSH|^~\&|...
                        // fields[0]="|", fields[1]="^~\&"
                        // After "MSH|" we write "^~\&" (MSH-2) then "|" for each subsequent field
                        sb.Append(seg[2]); // MSH-2 = encoding chars
                    }
                    else if (i == 2)
                    {
                        // Already written MSH-2 above, skip
                        continue;
                    }
                    else
                    {
                        sb.Append(_delimiters.FieldSeparator);
                        sb.Append(seg[i]);
                    }
                }
                sb.Append('\r');
            }
            else
            {
                sb.Append(seg.SegmentId);
                for (int i = 1; i <= seg.FieldCount; i++)
                {
                    sb.Append(_delimiters.FieldSeparator);
                    sb.Append(seg[i]);
                }
                sb.Append('\r');
            }
        }
        return sb.ToString();
    }
}
