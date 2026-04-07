using X12Net.DOM;
using X12Net.IO;
using X12Net.Validation;

namespace X12Net.CLI;

/// <summary>Result of <see cref="X12Tool.Parse"/>.</summary>
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

/// <summary>Result of <see cref="X12Tool.Validate"/>.</summary>
public sealed class ValidateResult
{
    internal ValidateResult(X12ValidationResult inner)
    {
        IsValid = inner.IsValid;
        Errors  = inner.Errors.Select(e => e.ToString()).ToList();
    }

    /// <summary><c>true</c> when the interchange passed all structural validation rules.</summary>
    public bool IsValid { get; }

    /// <summary>Human-readable error strings; empty when valid.</summary>
    public IReadOnlyList<string> Errors { get; }
}

/// <summary>Result of <see cref="X12Tool.Edit"/>.</summary>
public sealed class EditResult
{
    internal EditResult(bool success, string output, string? error = null)
    {
        Success = success;
        Output  = output;
        Error   = error;
    }

    /// <summary><c>true</c> when the edit completed without exception.</summary>
    public bool Success { get; }

    /// <summary>The modified EDI X12 text.</summary>
    public string Output { get; }

    /// <summary>Error message when <see cref="Success"/> is <c>false</c>.</summary>
    public string? Error { get; }
}

/// <summary>
/// Façade for common EDI X12 operations: parse, validate, and edit.
/// </summary>
public static class X12Tool
{
    /// <summary>
    /// Parses <paramref name="input"/> and returns the segment IDs in document order.
    /// </summary>
    public static ParseResult Parse(string input)
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

    /// <summary>
    /// Validates the structural integrity of <paramref name="input"/>.
    /// </summary>
    public static ValidateResult Validate(string input) =>
        new(X12Validator.Validate(input));

    /// <summary>
    /// Sets the element at <paramref name="elementIndex"/> (1-based) of the first
    /// <paramref name="segmentId"/> segment to <paramref name="newValue"/>.
    /// </summary>
    public static EditResult Edit(
        string input,
        string segmentId,
        int    elementIndex,
        string newValue)
    {
        try
        {
            var doc = X12Document.Parse(input);
            doc[segmentId, elementIndex] = newValue;
            return new EditResult(true, doc.ToString());
        }
        catch (Exception ex)
        {
            return new EditResult(false, input, ex.Message);
        }
    }
}
