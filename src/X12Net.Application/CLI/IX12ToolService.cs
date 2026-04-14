namespace woliver13.X12Net.CLI;

/// <summary>
/// Provides EDI X12 operations: parse, validate, and edit.
/// </summary>
public interface IX12ToolService
{
    /// <summary>Parses <paramref name="input"/> and returns the segment IDs in document order.</summary>
    ParseResult Parse(string input);

    /// <summary>Validates the structural integrity of <paramref name="input"/>.</summary>
    ValidateResult Validate(string input);

    /// <summary>
    /// Sets the element at <paramref name="elementIndex"/> (1-based) of the first
    /// <paramref name="segmentId"/> segment to <paramref name="newValue"/>.
    /// </summary>
    EditResult Edit(string input, string segmentId, int elementIndex, string newValue);
}
