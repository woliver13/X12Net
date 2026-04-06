using X12Net.DOM;

namespace X12Net.CLI;

/// <summary>Result of an <see cref="EditCommand"/> execution.</summary>
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
/// CLI command: edit a single element in an EDI X12 interchange and return the result.
/// </summary>
public static class EditCommand
{
    /// <summary>
    /// Sets the element at <paramref name="elementIndex"/> (1-based) of the first
    /// <paramref name="segmentId"/> segment to <paramref name="newValue"/>.
    /// </summary>
    public static EditResult Execute(
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
