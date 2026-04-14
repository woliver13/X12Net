namespace woliver13.HL7Net.IO;

/// <summary>
/// Thrown when an HL7 message exceeds the maximum number of segments configured on the reader.
/// </summary>
public sealed class Hl7MemoryCapException : Exception
{
    /// <summary>The configured segment cap that was exceeded.</summary>
    public int MaxSegments { get; }

    /// <summary>Initializes an <see cref="Hl7MemoryCapException"/>.</summary>
    public Hl7MemoryCapException(int maxSegments)
        : base($"HL7 message exceeds the maximum allowed segment count of {maxSegments}.")
    {
        MaxSegments = maxSegments;
    }
}
