using X12Net.Core;
using X12Net.IO;

namespace X12Net.CLI;

/// <summary>Result of a <see cref="ParseCommand"/> execution.</summary>
public sealed class ParseResult
{
    internal ParseResult(bool success, IReadOnlyList<string> segmentIds, string? error = null)
    {
        Success    = success;
        SegmentIds = segmentIds;
        Error      = error;
    }

    /// <summary><c>true</c> when parsing completed without exception.</summary>
    public bool Success { get; }

    /// <summary>Segment IDs in document order.</summary>
    public IReadOnlyList<string> SegmentIds { get; }

    /// <summary>Error message when <see cref="Success"/> is <c>false</c>.</summary>
    public string? Error { get; }
}

/// <summary>
/// CLI command: parse EDI X12 text and return an ordered list of segment IDs.
/// </summary>
public static class ParseCommand
{
    /// <summary>Executes the parse command against <paramref name="input"/>.</summary>
    public static ParseResult Execute(string input)
    {
        try
        {
            using var reader = new X12Reader(input);
            var ids = reader.ReadAllSegments().Select(s => s.SegmentId).ToList();
            return new ParseResult(true, ids);
        }
        catch (Exception ex)
        {
            return new ParseResult(false, Array.Empty<string>(), ex.Message);
        }
    }
}
