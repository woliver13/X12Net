using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using woliver13.X12Net.DOM;
using woliver13.X12Net.IO;
using woliver13.X12Net.Validation;

namespace woliver13.X12Net.CLI;

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
    /// <param name="logger">Optional logger; defaults to <see cref="NullLogger.Instance"/>.</param>
    public static ParseResult Parse(string input, ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;
        try
        {
            using var reader = new X12Reader(input);
            var ids = reader.ReadAllSegments().Select(s => s.SegmentId).ToList();
            logger.LogInformation("Parsed {SegmentCount} segment(s).", ids.Count);
            return new ParseResult(true, ids);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Parse failed: {Message}", ex.Message);
            return new ParseResult(false, Array.Empty<string>(), ex.Message);
        }
    }

    /// <summary>
    /// Validates the structural integrity of <paramref name="input"/>.
    /// </summary>
    /// <param name="logger">Optional logger; defaults to <see cref="NullLogger.Instance"/>.</param>
    public static ValidateResult Validate(string input, ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;
        var result = new ValidateResult(X12Validator.Validate(input));
        if (result.IsValid)
            logger.LogInformation("Validation passed.");
        else
            logger.LogWarning("Validation failed with {ErrorCount} error(s).", result.Errors.Count);
        return result;
    }

    /// <summary>
    /// Sets the element at <paramref name="elementIndex"/> (1-based) of the first
    /// <paramref name="segmentId"/> segment to <paramref name="newValue"/>.
    /// </summary>
    /// <param name="logger">Optional logger; defaults to <see cref="NullLogger.Instance"/>.</param>
    public static EditResult Edit(
        string   input,
        string   segmentId,
        int      elementIndex,
        string   newValue,
        ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;
        try
        {
            var doc = X12Document.Parse(input);
            doc[segmentId, elementIndex] = newValue;
            logger.LogInformation("Edited {SegmentId}[{Index}].", segmentId, elementIndex);
            return new EditResult(true, doc.ToString());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Edit failed: {Message}", ex.Message);
            return new EditResult(false, input, ex.Message);
        }
    }
}
