using woliver13.HL7Net.Core;

namespace woliver13.HL7Net.IO;

/// <summary>
/// Serializes HL7 v2.x segments to text.
/// </summary>
public sealed class Hl7Writer
{
    private readonly Hl7Delimiters _delimiters;

    /// <summary>Initializes an <see cref="Hl7Writer"/> with the given delimiters.</summary>
    public Hl7Writer(Hl7Delimiters delimiters)
    {
        _delimiters = delimiters ?? throw new ArgumentNullException(nameof(delimiters));
    }

    /// <summary>
    /// Writes a segment to HL7 text. For MSH, the first field is the field separator
    /// itself (MSH-1) and the second field is the encoding characters (MSH-2), so
    /// they are written verbatim without additional separators.
    /// </summary>
    /// <param name="segmentId">The 3-character segment identifier, e.g. "PID", "MSH".</param>
    /// <param name="fields">Field values in order (1-based; for MSH, field 1 is FieldSeparator, field 2 is encoding chars).</param>
    public string WriteSegment(string segmentId, params string[] fields)
    {
        if (segmentId == "MSH")
            return WriteMsh(fields);

        var sb = new System.Text.StringBuilder();
        sb.Append(segmentId);
        foreach (var f in fields)
        {
            sb.Append(_delimiters.FieldSeparator);
            sb.Append(f);
        }
        sb.Append('\r');
        return sb.ToString();
    }

    private string WriteMsh(string[] fields)
    {
        // MSH line: MSH|^~\&|field3|field4|...
        // fields[0] = FieldSeparator value (e.g. "|")
        // fields[1] = encoding chars (e.g. "^~\&")
        // remaining fields start at index 2
        var sb = new System.Text.StringBuilder();
        sb.Append("MSH");
        sb.Append(_delimiters.FieldSeparator);   // MSH-1 is the separator itself
        // fields[1] is encoding chars (MSH-2), no extra separator needed before it
        if (fields.Length >= 2)
            sb.Append(fields[1]);
        for (int i = 2; i < fields.Length; i++)
        {
            sb.Append(_delimiters.FieldSeparator);
            sb.Append(fields[i]);
        }
        sb.Append('\r');
        return sb.ToString();
    }
}
