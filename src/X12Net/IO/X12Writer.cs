using System.Text;

namespace X12Net.IO;

/// <summary>
/// Builds EDI X12 interchange text segment by segment.
/// </summary>
public sealed class X12Writer
{
    private readonly StringBuilder _sb;
    private readonly char _elementSeparator;
    private readonly char _componentSeparator;
    private readonly char _segmentTerminator;

    /// <summary>
    /// Initializes an <see cref="X12Writer"/> with the standard delimiters.
    /// </summary>
    public X12Writer(
        char elementSeparator   = '*',
        char componentSeparator = ':',
        char segmentTerminator  = '~')
    {
        _elementSeparator   = elementSeparator;
        _componentSeparator = componentSeparator;
        _segmentTerminator  = segmentTerminator;
        _sb = new StringBuilder();
    }

    /// <summary>
    /// Writes a segment with the given <paramref name="segmentId"/> and <paramref name="elements"/>.
    /// </summary>
    public void WriteSegment(string segmentId, params string[] elements)
    {
        _sb.Append(segmentId);
        foreach (var element in elements)
        {
            _sb.Append(_elementSeparator);
            _sb.Append(element);
        }
        _sb.Append(_segmentTerminator);
    }

    /// <summary>Returns the accumulated EDI text.</summary>
    public override string ToString() => _sb.ToString();
}
