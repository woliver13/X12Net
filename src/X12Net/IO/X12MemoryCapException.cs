namespace X12Net.IO;

/// <summary>
/// Thrown when an <see cref="X12Reader"/> is configured with a segment cap and
/// the interchange being read exceeds that limit.
/// </summary>
public sealed class X12MemoryCapException : Exception
{
    /// <summary>Initializes the exception with the configured cap value.</summary>
    public X12MemoryCapException(int maxSegments)
        : base($"Interchange exceeds the configured segment cap of {maxSegments}. " +
               $"Increase the cap or process the file in streaming mode.")
    {
        MaxSegments = maxSegments;
    }

    /// <summary>The segment limit that was exceeded.</summary>
    public int MaxSegments { get; }
}
