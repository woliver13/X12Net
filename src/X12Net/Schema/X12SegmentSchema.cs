namespace X12Net.Schema;

/// <summary>Describes the named elements of one segment type within a transaction set.</summary>
public sealed class X12SegmentSchema
{
    /// <summary>Initializes a segment schema.</summary>
    /// <param name="segmentId">The segment identifier (e.g. "CLM").</param>
    /// <param name="elementNames">
    /// Element names in order, 1-based (index 0 = element 1).
    /// </param>
    /// <param name="isRequired">When <c>true</c>, validation will fail if this segment is absent.</param>
    public X12SegmentSchema(string segmentId, IReadOnlyList<string> elementNames, bool isRequired = false)
    {
        SegmentId    = segmentId;
        ElementNames = elementNames;
        IsRequired   = isRequired;
    }

    /// <summary>The segment identifier this schema describes.</summary>
    public string SegmentId { get; }

    /// <summary>Element names in order (0-based; name at index 0 maps to element index 1).</summary>
    public IReadOnlyList<string> ElementNames { get; }

    /// <summary>When <c>true</c>, the segment must be present for validation to pass.</summary>
    public bool IsRequired { get; }

    /// <summary>
    /// Returns the 1-based element index for <paramref name="name"/>,
    /// or -1 if the name is not found.
    /// </summary>
    public int IndexOf(string name)
    {
        for (int i = 0; i < ElementNames.Count; i++)
            if (string.Equals(ElementNames[i], name, StringComparison.OrdinalIgnoreCase))
                return i + 1;
        return -1;
    }
}
