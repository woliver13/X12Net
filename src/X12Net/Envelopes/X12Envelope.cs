using woliver13.X12Net.DOM;

namespace woliver13.X12Net.Envelopes;

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
        var interchange = X12Interchange.Parse(input);
        return new X12Envelope(
            senderId:                 interchange.ISA[6].Trim(),
            receiverId:               interchange.ISA[8].Trim(),
            date:                     interchange.ISA[9],
            time:                     interchange.ISA[10],
            interchangeControlNumber: int.Parse(interchange.ISA[13]),
            declaredGroupCount:       int.Parse(interchange.IEA[1]),
            actualGroupCount:         interchange.FunctionalGroups.Count);
    }
}
