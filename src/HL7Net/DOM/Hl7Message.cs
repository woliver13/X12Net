using woliver13.HL7Net.Core;
using woliver13.HL7Net.IO;

namespace woliver13.HL7Net.DOM;

/// <summary>
/// An immutable DOM representation of an HL7 v2.x message, parsed from text.
/// </summary>
public sealed class Hl7Message
{
    private Hl7Message(IReadOnlyList<Hl7Segment> segments, Hl7Delimiters delimiters)
    {
        Segments   = segments;
        Delimiters = delimiters;
    }

    /// <summary>All segments in the message in order.</summary>
    public IReadOnlyList<Hl7Segment> Segments { get; }

    /// <summary>The delimiters detected from the MSH segment.</summary>
    public Hl7Delimiters Delimiters { get; }

    /// <summary>
    /// The message type from MSH-9 (e.g. "ADT^A01").
    /// Returns an empty string if the MSH segment is missing.
    /// </summary>
    public string MessageType
    {
        get
        {
            var msh = Segments.FirstOrDefault(s => s.SegmentId == "MSH");
            return msh?[9] ?? string.Empty;
        }
    }

    /// <summary>
    /// The HL7 version from MSH-12 (e.g. "2.5").
    /// Returns an empty string if the MSH segment is missing.
    /// </summary>
    public string Version
    {
        get
        {
            var msh = Segments.FirstOrDefault(s => s.SegmentId == "MSH");
            return msh?[12] ?? string.Empty;
        }
    }

    /// <summary>Parses an HL7 v2.x message from text.</summary>
    /// <exception cref="ArgumentException">Thrown when the message does not start with a valid MSH segment.</exception>
    public static Hl7Message Parse(string text)
    {
        if (text is null) throw new ArgumentNullException(nameof(text));

        var reader   = new Hl7Reader(text);
        var segments = reader.ReadAllSegments().ToList();

        if (segments.Count == 0 || segments[0].SegmentId != "MSH")
            throw new ArgumentException("HL7 message must begin with an MSH segment.", nameof(text));

        // Rebuild the MSH line prefix to detect delimiters
        var firstLine = text.TrimStart().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0];
        var delimiters = Hl7Delimiters.FromMsh(firstLine);

        return new Hl7Message(segments.AsReadOnly(), delimiters);
    }
}
