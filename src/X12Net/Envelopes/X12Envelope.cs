using X12Net.Core;
using X12Net.IO;

namespace X12Net.Envelopes;

/// <summary>
/// Parses and validates the ISA/IEA envelope of an EDI X12 interchange.
/// Exposes key control fields and a structural validity check.
/// </summary>
public sealed class X12Envelope
{
    private X12Envelope(
        string senderId,
        string receiverId,
        string date,
        string time,
        int    interchangeControlNumber,
        int    declaredGroupCount,
        int    actualGroupCount)
    {
        SenderId                 = senderId;
        ReceiverId               = receiverId;
        Date                     = date;
        Time                     = time;
        InterchangeControlNumber = interchangeControlNumber;
        DeclaredGroupCount       = declaredGroupCount;
        ActualGroupCount         = actualGroupCount;
    }

    // ── ISA fields ────────────────────────────────────────────────────────

    /// <summary>ISA06 – Interchange Sender ID (trimmed).</summary>
    public string SenderId { get; }

    /// <summary>ISA08 – Interchange Receiver ID (trimmed).</summary>
    public string ReceiverId { get; }

    /// <summary>ISA09 – Interchange Date (YYMMDD).</summary>
    public string Date { get; }

    /// <summary>ISA10 – Interchange Time (HHMM).</summary>
    public string Time { get; }

    /// <summary>ISA13 – Interchange Control Number.</summary>
    public int InterchangeControlNumber { get; }

    // ── IEA fields ────────────────────────────────────────────────────────

    /// <summary>IEA01 – Number of Functional Groups as declared in the trailer.</summary>
    public int DeclaredGroupCount { get; }

    /// <summary>Actual number of GS segments found in the interchange.</summary>
    public int ActualGroupCount { get; }

    // ── Validation ────────────────────────────────────────────────────────

    /// <summary><c>true</c> when structural envelope validation passes.</summary>
    public bool IsValid => ValidationMessage is null;

    /// <summary>
    /// Human-readable validation failure message, or <c>null</c> when valid.
    /// </summary>
    public string? ValidationMessage
    {
        get
        {
            if (DeclaredGroupCount != ActualGroupCount)
                return $"IEA group count mismatch: IEA01 declares {DeclaredGroupCount} " +
                       $"but found {ActualGroupCount} GS segment(s).";
            return null;
        }
    }

    // ── Factory ───────────────────────────────────────────────────────────

    /// <summary>Parses the ISA/IEA envelope from raw EDI X12 text.</summary>
    public static X12Envelope Parse(string input)
    {
        using var reader = new X12Reader(input);
        var segments = reader.ReadAllSegments().ToList();

        var isa = segments.FirstOrDefault(s => s.SegmentId == "ISA")
            ?? throw new InvalidOperationException("No ISA segment found.");

        var iea = segments.FirstOrDefault(s => s.SegmentId == "IEA")
            ?? throw new InvalidOperationException("No IEA segment found.");

        int gsCount = segments.Count(s => s.SegmentId == "GS");

        return new X12Envelope(
            senderId:                 isa[6].Trim(),
            receiverId:               isa[8].Trim(),
            date:                     isa[9],
            time:                     isa[10],
            interchangeControlNumber: int.Parse(isa[13]),
            declaredGroupCount:       int.Parse(iea[1]),
            actualGroupCount:         gsCount);
    }
}
