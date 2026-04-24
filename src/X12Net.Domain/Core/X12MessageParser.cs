namespace woliver13.X12Net.Core;

internal static class X12MessageParser
{
    internal static (List<X12Segment> Segments, X12Delimiters Delimiters)
        Parse(string input)
    {
        var delimiters = X12Delimiters.FromIsa(input);
        return (X12SegmentParser.ParseAll(input, delimiters).ToList(), delimiters);
    }
}
