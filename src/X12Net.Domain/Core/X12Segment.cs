namespace woliver13.X12Net.Core;

/// <summary>
/// A parsed EDI X12 segment: its identifier and all element values in order.
/// </summary>
public sealed class X12Segment
{
    /// <summary>Initializes an <see cref="X12Segment"/>.</summary>
    public X12Segment(string segmentId, IReadOnlyList<string> elements)
    {
        SegmentId = segmentId;
        Elements  = elements;
    }

    /// <summary>The segment identifier, e.g. "ISA", "GS", "ST".</summary>
    public string SegmentId { get; }

    /// <summary>
    /// All element values in order (excluding the segment identifier itself).
    /// Component values within a composite element are joined with the component separator.
    /// </summary>
    public IReadOnlyList<string> Elements { get; }

    /// <summary>Gets the element at <paramref name="index"/> (1-based, matching X12 field numbering).</summary>
    public string this[int index] => Elements[index - 1];

    /// <summary>
    /// Returns the individual repetitions within the element at <paramref name="elementIndex"/>
    /// by splitting on <paramref name="repetitionSeparator"/>.
    /// When the element contains no repetition separator the result has a single entry.
    /// </summary>
    public IReadOnlyList<string> GetRepetitions(int elementIndex, char repetitionSeparator) =>
        Elements[elementIndex - 1].Split(repetitionSeparator);

    /// <summary>Serializes the segment back to EDI text.</summary>
    internal string ToEdi(X12Delimiters delimiters)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(SegmentId);
        foreach (var el in Elements)
        {
            sb.Append(delimiters.ElementSeparator);
            sb.Append(el);
        }
        sb.Append(delimiters.SegmentTerminator);
        return sb.ToString();
    }
}
